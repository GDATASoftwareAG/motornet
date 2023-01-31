using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CloudNative.CloudEvents.SystemTextJson;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using Motor.Extensions.Hosting.Kafka;
using Motor.Extensions.Hosting.Kafka.Options;
using Motor.Extensions.TestUtilities;
using RandomDataGenerator.FieldOptions;
using RandomDataGenerator.Randomizers;
using Xunit;
using Range = Moq.Range;

namespace Motor.Extensions.Hosting.Kafka_IntegrationTest;

[Collection("KafkaMessage")]
public class KafkaExtensionTests : IClassFixture<KafkaFixture>
{
    private readonly KafkaFixture _fixture;
    private readonly IRandomizerString _randomizerString;

    public KafkaExtensionTests(KafkaFixture fixture)
    {
        _fixture = fixture;
        _randomizerString = RandomizerFactory.GetRandomizer(new FieldOptionsTextRegex { Pattern = @"^[A-Z]{10}" });
    }

    [Fact(Timeout = 50000)]
    public async Task Consume_RawPublishIntoKafkaAndConsumeCreateCloudEvent_ConsumedEqualsPublished()
    {
        var topic = _randomizerString.Generate();
        const string message = "testMessage";
        await PublishMessage(topic, message);
        var consumer = GetConsumer<string>(topic);
        var rawConsumedKafkaMessage = (byte[])null;
        var taskCompletionSource = new TaskCompletionSource();
        consumer.ConsumeCallbackAsync = async (dataEvent, _) =>
        {
            rawConsumedKafkaMessage = dataEvent.TypedData;
            taskCompletionSource.TrySetResult();
            return await Task.FromResult(ProcessedMessageStatus.Success);
        };

        await consumer.StartAsync();
        var consumerStartTask = consumer.ExecuteAsync();

        await Task.WhenAny(consumerStartTask, taskCompletionSource.Task);
        Assert.Equal(message, Encoding.UTF8.GetString(rawConsumedKafkaMessage));
    }

    [Fact(Timeout = 50000)]
    public async Task Consume_PublishIntoKafkaAndConsumeWithCloudEvent_ConsumedEqualsPublished()
    {
        var topic = _randomizerString.Generate();
        const string message = "testMessage";
        var publisher = GetPublisher<byte[]>(topic);
        var motorCloudEvent =
            MotorCloudEvent.CreateTestCloudEvent(message).CreateNew(Encoding.UTF8.GetBytes(message));
        await publisher.PublishMessageAsync(motorCloudEvent, CancellationToken.None);
        var consumer = GetConsumer<byte[]>(topic);
        string id = null;
        var taskCompletionSource = new TaskCompletionSource();
        consumer.ConsumeCallbackAsync = async (dataEvent, _) =>
        {
            id = dataEvent.Id;
            taskCompletionSource.TrySetResult();
            return await Task.FromResult(ProcessedMessageStatus.Success);
        };

        await consumer.StartAsync();
        var consumerStartTask = consumer.ExecuteAsync();

        await Task.WhenAny(consumerStartTask, taskCompletionSource.Task);
        Assert.Equal(motorCloudEvent.Id, id);
    }

    [Fact(Timeout = 50000)]
    public async Task Consume_PublishIntoExtensionDefinedTopic_ConsumedEqualsPublished()
    {
        var topic = _randomizerString.Generate();
        const string message = "testMessage";
        var publisher = GetPublisher<byte[]>("wrong_topic");
        var motorCloudEvent =
            MotorCloudEvent.CreateTestCloudEvent(message).CreateNew(Encoding.UTF8.GetBytes(message));
        motorCloudEvent.SetKafkaTopic(topic);
        await publisher.PublishMessageAsync(motorCloudEvent, CancellationToken.None);
        var consumer = GetConsumer<byte[]>(topic);
        string id = null;
        var taskCompletionSource = new TaskCompletionSource();
        consumer.ConsumeCallbackAsync = async (dataEvent, _) =>
        {
            id = dataEvent.Id;
            taskCompletionSource.TrySetResult();
            return await Task.FromResult(ProcessedMessageStatus.Success);
        };

        await consumer.StartAsync();
        var consumerStartTask = consumer.ExecuteAsync();

        await Task.WhenAny(consumerStartTask, taskCompletionSource.Task);
        Assert.Equal(motorCloudEvent.Id, id);
    }

    [Fact(Timeout = 50000)]
    public async Task Consume_LimitMaxConcurrentMessages_StartProcessingLimitedNumberOfMessagesSimultaneously()
    {
        const int maxConcurrentMessages = 5;
        var topic = _randomizerString.Generate();
        const string message = "testMessage";
        for (var i = 0; i < maxConcurrentMessages * 2; i++)
        {
            await PublishMessage(topic, message);
        }
        var consumer = GetConsumer<string>(topic, maxConcurrentMessages: maxConcurrentMessages);
        var numberOfParallelMessages = 0;
        consumer.ConsumeCallbackAsync = async (_, cancellationToken) =>
        {
            numberOfParallelMessages++;
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            numberOfParallelMessages--;
            return await Task.FromResult(ProcessedMessageStatus.Success);
        };

        await consumer.StartAsync();
        consumer.ExecuteAsync();
        await Task.Delay(TimeSpan.FromSeconds(2));

        Assert.Equal(maxConcurrentMessages, numberOfParallelMessages);
    }

