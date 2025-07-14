using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using CloudNative.CloudEvents.SystemTextJson;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
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
using Xunit.Abstractions;

namespace Motor.Extensions.Hosting.Kafka_IntegrationTest;

[Collection("KafkaMessage")]
public class KafkaExtensionTests : IClassFixture<KafkaFixture>
{
    private readonly ITestOutputHelper _output;
    private readonly KafkaFixture _fixture;
    private readonly Mock<IHostApplicationLifetime> _fakeLifetimeMock = new Mock<IHostApplicationLifetime>();
    private readonly string _topic;
    private const string Message = "message";
    private readonly byte[] _expectedMessage = Encoding.UTF8.GetBytes(Message);
    private readonly Channel<byte[]> _consumedChannel = Channel.CreateUnbounded<byte[]>();

    public KafkaExtensionTests(ITestOutputHelper output, KafkaFixture fixture)
    {
        _output = output;
        _fixture = fixture;
        var randomizer = RandomizerFactory.GetRandomizer(new FieldOptionsTextRegex { Pattern = @"^[A-Z]{10}" });
        _topic = randomizer.Generate();
    }

    [Fact(Timeout = 50000)]
    public async Task StopAsync_AlreadyDisposed_NoException()
    {
        var consumer = GetConsumer<string>(_topic);
        consumer.ConsumeCallbackAsync = async (_, _) => await Task.FromResult(ProcessedMessageStatus.Success);
        await consumer.StartAsync();
        _ = consumer.ExecuteAsync();
        consumer.Dispose();

        await consumer.StopAsync();
    }

    [Fact(Timeout = 50000)]
    public async Task StopAsync_CalledTwice_Idempotent()
    {
        var consumer = GetConsumer<string>(_topic);
        consumer.ConsumeCallbackAsync = async (_, _) => await Task.FromResult(ProcessedMessageStatus.Success);
        await consumer.StartAsync();
        _ = consumer.ExecuteAsync();
        await consumer.StopAsync();

        await consumer.StopAsync();
    }

    [Fact(Timeout = 50000)]
    public async Task Consume_RawPublishIntoKafkaAndConsumeCreateCloudEvent_ConsumedEqualsPublished()
    {
        const string message = "testMessage";
        await PublishMessage(_topic, "someKey", message);
        var consumer = GetConsumer<string>(_topic);
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
        await consumer.StopAsync();

        Assert.Equal(message, Encoding.UTF8.GetString(rawConsumedKafkaMessage));
    }

    [Fact(Timeout = 50000)]
    public async Task Consume_PublishIntoKafkaAndConsumeWithCloudEvent_ConsumedEqualsPublished()
    {
        var publisher = GetPublisher<byte[]>(_topic);
        var motorCloudEvent =
            MotorCloudEvent.CreateTestCloudEvent(Message).CreateNew(Encoding.UTF8.GetBytes(Message));
        await publisher.PublishMessageAsync(motorCloudEvent, CancellationToken.None);
        var consumer = GetConsumer<byte[]>(_topic);
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
        await consumer.StopAsync();

        Assert.Equal(motorCloudEvent.Id, id);
    }

    [Fact(Timeout = 50000)]
    public async Task Consume_PublishIntoExtensionDefinedTopic_ConsumedEqualsPublished()
    {
        var publisher = GetPublisher<byte[]>("wrong_topic");
        var motorCloudEvent =
            MotorCloudEvent.CreateTestCloudEvent(Message).CreateNew(Encoding.UTF8.GetBytes(Message));
        motorCloudEvent.SetKafkaTopic(_topic);
        await publisher.PublishMessageAsync(motorCloudEvent, CancellationToken.None);
        var consumer = GetConsumer<byte[]>(_topic);
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
        await consumer.StopAsync();

        Assert.Equal(motorCloudEvent.Id, id);
    }

