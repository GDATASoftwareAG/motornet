using System.Collections.Concurrent;
using System.Text;
using System.Threading.Channels;
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
    private readonly IRandomizerString _topicRandomizer = RandomizerFactory.GetRandomizer(
        new FieldOptionsTextRegex { Pattern = "^[A-Z]{10}" }
    );

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
        var motorCloudEvent = MotorCloudEvent.CreateTestCloudEvent(Message).CreateNew(Encoding.UTF8.GetBytes(Message));
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
        var motorCloudEvent = MotorCloudEvent.CreateTestCloudEvent(Message).CreateNew(Encoding.UTF8.GetBytes(Message));
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
        ProcessedMessageStatus returnStatus
    )
    {
        var topic = NewTopic();
        var taskCompletionSource = new TaskCompletionSource();
        await PublishMessage(topic, "someKey", "1");
        await PublishMessage(topic, "someKey", "2");
        var config = GetConsumerConfig<string>(topic, maxConcurrentMessagesPerPartition: 1);
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
        ProcessedMessageStatus processedMessageStatus
    )
    {
        var topic = NewTopic();
        const int numMessages = 10;
        var taskCompletionSource = new TaskCompletionSource();
        var messages = Enumerable.Range(1, numMessages).Select(i => $"{i}").ToHashSet();
        foreach (var message in messages)
        {
            await PublishMessage(topic, "someKey", message);
        }

        var config = GetConsumerConfig<string>(topic, maxConcurrentMessagesPerPartition: 1);
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
            topic,
            retriesOnTemporaryFailure: expectedNumberOfRetries,
            retryBasePeriod: retryBasePeriod
        );
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
            topic,
            retriesOnTemporaryFailure: numberOfRetries,
            retryBasePeriod: retryBasePeriod
        );
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
        var config = GetConsumerConfig<string>(
            topic,
            maxConcurrentMessagesPerPartition: 1,
            retriesOnTemporaryFailure: 1
        );
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
        var config = GetConsumerConfig<string>(
            topic,
            maxConcurrentMessagesPerPartition: 1,
            retriesOnTemporaryFailure: 1
        );
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
        var config = GetConsumerConfig<string>(
            topic,
            maxConcurrentMessagesPerPartition: 1,
            retriesOnTemporaryFailure: 1
        );
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
        var config = GetConsumerConfig<string>(
            topic,
            maxConcurrentMessagesPerPartition: 1,
            retriesOnTemporaryFailure: 1
        );
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
        ProcessedMessageStatus statusToReturn,
        Channel<byte[]> channel
    ) =>
        async (data, _) =>
        {
            output.WriteLine($"Processed message with status {statusToReturn.ToString()}");
            await channel.Writer.WriteAsync(data.TypedData, CancellationToken.None);
            return await Task.FromResult(statusToReturn);
        };

    private Func<MotorCloudEvent<byte[]>, CancellationToken, Task<ProcessedMessageStatus>> CreateBlockingCallback(
        Channel<byte[]> channel
    ) =>
        async (data, cancellationToken) =>
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
        await producer.ProduceAsync(
            topic,
            new Message<string, byte[]> { Key = key, Value = Encoding.UTF8.GetBytes(value) }
        );
        producer.Flush();
    }

    private async Task PublishMessageToPartition(string topic, int partition, string value)
    {
        using var producer = new ProducerBuilder<Null, byte[]>(
            new ProducerConfig { BootstrapServers = fixture.BootstrapServers }
        ).Build();
        await producer.ProduceAsync(
            new TopicPartition(topic, new Partition(partition)),
            new Message<Null, byte[]> { Value = Encoding.UTF8.GetBytes(value) }
        );
        producer.Flush();
    }

    private async Task<string> CreateMultiPartitionTopic(int numPartitions)
    {
        var topic = NewTopic();
        await fixture.CreateTopicAsync(topic, numPartitions);
        return topic;
    }

    private KafkaMessageConsumer<T> GetConsumer<T>(
        string topic,
        KafkaConsumerOptions<T> config = null,
        IHostApplicationLifetime fakeLifetimeMock = null
    )
    {
        var options = Options.Create(config ?? GetConsumerConfig<T>(topic));
        var logger = output.BuildLoggerFor<KafkaMessageConsumer<T>>();
        fakeLifetimeMock ??= Mock.Of<IHostApplicationLifetime>();
        return new KafkaMessageConsumer<T>(
            logger,
            options,
            fakeLifetimeMock,
            null,
            GetApplicationNameService(),
            new JsonEventFormatter()
        );
    }

    private KafkaMessagePublisher<T> GetPublisher<T>(string topic)
    {
        var options = Options.Create(GetPublisherConfig<T>(topic));
        var publisherOptions = Options.Create(new PublisherOptions());
        return new KafkaMessagePublisher<T>(
            options,
            new JsonEventFormatter(),
            publisherOptions,
            Mock.Of<ILogger<KafkaMessagePublisher<T>>>()
        );
    }

    private KafkaPublisherOptions<T> GetPublisherConfig<T>(string topic)
    {
        return new KafkaPublisherOptions<T> { Topic = topic, BootstrapServers = fixture.BootstrapServers };
    }

    private static IApplicationNameService GetApplicationNameService(string source = "test://non")
    {
        var mock = new Mock<IApplicationNameService>();
        mock.Setup(t => t.GetSource()).Returns(new Uri(source));
        return mock.Object;
    }

    private KafkaConsumerOptions<T> GetConsumerConfig<T>(
        string topic,
        int maxConcurrentMessagesPerPartition = 1000,
        string groupId = "group_id",
        int retriesOnTemporaryFailure = 10,
        TimeSpan? retryBasePeriod = null
    )
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
            MaxConcurrentMessagesPerPartition = maxConcurrentMessagesPerPartition,
            RetriesOnTemporaryFailure = retriesOnTemporaryFailure,
            RetryBasePeriod = retryBasePeriod ?? TimeSpan.FromSeconds(1),
        };
    }

    #region Per-partition fair processing tests

    [Fact(Timeout = 50000)]
    public async Task Consume_MultiPartitionTopic_AllPartitionsProcessed()
    {
        const int numPartitions = 3;
        const int messagesPerPartition = 5;
        var topic = await CreateMultiPartitionTopic(numPartitions);

        for (var p = 0; p < numPartitions; p++)
        {
            for (var m = 0; m < messagesPerPartition; m++)
            {
                await PublishMessageToPartition(topic, p, $"p{p}-m{m}");
            }
        }

        var config = GetConsumerConfig<string>(topic, maxConcurrentMessagesPerPartition: messagesPerPartition);
        config.CommitPeriod = 1;
        config.AutoCommitIntervalMs = 1;
        var consumer = GetConsumer(topic, config);
        var processedMessages = new ConcurrentBag<string>();
        var allProcessed = new TaskCompletionSource();
        const int totalMessages = numPartitions * messagesPerPartition;
        consumer.ConsumeCallbackAsync = async (data, _) =>
        {
            processedMessages.Add(Encoding.UTF8.GetString(data.TypedData));
            if (processedMessages.Count >= totalMessages)
            {
                allProcessed.TrySetResult();
            }

            return await Task.FromResult(ProcessedMessageStatus.Success);
        };

        await consumer.StartAsync();
        var executionTask = consumer.ExecuteAsync();
        await Task.WhenAny(allProcessed.Task, Task.Delay(TimeSpan.FromSeconds(30)));
        await consumer.StopAsync();
        await executionTask;

        Assert.Equal(totalMessages, processedMessages.Count);
        // Verify all partitions were represented
        for (var p = 0; p < numPartitions; p++)
        {
            for (var m = 0; m < messagesPerPartition; m++)
            {
                Assert.Contains($"p{p}-m{m}", processedMessages);
            }
        }
    }

    [Fact(Timeout = 50000)]
    public async Task Consume_SlowPartitionDoesNotBlockOtherPartitions_OtherPartitionsStillProcessed()
    {
        const int numPartitions = 2;
        var topic = await CreateMultiPartitionTopic(numPartitions);

        // Publish 1 message to partition 0 (will be slow) and multiple to partition 1
        await PublishMessageToPartition(topic, 0, "slow");
        for (var i = 0; i < 5; i++)
        {
            await PublishMessageToPartition(topic, 1, $"fast-{i}");
        }

        var config = GetConsumerConfig<string>(topic, maxConcurrentMessagesPerPartition: 5);
        config.CommitPeriod = 1;
        config.AutoCommitIntervalMs = 1;
        var consumer = GetConsumer(topic, config);

        var partition0Blocked = new TaskCompletionSource();
        var partition1Messages = new ConcurrentBag<string>();
        var allFastProcessed = new TaskCompletionSource();

        consumer.ConsumeCallbackAsync = async (data, cancellationToken) =>
        {
            var msg = Encoding.UTF8.GetString(data.TypedData);
            if (msg == "slow")
            {
                partition0Blocked.TrySetResult();
                // Block this message indefinitely to simulate a slow partition
                await Task.Delay(-1, cancellationToken);
                return ProcessedMessageStatus.Success;
            }

            partition1Messages.Add(msg);
            if (partition1Messages.Count >= 5)
            {
                allFastProcessed.TrySetResult();
            }

            return await Task.FromResult(ProcessedMessageStatus.Success);
        };

        await consumer.StartAsync();
        var executionTask = consumer.ExecuteAsync();

        // Wait for the slow message to start processing
        await Task.WhenAny(partition0Blocked.Task, Task.Delay(TimeSpan.FromSeconds(15)));
        Assert.True(partition0Blocked.Task.IsCompleted, "Slow message on partition 0 should have started processing");

        // Wait for all fast messages to complete — they should not be blocked by partition 0
        await Task.WhenAny(allFastProcessed.Task, Task.Delay(TimeSpan.FromSeconds(15)));
        Assert.True(
            allFastProcessed.Task.IsCompleted,
            "Fast messages on partition 1 should have completed despite slow partition 0"
        );

        await consumer.StopAsync();
        await executionTask;

        Assert.Equal(5, partition1Messages.Count);
    }

    [Fact(Timeout = 50000)]
    public async Task Consume_PerPartitionConcurrencyLimit_LimitsEachPartitionIndependently()
    {
        const int numPartitions = 2;
        const int perPartitionLimit = 3;
        var topic = await CreateMultiPartitionTopic(numPartitions);

        // Publish more messages than the per-partition limit to each partition
        for (var p = 0; p < numPartitions; p++)
        {
            for (var i = 0; i < perPartitionLimit * 2; i++)
            {
                await PublishMessageToPartition(topic, p, $"p{p}-{i}");
            }
        }

        var config = GetConsumerConfig<string>(topic, maxConcurrentMessagesPerPartition: perPartitionLimit);
        var consumer = GetConsumer(topic, config);

        var perPartitionCounts = new ConcurrentDictionary<string, int>();
        var firstMessageProcessed = new TaskCompletionSource();

        consumer.ConsumeCallbackAsync = async (data, cancellationToken) =>
        {
            var msg = Encoding.UTF8.GetString(data.TypedData);
            var partition = msg.Split('-')[0]; // "p0" or "p1"
            perPartitionCounts.AddOrUpdate(partition, 1, (_, count) => count + 1);
            firstMessageProcessed.TrySetResult();

            // Block indefinitely to keep messages in-flight
            await Task.Delay(-1, cancellationToken);
            return ProcessedMessageStatus.Success;
        };

        await consumer.StartAsync();
        var executionTask = consumer.ExecuteAsync();

        // Wait for processing to start
        await Task.WhenAny(firstMessageProcessed.Task, Task.Delay(TimeSpan.FromSeconds(15)));
        // Give time for the consumer to fill up the channels
        await Task.Delay(TimeSpan.FromSeconds(2));

        await consumer.StopAsync();
        await executionTask;

        // Each partition should have at most perPartitionLimit in-flight messages
        foreach (var kvp in perPartitionCounts)
        {
            output.WriteLine($"Partition {kvp.Key}: {kvp.Value} messages started");
            Assert.True(
                kvp.Value <= perPartitionLimit,
                $"Partition {kvp.Key} had {kvp.Value} concurrent messages, expected at most {perPartitionLimit}"
            );
        }
    }

    [Fact(Timeout = 50000)]
    public async Task Consume_MultiPartition_CommitsAllPartitions()
    {
        const int numPartitions = 3;
        const int messagesPerPartition = 3;
        var topic = await CreateMultiPartitionTopic(numPartitions);

        for (var p = 0; p < numPartitions; p++)
        {
            for (var m = 0; m < messagesPerPartition; m++)
            {
                await PublishMessageToPartition(topic, p, $"p{p}-m{m}");
            }
        }

        var config = GetConsumerConfig<string>(topic, maxConcurrentMessagesPerPartition: 5);
        config.CommitPeriod = 1;
        config.AutoCommitIntervalMs = 1;
        var consumer = GetConsumer(topic, config);
        var processedCount = 0;
        var allProcessed = new TaskCompletionSource();
        const int totalMessages = numPartitions * messagesPerPartition;
        var lockObject = new object();
        consumer.ConsumeCallbackAsync = async (_, _) =>
        {
            lock (lockObject)
            {
                processedCount++;
                output.WriteLine($"Processed message {processedCount}/{totalMessages}");
                if (processedCount >= totalMessages)
                {
                    allProcessed.TrySetResult();
                }
            }

            return await Task.FromResult(ProcessedMessageStatus.Success);
        };

        await consumer.StartAsync();
        var executionTask = consumer.ExecuteAsync();

        // Wait until all messages are processed
        await Task.WhenAny(allProcessed.Task, Task.Delay(TimeSpan.FromSeconds(30)));
        Assert.True(
            allProcessed.Task.IsCompleted,
            $"Expected all {totalMessages} messages to be processed, but only {processedCount} were processed"
        );

        // Give time for the commit loop to commit all offsets
        await Task.Delay(TimeSpan.FromSeconds(2));

        // Verify all committed offsets sum to total messages (must be done before StopAsync disposes the consumer)
        var offsets = consumer.Committed();
        var committedTotal = offsets.Where(tpo => tpo.Offset != Offset.Unset).Sum(tpo => (long)tpo.Offset);

        await consumer.StopAsync();
        await executionTask;

        Assert.Equal(totalMessages, committedTotal);
    }

    [Fact(Timeout = 50000)]
    public async Task Consume_MultiPartition_IrrecoverableFailureStopsApplication()
    {
        const int numPartitions = 2;
        var topic = await CreateMultiPartitionTopic(numPartitions);
        var fakeLifetimeMock = new Mock<IHostApplicationLifetime>();

        await PublishMessageToPartition(topic, 0, "good");
        await PublishMessageToPartition(topic, 1, "bad");

        var config = GetConsumerConfig<string>(topic, maxConcurrentMessagesPerPartition: 5);
        var consumer = GetConsumer(topic, config, fakeLifetimeMock.Object);
        consumer.ConsumeCallbackAsync = async (data, _) =>
        {
            var msg = Encoding.UTF8.GetString(data.TypedData);
            return await Task.FromResult(
                msg == "bad" ? ProcessedMessageStatus.CriticalFailure : ProcessedMessageStatus.Success
            );
        };

        await consumer.StartAsync();
        var executionTask = consumer.ExecuteAsync();

        WaitUntil(() => fakeLifetimeMock.Verify(mock => mock.StopApplication()));
        await consumer.StopAsync();
        await executionTask;
    }

    #endregion

    #region Rebalance / repartitioning tests

    [Fact(Timeout = 60000)]
    public async Task Consume_SecondConsumerJoinsGroup_RebalanceDoesNotLoseMessages()
    {
        const int numPartitions = 10;
        var topic = await CreateMultiPartitionTopic(numPartitions);
        var groupId = $"rebalance-join-{Guid.NewGuid():N}";

        var processedMessagesA = new ConcurrentBag<string>();
        var processedMessagesB = new ConcurrentBag<string>();

        // Start consumer A — initially owns all 4 partitions
        var configA = GetConsumerConfig<string>(topic, maxConcurrentMessagesPerPartition: 10, groupId: groupId);
        configA.CommitPeriod = 1;
        configA.AutoCommitIntervalMs = 1;
        configA.SessionTimeoutMs = 6000;
        var consumerA = GetConsumer(topic, configA);
        consumerA.ConsumeCallbackAsync = async (data, _) =>
        {
            processedMessagesA.Add(Encoding.UTF8.GetString(data.TypedData));
            return await Task.FromResult(ProcessedMessageStatus.Success);
        };
        await consumerA.StartAsync();
        var executionA = consumerA.ExecuteAsync();

        // Start a background publisher that continuously sends messages across all partitions.
        // The stream runs before, during, and after both rebalances.
        var publishCts = new CancellationTokenSource();
        var publishedMessages = new ConcurrentBag<string>();
        var publisherTask = Task.Run(
            async () =>
            {
                var messageIndex = 0;
                while (!publishCts.Token.IsCancellationRequested)
                {
                    for (var p = 0; p < numPartitions; p++)
                    {
                        var msg = $"msg-{messageIndex++}-p{p}";
                        try
                        {
                            await PublishMessageToPartition(topic, p, msg);
                            publishedMessages.Add(msg);
                        }
                        catch (Exception) when (publishCts.Token.IsCancellationRequested)
                        {
                            return;
                        }
                    }
                    await Task.Delay(10, publishCts.Token);
                }
            },
            publishCts.Token
        );

        // Wait for consumer A to consume some messages before the first rebalance
        await Task.Delay(TimeSpan.FromSeconds(3));

        // --- First rebalance: consumer B joins ---
        var configB = GetConsumerConfig<string>(topic, maxConcurrentMessagesPerPartition: 10, groupId: groupId);
        configB.CommitPeriod = 1;
        configB.AutoCommitIntervalMs = 1;
        configB.SessionTimeoutMs = 6000;
        var consumerB = GetConsumer(topic, configB);
        consumerB.ConsumeCallbackAsync = async (data, _) =>
        {
            processedMessagesB.Add(Encoding.UTF8.GetString(data.TypedData));
            return await Task.FromResult(ProcessedMessageStatus.Success);
        };
        await consumerB.StartAsync();
        var executionB = consumerB.ExecuteAsync();

        // Let both consumers process messages together for a while
        await Task.Delay(TimeSpan.FromSeconds(5));

        // --- Second rebalance: consumer B leaves ---
        await consumerB.StopAsync();
        await executionB;

        // Let consumer A process alone after the second rebalance
        await Task.Delay(TimeSpan.FromSeconds(3));

        // Stop the message stream
        await publishCts.CancelAsync();
        try
        {
            await publisherTask;
        }
        catch (OperationCanceledException) { }

        // Give consumer A time to process remaining messages
        await Task.Delay(TimeSpan.FromSeconds(3));

        await consumerA.StopAsync();
        await executionA;

        // Both consumers must have processed at least one message
        output.WriteLine($"Consumer A processed: {processedMessagesA.Count}");
        output.WriteLine($"Consumer B processed: {processedMessagesB.Count}");
        output.WriteLine($"Total published: {publishedMessages.Count}");
        Assert.NotEmpty(processedMessagesA);
        Assert.NotEmpty(processedMessagesB);

        // All published messages must have been processed by one of the consumers
        var allProcessed = processedMessagesA.Concat(processedMessagesB).ToHashSet();
        foreach (var msg in publishedMessages)
        {
            Assert.Contains(msg, allProcessed);
        }
    }

    #endregion
}