    [Fact]
    public async Task Consume_CorrectlyCommitMessages_InvalidInputNotProcessedAgain()
    {
        const int messageCount = 10;
        var faultyData = 3.ToString();
        var topic = _randomizerString.Generate();
        for (var i = 0; i < messageCount; i++)
        {
            await PublishMessage(topic, i.ToString());
        }
        var consumer = GetConsumer<string>(topic, commitPeriod: messageCount / 2);
        List<string> processedEvents = new();
        consumer.ConsumeCallbackAsync = async (dataCloudEvent, _) =>
        {
            var data = Encoding.UTF8.GetString(dataCloudEvent.TypedData);
            var result = data == faultyData ? ProcessedMessageStatus.InvalidInput : ProcessedMessageStatus.Success;
            processedEvents.Add(data);
            return await Task.FromResult(result);
        };
        var cancellationTokenSource = new CancellationTokenSource();

        await consumer.StartAsync();
        consumer.ExecuteAsync(cancellationTokenSource.Token);
        await Task.Delay(TimeSpan.FromSeconds(10));
        cancellationTokenSource.Cancel();

        for (var i = 0; i < messageCount; i++)
        {
            Assert.Single(processedEvents.GroupBy(data => data).First(group => group.Key == i.ToString()));
        }
    }
    
    [Fact]
    public async Task Consume_CorrectlyCommitMessages_TemporaryFailureProcessedAgain()
    {
        const int messageCount = 10;
        var faultyData = 3.ToString();
        var topic = _randomizerString.Generate();
        for (var i = 0; i < messageCount; i++)
        {
            await PublishMessage(topic, i.ToString());
        }
        var consumer = GetConsumer<string>(topic, commitPeriod: messageCount / 2);
        List<string> processedEvents = new();
        consumer.ConsumeCallbackAsync = async (dataCloudEvent, _) =>
        {
            var data = Encoding.UTF8.GetString(dataCloudEvent.TypedData);
            var result = data == faultyData ? ProcessedMessageStatus.TemporaryFailure : ProcessedMessageStatus.Success;
            processedEvents.Add(data);
            return await Task.FromResult(result);
        };
        var cancellationTokenSource = new CancellationTokenSource();

        await consumer.StartAsync();
        consumer.ExecuteAsync(cancellationTokenSource.Token);
        await Task.Delay(TimeSpan.FromSeconds(10));
        cancellationTokenSource.Cancel();

        Assert.True(processedEvents.Count(d => d == faultyData) > 1);
    }
    
    private async Task PublishMessage(string topic, string value)
    {
        using var producer = new ProducerBuilder<string, byte[]>(GetPublisherConfig<string>(topic)).Build();
        await producer.ProduceAsync(topic,
            new Message<string, byte[]> { Key = "someKey", Value = Encoding.UTF8.GetBytes(value) });
        producer.Flush();
    }

    private KafkaMessageConsumer<T> GetConsumer<T>(string topic, int commitPeriod = 1, int maxConcurrentMessages = 1000)
    {
        var options = Options.Create(GetConsumerConfig<T>(topic, commitPeriod, maxConcurrentMessages));
        var fakeLoggerMock = Mock.Of<ILogger<KafkaMessageConsumer<T>>>();
        return new KafkaMessageConsumer<T>(fakeLoggerMock, options, null, GetApplicationNameService(),
            new JsonEventFormatter());
    }

    private KafkaMessagePublisher<T> GetPublisher<T>(string topic)
    {
        var options = Options.Create(GetPublisherConfig<T>(topic));
        var publisherOptions = Options.Create(new PublisherOptions());
        return new KafkaMessagePublisher<T>(options, new JsonEventFormatter(), publisherOptions);
    }

    private KafkaPublisherOptions<T> GetPublisherConfig<T>(string topic)
    {
        return new()
        {
            Topic = topic,
            BootstrapServers = _fixture.BootstrapServers,
        };
    }

    private IApplicationNameService GetApplicationNameService(string source = "test://non")
    {
        var mock = new Mock<IApplicationNameService>();
        mock.Setup(t => t.GetSource()).Returns(new Uri(source));
        return mock.Object;
    }

    private KafkaConsumerOptions<T> GetConsumerConfig<T>(string topic, int commitPeriod, int maxConcurrentMessages)
    {
        return new()
        {
            Topic = topic,
            GroupId = "group_id",
            CommitPeriod = commitPeriod,
            BootstrapServers = _fixture.BootstrapServers,
            EnableAutoCommit = false,
            StatisticsIntervalMs = 5000,
            SessionTimeoutMs = 6000,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            MaxConcurrentMessages = maxConcurrentMessages
        };
    }
}