    [Fact(Timeout = 50000)]
    public async Task Consume_LimitMaxConcurrentMessages_StartProcessingLimitedNumberOfMessagesSimultaneously()
    {
        const int maxConcurrentMessages = 5;
        var taskCompletionSource = new TaskCompletionSource();
        for (var i = 0; i < maxConcurrentMessages * 2; i++)
        {
            await PublishMessage(_topic, "someKey", Message);
        }

        var config = GetConsumerConfig<string>(_topic, maxConcurrentMessages);
        var consumer = GetConsumer(_topic, config);
        var numberOfStartedMessages = 0;
        consumer.ConsumeCallbackAsync = async (_, cancellationToken) =>
        {
            numberOfStartedMessages++;
            taskCompletionSource.TrySetResult();
            await Task.Delay(-1, cancellationToken); // Wait indefinitely
            return await Task.FromResult(ProcessedMessageStatus.Success);
        };

        await consumer.StartAsync();
        consumer.ExecuteAsync();
        // Wait until processing begins
        await taskCompletionSource.Task;
        // Give consumer enough time to process further messages
        await Task.Delay(TimeSpan.FromSeconds(1));

        Assert.Equal(maxConcurrentMessages, numberOfStartedMessages);
        await consumer.StopAsync();
    }

    [Theory(Timeout = 50000)]
    [InlineData(ProcessedMessageStatus.TemporaryFailure)]
    [InlineData(ProcessedMessageStatus.CriticalFailure)]
    public async Task Consume_SynchronousMessageHandlingWhereProcessingFailed_DoesNotProcessSecondMessage(
        ProcessedMessageStatus returnStatus)
    {
        var taskCompletionSource = new TaskCompletionSource();
        await PublishMessage(_topic, "someKey", "1");
        await PublishMessage(_topic, "someKey", "2");
        var config = GetConsumerConfig<string>(_topic, maxConcurrentMessages: 1);
        var consumer = GetConsumer(_topic, config);
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
        await consumer.StopAsync();

        Assert.Single(distinctHandledMessages);
    }

    [Theory(Timeout = 50000)]
    [InlineData(ProcessedMessageStatus.Success)]
    [InlineData(ProcessedMessageStatus.Failure)]
    [InlineData(ProcessedMessageStatus.InvalidInput)]
    public async Task Consume_SynchronousMessageHandlingWithMultipleMessages_AllMessagesProcessed(
        ProcessedMessageStatus processedMessageStatus)
    {
        const int numMessages = 10;
        var taskCompletionSource = new TaskCompletionSource();
        var messages = Enumerable.Range(1, numMessages).Select(i => $"{i}").ToHashSet();
        foreach (var message in messages)
        {
            await PublishMessage(_topic, "someKey", message);
        }

        var config = GetConsumerConfig<string>(_topic, maxConcurrentMessages: 1);
        var consumer = GetConsumer(_topic, config);
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
        await consumer.StopAsync();

        Assert.True(messages.SetEquals(distinctHandledMessages));
    }

    [Fact(Timeout = 50000)]
    public async Task Consume_TemporaryFailure_ExecuteTheConfiguredNumberOfRetries()
    {
        const int expectedNumberOfRetries = 2;
        var taskCompletionSource = new TaskCompletionSource();
        await PublishMessage(_topic, "someKey", Message);
        var config = GetConsumerConfig<string>(_topic, retriesOnTemporaryFailure: expectedNumberOfRetries);
        var consumer = GetConsumer(_topic, config);
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
        await consumer.StopAsync();

        Assert.Equal(expectedNumberOfRetries + 1, actualNumberOfTries);
    }

    [Fact(Timeout = 50000)]
    public async Task Consume_TemporaryFailureEvenAfterRetries_ApplicationIsStopped()
    {
        const int numberOfRetries = 2;
        var taskCompletionSource = new TaskCompletionSource();
        await PublishMessage(_topic, "someKey", Message);
        var config = GetConsumerConfig<string>(_topic, retriesOnTemporaryFailure: numberOfRetries);
        var consumer = GetConsumer(_topic, config, _fakeLifetimeMock.Object);
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
        await consumer.StopAsync();

        _fakeLifetimeMock.Verify(mock => mock.StopApplication(), Times.Once);
    }

