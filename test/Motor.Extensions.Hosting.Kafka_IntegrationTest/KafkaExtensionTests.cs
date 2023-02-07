using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CloudNative.CloudEvents.SystemTextJson;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
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
        await PublishMessage(topic, "someKey", message);
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
        var taskCompletionSource = new TaskCompletionSource();
        var topic = _randomizerString.Generate();
        const string message = "testMessage";
        for (var i = 0; i < maxConcurrentMessages * 2; i++)
        {
            await PublishMessage(topic, "someKey", message);
        }
        var config = GetConsumerConfig<string>(topic, maxConcurrentMessages);
        var consumer = GetConsumer(topic, config);
        var numberOfStartedMessages = 0;
        consumer.ConsumeCallbackAsync = async (_, cancellationToken) =>
        {
            numberOfStartedMessages++;
            taskCompletionSource.TrySetResult();
            await Task.Delay(-1, cancellationToken);  // Wait indefinitely
            return await Task.FromResult(ProcessedMessageStatus.Success);
        };

        await consumer.StartAsync();
        consumer.ExecuteAsync();

        // Wait until processing begins
        await taskCompletionSource.Task;
        // Give consumer enough time to process further messages
        await Task.Delay(TimeSpan.FromSeconds(1));
        Assert.Equal(maxConcurrentMessages, numberOfStartedMessages);
    }

    [Theory(Timeout = 50000)]
    [InlineData(ProcessedMessageStatus.TemporaryFailure)]
    [InlineData(ProcessedMessageStatus.CriticalFailure)]
    public async Task Consume_SynchronousMessageHandlingWhereProcessingFailed_DoesNotProcessSecondMessage(ProcessedMessageStatus returnStatus)
    {
        var taskCompletionSource = new TaskCompletionSource();
        var topic = _randomizerString.Generate();
        await PublishMessage(topic, "someKey", "1");
        await PublishMessage(topic, "someKey", "2");
        var config = GetConsumerConfig<string>(topic, maxConcurrentMessages: 1);
        var consumer = GetConsumer(topic, config);
        var distinctHandledMessages = new HashSet<string>();
        consumer.ConsumeCallbackAsync = async (data, _) =>
        {
            taskCompletionSource.TrySetResult();
            distinctHandledMessages.Add(Encoding.UTF8.GetString(data.TypedData));
            return await Task.FromResult(returnStatus);
        };

        await consumer.StartAsync();
        consumer.ExecuteAsync();

        // Wait until processing begins
        await taskCompletionSource.Task;
        // Give consumer enough time to process further messages
        await Task.Delay(TimeSpan.FromSeconds(1));
        Assert.Single(distinctHandledMessages);
    }

    [Theory(Timeout = 50000)]
    [InlineData(ProcessedMessageStatus.Success)]
    [InlineData(ProcessedMessageStatus.Failure)]
    [InlineData(ProcessedMessageStatus.InvalidInput)]
    public async Task Consume_SynchronousMessageHandlingWithMultipleMessages_AllMessagesProcessed(ProcessedMessageStatus processedMessageStatus)
    {
        const int numMessages = 10;
        var taskCompletionSource = new TaskCompletionSource();
        var topic = _randomizerString.Generate();
        var messages = Enumerable.Range(1, numMessages).Select(i => $"{i}").ToHashSet();
        foreach (var message in messages)
        {
            await PublishMessage(topic, "someKey", message);
        }
        var config = GetConsumerConfig<string>(topic, maxConcurrentMessages: 1);
        var consumer = GetConsumer(topic, config);
        var distinctHandledMessages = new HashSet<string>();
        consumer.ConsumeCallbackAsync = async (data, _) =>
        {
            taskCompletionSource.TrySetResult();
            distinctHandledMessages.Add(Encoding.UTF8.GetString(data.TypedData));
            return await Task.FromResult(processedMessageStatus);
        };

        await consumer.StartAsync();
        consumer.ExecuteAsync();

        // Wait until processing begins
        await taskCompletionSource.Task;
        // Give consumer enough time to process further messages
        await Task.Delay(TimeSpan.FromSeconds(1));
        Assert.True(messages.SetEquals(distinctHandledMessages));
    }

    [Fact(Timeout = 50000)]
    public async Task Consume_TemporaryFailure_ExecuteTheConfiguredNumberOfRetries()
    {
        const int expectedNumberOfRetries = 2;
        var taskCompletionSource = new TaskCompletionSource();
        var topic = _randomizerString.Generate();
        await PublishMessage(topic, "someKey", "message");
        var config = GetConsumerConfig<string>(topic, retriesOnTemporaryFailure: expectedNumberOfRetries);
        var consumer = GetConsumer(topic, config);
        var actualNumberOfTries = 0;
        consumer.ConsumeCallbackAsync = async (data, _) =>
        {
            taskCompletionSource.TrySetResult();
            actualNumberOfTries += 1;
            return await Task.FromResult(ProcessedMessageStatus.TemporaryFailure);
        };

        await consumer.StartAsync();
        consumer.ExecuteAsync();

        // Wait until processing begins
        await taskCompletionSource.Task;
        // Give consumer enough time to handle returned ProcessedMessageStatus
        await Task.Delay(TimeSpan.FromSeconds(2 * Math.Pow(2, expectedNumberOfRetries)));
        Assert.Equal(expectedNumberOfRetries + 1, actualNumberOfTries);
    }

    [Fact(Timeout = 50000)]
    public async Task Consume_TemporaryFailureEvenAfterRetries_ApplicationIsStopped()
    {
        const int numberOfRetries = 2;
        var fakeLifetimeMock = new Mock<IHostApplicationLifetime>();
        var taskCompletionSource = new TaskCompletionSource();
        var topic = _randomizerString.Generate();
        await PublishMessage(topic, "someKey", "message");
        var config = GetConsumerConfig<string>(topic, retriesOnTemporaryFailure: numberOfRetries);
        var consumer = GetConsumer(topic, config, fakeLifetimeMock.Object);
        consumer.ConsumeCallbackAsync = async (data, _) =>
        {
            taskCompletionSource.TrySetResult();
            return await Task.FromResult(ProcessedMessageStatus.TemporaryFailure);
        };

        await consumer.StartAsync();
        consumer.ExecuteAsync();

        // Wait until processing begins
        await taskCompletionSource.Task;
        // Give consumer enough time to handle returned ProcessedMessageStatus
        await Task.Delay(TimeSpan.FromSeconds(2 * Math.Pow(2, numberOfRetries)));
        fakeLifetimeMock.Verify(mock => mock.StopApplication(), Times.Once);
    }

    [Fact(Timeout = 50000)]
    public async Task Consume_CriticalFailure_ApplicationIsStopped()
    {
        var fakeLifetimeMock = new Mock<IHostApplicationLifetime>();
        var taskCompletionSource = new TaskCompletionSource();
        var topic = _randomizerString.Generate();
        await PublishMessage(topic, "someKey", "message");
        var config = GetConsumerConfig<string>(topic);
        var consumer = GetConsumer(topic, config, fakeLifetimeMock.Object);
        consumer.ConsumeCallbackAsync = async (data, _) =>
        {
            taskCompletionSource.TrySetResult();
            return await Task.FromResult(ProcessedMessageStatus.CriticalFailure);
        };

        await consumer.StartAsync();
        consumer.ExecuteAsync();

        // Wait until processing begins
        await taskCompletionSource.Task;
        // Give consumer enough time to handle returned ProcessedMessageStatus
        await Task.Delay(TimeSpan.FromSeconds(1));
        fakeLifetimeMock.Verify(mock => mock.StopApplication(), Times.Once);
    }

    private async Task PublishMessage(string topic, string key, string value)
    {
        using var producer = new ProducerBuilder<string, byte[]>(GetPublisherConfig<string>(topic)).Build();
        await producer.ProduceAsync(topic,
            new Message<string, byte[]> { Key = key, Value = Encoding.UTF8.GetBytes(value) });
        producer.Flush();
    }

    private KafkaMessageConsumer<T> GetConsumer<T>(string topic, KafkaConsumerOptions<T> config = null, IHostApplicationLifetime fakeLifetimeMock = null)
    {
        var options = Options.Create(config ?? GetConsumerConfig<T>(topic));
        var fakeLoggerMock = Mock.Of<ILogger<KafkaMessageConsumer<T>>>();
        fakeLifetimeMock ??= Mock.Of<IHostApplicationLifetime>();
        return new KafkaMessageConsumer<T>(fakeLoggerMock, options, fakeLifetimeMock, null, GetApplicationNameService(),
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

    private KafkaConsumerOptions<T> GetConsumerConfig<T>(string topic, int maxConcurrentMessages = 1000,
        string groupId = "group_id", int retriesOnTemporaryFailure = 10)
    {
        return new()
        {
            Topic = topic,
            GroupId = groupId,
            CommitPeriod = 1000,
            BootstrapServers = _fixture.BootstrapServers,
            EnableAutoCommit = false,
            StatisticsIntervalMs = 5000,
            SessionTimeoutMs = 6000,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            MaxConcurrentMessages = maxConcurrentMessages,
            RetriesOnTemporaryFailure = retriesOnTemporaryFailure
        };
    }
}
