using System.Text;
using CloudNative.CloudEvents.SystemTextJson;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using Motor.Extensions.Hosting.PgMq;
using Motor.Extensions.Hosting.PgMq.Options;
using Motor.Extensions.TestUtilities;
using Npgmq;
using RandomDataGenerator.FieldOptions;
using RandomDataGenerator.Randomizers;
using Xunit;
using MSOptions = Microsoft.Extensions.Options.Options;

namespace Motor.Extensions.Hosting.PgMq_IntegrationTest;

[Collection("PgMqMessage")]
public class PgMqIntegrationTests : IClassFixture<PostgresFixture>
{
    private readonly IRandomizerString _randomizerString;
    private readonly string _connectionString;

    public PgMqIntegrationTests(PostgresFixture fixture)
    {
        _randomizerString = RandomizerFactory.GetRandomizer(new FieldOptionsTextRegex { Pattern = @"^[a-z]{10}" });
        _connectionString = fixture.ConnectionString;
    }

    // ------------------------------------------------------------------
    // Test 1: Happy path – Protocol format
    // ------------------------------------------------------------------
    [Fact(Timeout = 50000)]
    public async Task Consume_ProduceAndConsumeProtocolFormat_ConsumedDataEqualsPublished()
    {
        const string expectedMessage = "hello-protocol";
        var queueName = _randomizerString.Generate()!;
        var producer = GetProducer<string>(queueName, CloudEventFormat.Protocol);
        var consumer = GetConsumer<string>(queueName, CloudEventFormat.Protocol);
        await producer.StartAsync();
        await consumer.StartAsync();
        byte[]? received = null;
        var tcs = new TaskCompletionSource();
        consumer.ConsumeCallbackAsync = async (evt, _) =>
        {
            received = evt.TypedData;
            tcs.TrySetResult();
            return await Task.FromResult(ProcessedMessageStatus.Success);
        };
        var executeTask = consumer.ExecuteAsync();
        await producer.PublishMessageAsync(
            MotorCloudEvent.CreateTestCloudEvent(Encoding.UTF8.GetBytes(expectedMessage))
        );
        await Task.WhenAny(tcs.Task, executeTask, Task.Delay(TimeSpan.FromSeconds(30)));
        Assert.NotNull(received);
        Assert.Equal(expectedMessage, Encoding.UTF8.GetString(received!));
    }

    // ------------------------------------------------------------------
    // Test 2: Happy path – JSON format
    // ------------------------------------------------------------------
    [Fact(Timeout = 50000)]
    public async Task Consume_ProduceAndConsumeJsonFormat_ConsumedDataEqualsPublished()
    {
        const string expectedMessage = "hello-json";
        var queueName = _randomizerString.Generate()!;
        var producer = GetProducer<string>(queueName, CloudEventFormat.Json);
        var consumer = GetConsumer<string>(queueName, CloudEventFormat.Json);
        await producer.StartAsync();
        await consumer.StartAsync();
        byte[]? received = null;
        var tcs = new TaskCompletionSource();
        consumer.ConsumeCallbackAsync = async (evt, _) =>
        {
            received = evt.TypedData;
            tcs.TrySetResult();
            return await Task.FromResult(ProcessedMessageStatus.Success);
        };
        var executeTask = consumer.ExecuteAsync();
        await producer.PublishMessageAsync(
            MotorCloudEvent.CreateTestCloudEvent(Encoding.UTF8.GetBytes(expectedMessage))
        );
        await Task.WhenAny(tcs.Task, executeTask, Task.Delay(TimeSpan.FromSeconds(30)));
        Assert.NotNull(received);
        Assert.Equal(expectedMessage, Encoding.UTF8.GetString(received!));
    }