    [Fact(Timeout = 50000)]
    public async Task Consume_CriticalFailure_ApplicationIsStopped()
    {
        var taskCompletionSource = new TaskCompletionSource();
        await PublishMessage(_topic, "someKey", Message);
        var config = GetConsumerConfig<string>(_topic);
        var consumer = GetConsumer(_topic, config, _fakeLifetimeMock.Object);
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
        await consumer.StopAsync();

        _fakeLifetimeMock.Verify(mock => mock.StopApplication(), Times.Once);
    }

    [Fact(Timeout = 50000)]
    public async Task Consume_AfterProcessingAMessage_CommitsEveryCommitPeriod()
    {
        var config = GetConsumerConfig<string>(_topic, maxConcurrentMessages: 1, retriesOnTemporaryFailure: 1);
        config.CommitPeriod = 2;
        config.AutoCommitIntervalMs = null;
        using var consumer = GetConsumer(_topic, config, _fakeLifetimeMock.Object);
        consumer.ConsumeCallbackAsync = CreateConsumeCallback(ProcessedMessageStatus.Success, _consumedChannel);
        var cts = new CancellationTokenSource();
        await consumer.StartAsync(cts.Token);
        var execution = consumer.ExecuteAsync(cts.Token);

        var numberOfProcessedMessages = 2;
        await PublishAndAwaitMessages(_consumedChannel, numberOfProcessedMessages);

        await WaitForCommittedOffset(consumer, numberOfProcessedMessages);
        cts.Cancel();
        await execution;
        await consumer.StopAsync();
    }

    private async Task PublishAndAwaitMessages(Channel<byte[]> channel, int count)
    {
        for (var i = 0; i < count; i++)
        {
            await PublishMessage(_topic, "someKey", Message);
        }

        for (var i = 0; i < count; i++)
        {
            Assert.Equal(_expectedMessage, await channel.Reader.ReadAsync(CancellationToken.None));
        }
    }

    /// <param name="consumer">Consumer to get offset from</param>
    /// <param name="expectedOffset">Offset of last committed message + 1, starts at 0</param>
    private async Task WaitForCommittedOffset<TData>(KafkaMessageConsumer<TData> consumer, long expectedOffset)
    {
        while (true)
        {
            var offset = GetCommittedOffset(consumer);

            _output.WriteLine($"Waiting for offset {expectedOffset} got {offset}");
            if (offset == expectedOffset)
            {
                return;
            }

            await Task.Delay(10, CancellationToken.None);
        }
    }

    private Offset? GetCommittedOffset<TData>(KafkaMessageConsumer<TData> consumer)
    {
        var offsets = consumer.Committed();
        return offsets.FirstOrDefault()?.Offset;
    }

    [Fact(Timeout = 50000)]
    public async Task Consume_AfterProcessingAMessage_CommitsOnlyEveryCommitPeriod()
    {
        var config = GetConsumerConfig<string>(_topic, maxConcurrentMessages: 1, retriesOnTemporaryFailure: 1);
        config.CommitPeriod = 10;
        config.AutoCommitIntervalMs = null;
        using var consumer = GetConsumer(_topic, config, _fakeLifetimeMock.Object);
        consumer.ConsumeCallbackAsync = CreateConsumeCallback(ProcessedMessageStatus.Success, _consumedChannel);
        var cts = new CancellationTokenSource();
        await consumer.StartAsync(cts.Token);
        var execution = consumer.ExecuteAsync(cts.Token);

        var numberOfProcessedMessages = 9;
        await PublishAndAwaitMessages(_consumedChannel, numberOfProcessedMessages);

        await Task.Delay(5000, CancellationToken.None);
        Assert.Equal(Offset.Unset, GetCommittedOffset(consumer));
        cts.Cancel();
        await execution;
        await consumer.StopAsync();
    }

