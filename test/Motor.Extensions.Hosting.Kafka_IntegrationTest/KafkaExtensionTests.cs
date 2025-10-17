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
using Polly;
using RandomDataGenerator.FieldOptions;
using RandomDataGenerator.Randomizers;
using Xunit;
using Xunit.Abstractions;

namespace Motor.Extensions.Hosting.Kafka_IntegrationTest;

[Collection("KafkaMessage")]
public class KafkaExtensionTests(ITestOutputHelper output, KafkaFixture fixture) : IClassFixture<KafkaFixture>
{
    private const string Message = "message";
    private readonly byte[] _expectedMessage = Encoding.UTF8.GetBytes(Message);
    private readonly Channel<byte[]> _consumedChannel = Channel.CreateUnbounded<byte[]>();
    private readonly IRandomizerString _topicRandomizer =
        RandomizerFactory.GetRandomizer(new FieldOptionsTextRegex { Pattern = "^[A-Z]{10}" });
    private string NewTopic() => _topicRandomizer.Generate();

    [Fact(Timeout = 50000)]
    public async Task StopAsync_AlreadyDisposed_NoException()
    {
        var consumer = GetConsumer<string>(NewTopic());
        consumer.ConsumeCallbackAsync = async (_, _) => await Task.FromResult(ProcessedMessageStatus.Success);
        await consumer.StartAsync();
        _ = consumer.ExecuteAsync();
        consumer.Dispose();

        await consumer.StopAsync();
    }

    [Fact(Timeout = 50000)]
    public async Task StopAsync_CalledTwice_Idempotent()
    {
        var consumer = GetConsumer<string>(NewTopic());
        consumer.ConsumeCallbackAsync = async (_, _) => await Task.FromResult(ProcessedMessageStatus.Success);
        await consumer.StartAsync();
        _ = consumer.ExecuteAsync();
        await consumer.StopAsync();

        await consumer.StopAsync();
    }

    [Fact(Timeout = 50000)]
    public async Task Consume_RawPublishIntoKafkaAndConsumeCreateCloudEvent_ConsumedEqualsPublished()
    {
        var topic = NewTopic();
        const string message = "testMessage";
        await PublishMessage(topic, "someKey", message);
        var consumer = GetConsumer<string>(topic);
        byte[] rawConsumedKafkaMessage = null;
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
        var topic = NewTopic();
        var publisher = GetPublisher<byte[]>(topic);
        var motorCloudEvent =
            MotorCloudEvent.CreateTestCloudEvent(Message).CreateNew(Encoding.UTF8.GetBytes(Message));
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
        await consumer.StopAsync();

        Assert.Equal(motorCloudEvent.Id, id);
    }

    [Fact(Timeout = 50000)]
    public async Task Consume_PublishIntoExtensionDefinedTopic_ConsumedEqualsPublished()
    {
        var topic = NewTopic();
        var publisher = GetPublisher<byte[]>("wrong_topic");
        var motorCloudEvent =
            MotorCloudEvent.CreateTestCloudEvent(Message).CreateNew(Encoding.UTF8.GetBytes(Message));
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
        await consumer.StopAsync();

        Assert.Equal(motorCloudEvent.Id, id);
    }

    [Fact(Timeout = 50000)]
    public async Task Consume_LimitMaxConcurrentMessages_StartProcessingLimitedNumberOfMessagesSimultaneously()
    {
        var topic = NewTopic();
        const int maxConcurrentMessages = 5;
        var taskCompletionSource = new TaskCompletionSource();
        for (var i = 0; i < maxConcurrentMessages * 2; i++)
        {
            await PublishMessage(topic, "someKey", Message);
        }

        var config = GetConsumerConfig<string>(topic, maxConcurrentMessages);
        var consumer = GetConsumer(topic, config);
        var numberOfStartedMessages = 0;
        var lockObject = new object();
        consumer.ConsumeCallbackAsync = async (_, cancellationToken) =>
        {
            lock (lockObject)
            {
                numberOfStartedMessages++;
                taskCompletionSource.TrySetResult();
            }

            await Task.Delay(-1, cancellationToken); // Wait indefinitely
            return await Task.FromResult(ProcessedMessageStatus.Success);
        };

        await consumer.StartAsync();
        var executionTask = consumer.ExecuteAsync();
        // Wait until processing begins
        await taskCompletionSource.Task;
        // Give consumer enough time to process further messages that would violate the limit
        await Task.Delay(TimeSpan.FromSeconds(1));

        Assert.Equal(maxConcurrentMessages, numberOfStartedMessages);
        await consumer.StopAsync();
        await executionTask;
    }

