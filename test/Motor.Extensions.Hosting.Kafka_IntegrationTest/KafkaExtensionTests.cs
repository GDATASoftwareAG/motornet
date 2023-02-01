using System;
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
        var message = "testMessage";
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
        var message = "testMessage";
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
        var consumer = GetConsumer<string>(topic, maxConcurrentMessages);
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

    private async Task PublishMessage(string topic, string key, string value)
    {
        using var producer = new ProducerBuilder<string, byte[]>(GetPublisherConfig<string>(topic)).Build();
        await producer.ProduceAsync(topic,
            new Message<string, byte[]> { Key = key, Value = Encoding.UTF8.GetBytes(value) });
        producer.Flush();
    }

    private KafkaMessageConsumer<T> GetConsumer<T>(string topic, int maxConcurrentMessages = 1000)
    {
        var options = Options.Create(GetConsumerConfig<T>(topic, maxConcurrentMessages));
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

    private KafkaConsumerOptions<T> GetConsumerConfig<T>(string topic, int maxConcurrentMessages, string groupId = "group_id")
    {
        return new()
        {
            Topic = topic,
            GroupId = groupId,
            CommitPeriod = 1,
            BootstrapServers = _fixture.BootstrapServers,
            EnableAutoCommit = false,
            StatisticsIntervalMs = 5000,
            SessionTimeoutMs = 6000,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            MaxConcurrentMessages = maxConcurrentMessages
        };
    }
}