    [Fact(Timeout = 50000)]
    public async Task Consume_WhenHavingUncommittedMessages_CommitsEveryAutoCommitIntervalMs()
    {
        var config = GetConsumerConfig<string>(_topic, maxConcurrentMessages: 1, retriesOnTemporaryFailure: 1);
        config.CommitPeriod = 1000; // default
        config.AutoCommitIntervalMs = 1;
        using var consumer = GetConsumer(_topic, config, _fakeLifetimeMock.Object);
        consumer.ConsumeCallbackAsync = CreateConsumeCallback(ProcessedMessageStatus.Success, _consumedChannel);
        var cts = new CancellationTokenSource();
        await consumer.StartAsync(cts.Token);
        var execution = consumer.ExecuteAsync(cts.Token);

        var numberOfProcessedMessages = 2;
        await PublishAndAwaitMessages(_consumedChannel, numberOfProcessedMessages);

        await WaitForCommittedOffset(consumer, numberOfProcessedMessages);
        cts.Cancel();
        await execution;
        await consumer.StopAsync();
    }

    [Fact(Timeout = 50000)]
    public async Task Consume_OnShutdown_Commits()
    {
        var config = GetConsumerConfig<string>(_topic, maxConcurrentMessages: 1, retriesOnTemporaryFailure: 1);
        config.AutoCommitIntervalMs = null;
        using var consumer = GetConsumer(_topic, config, _fakeLifetimeMock.Object);
        consumer.ConsumeCallbackAsync = CreateConsumeCallback(ProcessedMessageStatus.Success, _consumedChannel);
        var cts = new CancellationTokenSource();
        await consumer.StartAsync(cts.Token);
        var execution = consumer.ExecuteAsync(cts.Token);

        var numberOfProcessedMessages = 2;
        await PublishAndAwaitMessages(_consumedChannel, numberOfProcessedMessages);
        // Publish another message, that blocks in processing.
        // This makes sure that the first 2 messages have been processed and have been written to the commit channel.
        consumer.ConsumeCallbackAsync = CreateBlockingCallback(_consumedChannel);
        await PublishAndAwaitMessages(_consumedChannel, 1);
        cts.Cancel();
        await execution;

        await WaitForCommittedOffset(consumer, numberOfProcessedMessages);
        await consumer.StopAsync();
    }

    private Func<MotorCloudEvent<byte[]>, CancellationToken, Task<ProcessedMessageStatus>> CreateConsumeCallback(
        ProcessedMessageStatus statusToReturn, Channel<byte[]> channel) => async (data, _) =>
    {
        _output.WriteLine($"Processed message with status {statusToReturn.ToString()}");
        await channel.Writer.WriteAsync(data.TypedData, CancellationToken.None);
        return await Task.FromResult(statusToReturn);
    };

    private Func<MotorCloudEvent<byte[]>, CancellationToken, Task<ProcessedMessageStatus>> CreateBlockingCallback(
        Channel<byte[]> channel) => async (data, cancellationToken) =>
    {
        _output.WriteLine($"Blocking message");
        await channel.Writer.WriteAsync(data.TypedData, CancellationToken.None);
        await Task.Delay(-1, cancellationToken);
        return ProcessedMessageStatus.Success;
    };

    private async Task PublishMessage(string topic, string key, string value)
    {
        using var producer = new ProducerBuilder<string, byte[]>(GetPublisherConfig<string>(topic)).Build();
        await producer.ProduceAsync(topic,
            new Message<string, byte[]> { Key = key, Value = Encoding.UTF8.GetBytes(value) });
        producer.Flush();
    }

    private KafkaMessageConsumer<T> GetConsumer<T>(string topic, KafkaConsumerOptions<T> config = null,
        IHostApplicationLifetime fakeLifetimeMock = null)
    {
        var options = Options.Create(config ?? GetConsumerConfig<T>(topic));
        var logger = _output.BuildLoggerFor<KafkaMessageConsumer<T>>();
        fakeLifetimeMock ??= Mock.Of<IHostApplicationLifetime>();
        return new KafkaMessageConsumer<T>(logger, options, fakeLifetimeMock, null, GetApplicationNameService(),
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