    [Theory(Timeout = 50000)]
    [InlineData(ProcessedMessageStatus.TemporaryFailure)]
    [InlineData(ProcessedMessageStatus.CriticalFailure)]
    public async Task Consume_SynchronousMessageHandlingWhereProcessingFailed_DoesNotProcessSecondMessage(
        ProcessedMessageStatus returnStatus)
    {
        var topic = NewTopic();
        var taskCompletionSource = new TaskCompletionSource();
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
        var executionTask = consumer.ExecuteAsync();
        // Wait until processing begins
        await taskCompletionSource.Task;
        // Give consumer enough time to process further messages that would violate the limit
        await Task.Delay(TimeSpan.FromSeconds(1));
        await consumer.StopAsync();
        await executionTask;

        Assert.Single(distinctHandledMessages);
    }

    [Theory(Timeout = 50000)]
    [InlineData(ProcessedMessageStatus.Success)]
    [InlineData(ProcessedMessageStatus.Failure)]
    [InlineData(ProcessedMessageStatus.InvalidInput)]
    public async Task Consume_SynchronousMessageHandlingWithMultipleMessages_AllMessagesProcessed(
        ProcessedMessageStatus processedMessageStatus)
    {
        var topic = NewTopic();
        const int numMessages = 10;
        var taskCompletionSource = new TaskCompletionSource();
        var messages = Enumerable.Range(1, numMessages).Select(i => $"{i}").ToHashSet();
        foreach (var message in messages)
        {
            await PublishMessage(topic, "someKey", message);
        }

        var config = GetConsumerConfig<string>(topic, maxConcurrentMessages: 1);
        config.CommitPeriod = 1;
        config.AutoCommitIntervalMs = null;
        var consumer = GetConsumer(topic, config);
        var distinctHandledMessages = new HashSet<string>();
        var lockObject = new object();
        consumer.ConsumeCallbackAsync = async (data, _) =>
        {
            lock (lockObject)
            {
                distinctHandledMessages.Add(Encoding.UTF8.GetString(data.TypedData));
                if (distinctHandledMessages.Count == numMessages)
                {
                    taskCompletionSource.TrySetResult();
                }
            }

            return await Task.FromResult(processedMessageStatus);
        };

        await consumer.StartAsync();
        var executionTask = consumer.ExecuteAsync();
        // Wait until last message is processed
        await WaitForCommittedOffset(consumer, numMessages);
        await consumer.StopAsync();
        await executionTask;

        Assert.True(messages.SetEquals(distinctHandledMessages));
    }

    [Fact(Timeout = 50000)]
    public async Task Consume_TemporaryFailure_ExecuteTheConfiguredNumberOfRetries()
    {
        var topic = NewTopic();
        const int expectedNumberOfRetries = 2;
        var retryBasePeriod = TimeSpan.FromMilliseconds(10);
        var taskCompletionSource = new TaskCompletionSource();
        await PublishMessage(topic, "someKey", Message);
        var config = GetConsumerConfig<string>(
            topic, retriesOnTemporaryFailure: expectedNumberOfRetries, retryBasePeriod: retryBasePeriod);
        var consumer = GetConsumer(topic, config);
        var actualNumberOfTries = 0;
        consumer.ConsumeCallbackAsync = async (_, _) =>
        {
            taskCompletionSource.TrySetResult();
            actualNumberOfTries += 1;
            return await Task.FromResult(ProcessedMessageStatus.TemporaryFailure);
        };

        await consumer.StartAsync();
        var executionTask = consumer.ExecuteAsync();
        // Wait until processing begins
        await taskCompletionSource.Task;
        // Give consumer enough time to handle returned ProcessedMessageStatus
        await Task.Delay(2 * retryBasePeriod * Math.Pow(2, expectedNumberOfRetries));
        await consumer.StopAsync();
        await executionTask;

        Assert.Equal(expectedNumberOfRetries + 1, actualNumberOfTries);
    }

    [Fact(Timeout = 50000)]
    public async Task Consume_TemporaryFailureEvenAfterRetries_ApplicationIsStopped()
    {
        var topic = NewTopic();
        var fakeLifetimeMock = new Mock<IHostApplicationLifetime>();
        const int numberOfRetries = 2;
        var retryBasePeriod = TimeSpan.FromMilliseconds(10);
        await PublishMessage(topic, "someKey", Message);
        var config = GetConsumerConfig<string>(
            topic, retriesOnTemporaryFailure: numberOfRetries, retryBasePeriod: retryBasePeriod);
        var consumer = GetConsumer(topic, config, fakeLifetimeMock.Object);
        consumer.ConsumeCallbackAsync = async (_, _) => await Task.FromResult(ProcessedMessageStatus.TemporaryFailure);

        await consumer.StartAsync();
        var executionTask = consumer.ExecuteAsync();

        WaitUntil(() => fakeLifetimeMock.Verify(mock => mock.StopApplication()));
        await consumer.StopAsync();
        await executionTask;
    }