    // ------------------------------------------------------------------
    // Test 3: ExecuteAsync without StartAsync logs error and returns
    // ------------------------------------------------------------------
    [Fact(Timeout = 10000)]
    public async Task ExecuteAsync_CalledWithoutStartAsync_LogsErrorAndReturnsImmediately()
    {
        var queueName = _randomizerString.Generate()!;
        var loggerMock = new Mock<ILogger<PgMqMessageConsumer<string>>>();
        var consumer = GetConsumer(queueName, CloudEventFormat.Protocol, loggerMock.Object);
        // Do NOT call StartAsync
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await consumer.ExecuteAsync(cts.Token);
        loggerMock.Verify(
            x =>
                x.Log(
                    LogLevel.Error,
                    PgMq.LogEvents.ConsumerNotStarted,
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    // ------------------------------------------------------------------
    // Test 4: Non-success callback status stops application
    // ------------------------------------------------------------------
    [Fact(Timeout = 50000)]
    public async Task Consume_CallbackReturnsFailureStatus_StopsApplicationAndLogsError()
    {
        var queueName = _randomizerString.Generate()!;
        var lifetimeMock = new Mock<IHostApplicationLifetime>();
        var producer = GetProducer<string>(queueName, CloudEventFormat.Protocol);
        var consumer = GetConsumer<string>(
            queueName,
            CloudEventFormat.Protocol,
            applicationLifetime: lifetimeMock.Object
        );
        await producer.StartAsync();
        await consumer.StartAsync();
        consumer.ConsumeCallbackAsync = (_, _) => Task.FromResult(ProcessedMessageStatus.CriticalFailure);
        var executeTask = consumer.ExecuteAsync();
        await producer.PublishMessageAsync(MotorCloudEvent.CreateTestCloudEvent(Encoding.UTF8.GetBytes("fail-me")));
        await Task.WhenAny(executeTask, Task.Delay(TimeSpan.FromSeconds(30)));
        lifetimeMock.Verify(x => x.StopApplication(), Times.AtLeastOnce);
    }

    // ------------------------------------------------------------------
    // Test 5: Unhandled exception in callback stops application
    // ------------------------------------------------------------------
    [Fact(Timeout = 50000)]
    public async Task Consume_UnhandledExceptionInCallback_StopsApplicationAndLogsCritical()
    {
        var queueName = _randomizerString.Generate()!;
        var lifetimeMock = new Mock<IHostApplicationLifetime>();
        var loggerMock = new Mock<ILogger<PgMqMessageConsumer<string>>>();
        var producer = GetProducer<string>(queueName, CloudEventFormat.Protocol);
        var consumer = GetConsumer(queueName, CloudEventFormat.Protocol, loggerMock.Object, lifetimeMock.Object);
        await producer.StartAsync();
        await consumer.StartAsync();
        consumer.ConsumeCallbackAsync = (_, _) => throw new InvalidOperationException("boom");
        var executeTask = consumer.ExecuteAsync();
        await producer.PublishMessageAsync(MotorCloudEvent.CreateTestCloudEvent(Encoding.UTF8.GetBytes("boom")));
        await Task.WhenAny(executeTask, Task.Delay(TimeSpan.FromSeconds(30)));
        lifetimeMock.Verify(x => x.StopApplication(), Times.AtLeastOnce);
        loggerMock.Verify(
            x =>
                x.Log(
                    LogLevel.Critical,
                    PgMq.LogEvents.MessageHandlingUnexpectedException,
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.AtLeastOnce
        );
    }

    // ------------------------------------------------------------------
    // Test 6: Multiple messages consumed sequentially
    // ------------------------------------------------------------------
    [Fact(Timeout = 60000)]
    public async Task Consume_MultipleMessages_AllConsumedSequentiallyAndAcknowledged()
    {
        const int messageCount = 5;
        var queueName = _randomizerString.Generate()!;
        var producer = GetProducer<string>(queueName, CloudEventFormat.Protocol);
        var consumer = GetConsumer<string>(queueName, CloudEventFormat.Protocol);
        await producer.StartAsync();
        await consumer.StartAsync();
        var received = new List<string>();
        var tcs = new TaskCompletionSource();
        consumer.ConsumeCallbackAsync = async (evt, _) =>
        {
            lock (received)
            {
                received.Add(Encoding.UTF8.GetString(evt.TypedData));
                if (received.Count == messageCount)
                {
                    tcs.TrySetResult();
                }
            }
            return await Task.FromResult(ProcessedMessageStatus.Success);
        };
        // Publish all messages before starting consumer loop
        for (var i = 0; i < messageCount; i++)
        {
            await producer.PublishMessageAsync(
                MotorCloudEvent.CreateTestCloudEvent(Encoding.UTF8.GetBytes($"msg-{i}"))
            );
        }
        using var cts = new CancellationTokenSource();
        var executeTask = consumer.ExecuteAsync(cts.Token);
        await Task.WhenAny(tcs.Task, executeTask, Task.Delay(TimeSpan.FromSeconds(45)));
        Assert.Equal(messageCount, received.Count);
        // Cancel consumer and verify queue is empty via raw NpgmqClient
        await cts.CancelAsync();
        var rawClient = new NpgmqClient(_connectionString);
        await rawClient.InitAsync();
        var leftover = await rawClient.ReadAsync<byte[]>(queueName, 1);
        Assert.Null(leftover);
    }

    // ------------------------------------------------------------------
    // Test 7: Cancellation during polling exits gracefully
    // ------------------------------------------------------------------
    [Fact(Timeout = 20000)]
    public async Task Consume_TokenCancelledDuringPoll_ExitsGracefully()
    {
        var queueName = _randomizerString.Generate()!;
        var lifetimeMock = new Mock<IHostApplicationLifetime>();
        var consumer = GetConsumer<string>(
            queueName,
            CloudEventFormat.Protocol,
            applicationLifetime: lifetimeMock.Object
        );
        consumer.ConsumeCallbackAsync = (_, _) => Task.FromResult(ProcessedMessageStatus.Success);
        await consumer.StartAsync();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await consumer.ExecuteAsync(cts.Token);
        lifetimeMock.Verify(x => x.StopApplication(), Times.Never);
    }

    // ------------------------------------------------------------------
    // Test 8: Message deleted on success – no redelivery
    // ------------------------------------------------------------------
    [Fact(Timeout = 30000)]
    public async Task Consume_MessageDeletedOnSuccess_NotRedelivered()
    {
        var queueName = _randomizerString.Generate()!;
        var producer = GetProducer<string>(queueName, CloudEventFormat.Protocol);
        var consumer = GetConsumer<string>(queueName, CloudEventFormat.Protocol, visibilityTimeoutInSeconds: 3);
        await producer.StartAsync();
        await consumer.StartAsync();
        var callCount = 0;
        var tcs = new TaskCompletionSource();
        consumer.ConsumeCallbackAsync = async (_, _) =>
        {
            callCount++;
            tcs.TrySetResult();
            return await Task.FromResult(ProcessedMessageStatus.Success);
        };
        await producer.PublishMessageAsync(MotorCloudEvent.CreateTestCloudEvent("delete-me"u8.ToArray()));
        _ = consumer.ExecuteAsync();
        await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(15)));
        // Wait longer than visibility timeout to ensure no redelivery
        await Task.Delay(TimeSpan.FromSeconds(5));
        Assert.Equal(1, callCount);
    }

    // ------------------------------------------------------------------
    // Test 9: CloudEvent attributes round-trip (Protocol format)
    // ------------------------------------------------------------------
    [Fact(Timeout = 50000)]
    public async Task Consume_SuccessfulMessage_ProtocolFormat_CloudEventAttributesRoundTrip()
    {
        var queueName = _randomizerString.Generate()!;
        var expectedSource = new Uri("motor://test-source");
        var expectedId = Guid.NewGuid().ToString();
        var producer = GetProducer<string>(queueName, CloudEventFormat.Protocol);
        var consumer = GetConsumer<string>(queueName, CloudEventFormat.Protocol);
        await producer.StartAsync();
        await consumer.StartAsync();
        MotorCloudEvent<byte[]>? receivedEvent = null;
        var tcs = new TaskCompletionSource();
        consumer.ConsumeCallbackAsync = async (evt, _) =>
        {
            receivedEvent = evt;
            tcs.TrySetResult();
            return await Task.FromResult(ProcessedMessageStatus.Success);
        };
        var executeTask = consumer.ExecuteAsync();
        var appNameService = GetApplicationNameService(expectedSource.ToString());
        var cloudEvent = new MotorCloudEvent<byte[]>(
            appNameService,
            Encoding.UTF8.GetBytes("round-trip"),
            null,
            expectedSource,
            expectedId,
            null,
            null
        );
        await producer.PublishMessageAsync(cloudEvent);
        await Task.WhenAny(tcs.Task, executeTask, Task.Delay(TimeSpan.FromSeconds(30)));
        Assert.NotNull(receivedEvent);
        Assert.Equal(expectedId, receivedEvent!.Id);
        Assert.Equal(expectedSource, receivedEvent.Source);
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------
    private PgMqMessageProducer<T> GetProducer<T>(string queueName, CloudEventFormat cloudEventFormat)
        where T : notnull
    {
        var publisherOptions = MSOptions.Create(
            new PgMqPublisherOptions<T> { ConnectionString = _connectionString, QueueName = queueName }
        );
        var producerOptions = MSOptions.Create(new PublisherOptions { CloudEventFormat = cloudEventFormat });
        return new PgMqMessageProducer<T>(publisherOptions, producerOptions, new JsonEventFormatter());
    }

    private PgMqMessageConsumer<T> GetConsumer<T>(
        string queueName,
        CloudEventFormat cloudEventFormat,
        ILogger<PgMqMessageConsumer<T>>? logger = null,
        IHostApplicationLifetime? applicationLifetime = null,
        int visibilityTimeoutInSeconds = 30
    )
        where T : notnull
    {
        var options = new PgMqConsumerOptions<T>
        {
            ConnectionString = _connectionString,
            QueueName = queueName,
            CloudEventFormat = cloudEventFormat,
            VisibilityTimeoutInSeconds = visibilityTimeoutInSeconds,
            PollingIntervalInMilliseconds = 100,
        };
        logger ??= Mock.Of<ILogger<PgMqMessageConsumer<T>>>();
        applicationLifetime ??= Mock.Of<IHostApplicationLifetime>();
        return new PgMqMessageConsumer<T>(
            options,
            new JsonEventFormatter(),
            logger,
            applicationLifetime,
            GetApplicationNameService()
        );
    }

    private IApplicationNameService GetApplicationNameService(string source = "motor://test")
    {
        var mock = new Mock<IApplicationNameService>();
        mock.Setup(t => t.GetSource()).Returns(new Uri(source));
        mock.Setup(t => t.GetFullName()).Returns("test");
        return mock.Object;
    }
}