    [Fact(Timeout = 50000)]
    public async Task Consume_CriticalFailure_ApplicationIsStopped()
    {
        var topic = NewTopic();
        var fakeLifetimeMock = new Mock<IHostApplicationLifetime>();
        await PublishMessage(topic, "someKey", Message);
        var config = GetConsumerConfig<string>(topic);
        var consumer = GetConsumer(topic, config, fakeLifetimeMock.Object);
        consumer.ConsumeCallbackAsync = async (_, _) => await Task.FromResult(ProcessedMessageStatus.CriticalFailure);

        await consumer.StartAsync();
        var executionTask = consumer.ExecuteAsync();

        WaitUntil(() => fakeLifetimeMock.Verify(mock => mock.StopApplication()));
        await consumer.StopAsync();
        await executionTask;
    }

    [Fact(Timeout = 50000)]
    public async Task Consume_AfterProcessingAMessage_CommitsEveryCommitPeriod()
    {
        var topic = NewTopic();
        var fakeLifetimeMock = new Mock<IHostApplicationLifetime>();
        var config = GetConsumerConfig<string>(topic, maxConcurrentMessages: 1, retriesOnTemporaryFailure: 1);
        config.CommitPeriod = 2;
        config.AutoCommitIntervalMs = null;
        using var consumer = GetConsumer(topic, config, fakeLifetimeMock.Object);
        consumer.ConsumeCallbackAsync = CreateConsumeCallback(ProcessedMessageStatus.Success, _consumedChannel);
        var cts = new CancellationTokenSource();
        await consumer.StartAsync(cts.Token);
        var execution = consumer.ExecuteAsync(cts.Token);

        const int numberOfProcessedMessages = 2;
        await PublishAndAwaitMessages(topic, _consumedChannel, numberOfProcessedMessages);

        await WaitForCommittedOffset(consumer, numberOfProcessedMessages);
        await cts.CancelAsync();
        await execution;
        await consumer.StopAsync(CancellationToken.None);
    }

    private async Task PublishAndAwaitMessages(string topic, Channel<byte[]> channel, int count)
    {
        for (var i = 0; i < count; i++)
        {
            await PublishMessage(topic, "someKey", Message);
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

            output.WriteLine($"Waiting for offset {expectedOffset} got {offset}");
            if (offset == expectedOffset)
            {
                return;
            }

            await Task.Delay(10, CancellationToken.None);
        }
    }

    private static Offset? GetCommittedOffset<TData>(KafkaMessageConsumer<TData> consumer)
    {
        var offsets = consumer.Committed();
        return offsets.FirstOrDefault()?.Offset;
    }

    [Fact(Timeout = 50000)]
    public async Task Consume_AfterProcessingAMessage_CommitsOnlyEveryCommitPeriod()
    {
        var topic = NewTopic();
        var fakeLifetimeMock = new Mock<IHostApplicationLifetime>();
        var config = GetConsumerConfig<string>(topic, maxConcurrentMessages: 1, retriesOnTemporaryFailure: 1);
        config.CommitPeriod = 10;
        config.AutoCommitIntervalMs = null;
        using var consumer = GetConsumer(topic, config, fakeLifetimeMock.Object);
        consumer.ConsumeCallbackAsync = CreateConsumeCallback(ProcessedMessageStatus.Success, _consumedChannel);
        var cts = new CancellationTokenSource();
        await consumer.StartAsync(cts.Token);
        var execution = consumer.ExecuteAsync(cts.Token);

        const int numberOfProcessedMessages = 9;
        await PublishAndAwaitMessages(topic, _consumedChannel, numberOfProcessedMessages);

        Assert.Equal(Offset.Unset, GetCommittedOffset(consumer));
        await cts.CancelAsync();
        await execution;
        await consumer.StopAsync(CancellationToken.None);
    }

    [Fact(Timeout = 50000)]
    public async Task Consume_WhenHavingUncommittedMessages_CommitsEveryAutoCommitIntervalMs()
    {
        var topic = NewTopic();
        var fakeLifetimeMock = new Mock<IHostApplicationLifetime>();
        var config = GetConsumerConfig<string>(topic, maxConcurrentMessages: 1, retriesOnTemporaryFailure: 1);
        config.CommitPeriod = 1000; // default
        config.AutoCommitIntervalMs = 1;
        using var consumer = GetConsumer(topic, config, fakeLifetimeMock.Object);
        consumer.ConsumeCallbackAsync = CreateConsumeCallback(ProcessedMessageStatus.Success, _consumedChannel);
        var cts = new CancellationTokenSource();
        await consumer.StartAsync(cts.Token);
        var executionTask = consumer.ExecuteAsync(cts.Token);

        const int numberOfProcessedMessages = 2;
        await PublishAndAwaitMessages(topic, _consumedChannel, numberOfProcessedMessages);

        await WaitForCommittedOffset(consumer, numberOfProcessedMessages);
        await cts.CancelAsync();
        await executionTask;
        await consumer.StopAsync(CancellationToken.None);
    }

    [Fact(Timeout = 50000)]
    public async Task Consume_OnShutdown_Commits()
    {
        var topic = NewTopic();
        var fakeLifetimeMock = new Mock<IHostApplicationLifetime>();
        var config = GetConsumerConfig<string>(topic, maxConcurrentMessages: 1, retriesOnTemporaryFailure: 1);
        config.AutoCommitIntervalMs = null;
        using var consumer = GetConsumer(topic, config, fakeLifetimeMock.Object);
        consumer.ConsumeCallbackAsync = CreateConsumeCallback(ProcessedMessageStatus.Success, _consumedChannel);
        var cts = new CancellationTokenSource();
        await consumer.StartAsync(cts.Token);
        var executionTask = consumer.ExecuteAsync(cts.Token);

        const int numberOfProcessedMessages = 2;
        await PublishAndAwaitMessages(topic, _consumedChannel, numberOfProcessedMessages);
        // Publish another message, that blocks in processing.
        // This makes sure that the first 2 messages have been processed and have been written to the commit channel.
        consumer.ConsumeCallbackAsync = CreateBlockingCallback(_consumedChannel);
        await PublishAndAwaitMessages(topic, _consumedChannel, 1);
        await cts.CancelAsync();
        await executionTask;

        await WaitForCommittedOffset(consumer, numberOfProcessedMessages);
        await consumer.StopAsync(CancellationToken.None);
    }

    private Func<MotorCloudEvent<byte[]>, CancellationToken, Task<ProcessedMessageStatus>> CreateConsumeCallback(
        ProcessedMessageStatus statusToReturn, Channel<byte[]> channel) => async (data, _) =>
    {
        output.WriteLine($"Processed message with status {statusToReturn.ToString()}");
        await channel.Writer.WriteAsync(data.TypedData, CancellationToken.None);
        return await Task.FromResult(statusToReturn);
    };

    private Func<MotorCloudEvent<byte[]>, CancellationToken, Task<ProcessedMessageStatus>> CreateBlockingCallback(
        Channel<byte[]> channel) => async (data, cancellationToken) =>
    {
        output.WriteLine("Blocking message");
        await channel.Writer.WriteAsync(data.TypedData, CancellationToken.None);
        await Task.Delay(-1, cancellationToken);
        return ProcessedMessageStatus.Success;
    };

    private static void WaitUntil(Action action) => Policy.Handle<Exception>().RetryForever().Execute(action);

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
        var logger = output.BuildLoggerFor<KafkaMessageConsumer<T>>();
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
        return new KafkaPublisherOptions<T>
        {
            Topic = topic,
            BootstrapServers = fixture.BootstrapServers,
        };
    }

    private static IApplicationNameService GetApplicationNameService(string source = "test://non")
    {
        var mock = new Mock<IApplicationNameService>();
        mock.Setup(t => t.GetSource()).Returns(new Uri(source));
        return mock.Object;
    }

    private KafkaConsumerOptions<T> GetConsumerConfig<T>(string topic, int maxConcurrentMessages = 1000,
        string groupId = "group_id", int retriesOnTemporaryFailure = 10, TimeSpan? retryBasePeriod = null)
    {
        return new KafkaConsumerOptions<T>
        {
            Topic = topic,
            GroupId = groupId,
            CommitPeriod = 1000,
            BootstrapServers = fixture.BootstrapServers,
            EnableAutoCommit = false,
            StatisticsIntervalMs = 5000,
            SessionTimeoutMs = 6000,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            MaxConcurrentMessages = maxConcurrentMessages,
            RetriesOnTemporaryFailure = retriesOnTemporaryFailure,
            RetryBasePeriod = retryBasePeriod ?? TimeSpan.FromSeconds(1)
        };
    }
}
