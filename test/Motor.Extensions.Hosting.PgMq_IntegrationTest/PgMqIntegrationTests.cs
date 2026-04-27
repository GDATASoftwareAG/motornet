using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using Motor.Extensions.Hosting.PgMq;
using Motor.Extensions.Hosting.PgMq.Options;
using Motor.Extensions.TestUtilities;
using Npgmq;
using Npgsql;
using RandomDataGenerator.FieldOptions;
using RandomDataGenerator.Randomizers;
using Xunit;
using MSOptions = Microsoft.Extensions.Options.Options;

namespace Motor.Extensions.Hosting.PgMq_IntegrationTest;

[Collection("PgMqMessage")]
public class PgMqIntegrationTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private const int TestTimeout = 50000;
    private readonly IRandomizerString _randomizerString = RandomizerFactory.GetRandomizer(
        new FieldOptionsTextRegex { Pattern = @"^[a-z]{10}" }
    );
    private readonly NpgsqlConnectionStringBuilder _pgConnectionStringBuilder = new(fixture.ConnectionString);

    [Fact(Timeout = TestTimeout)]
    public async Task Consume_ProduceAndConsumeProtocolFormat_ConsumedDataEqualsPublished()
    {
        const string expectedMessage = "hello-protocol";
        var queueName = _randomizerString.Generate()!;
        var producer = GetProducer<string>(queueName, CloudEventFormat.Protocol);
        var consumer = GetConsumer<string>(queueName);
        await producer.StartAsync();
        await consumer.StartAsync();
        var tcs = new TaskCompletionSource<byte[]>();
        consumer.ConsumeCallbackAsync = (evt, _) =>
        {
            tcs.TrySetResult(evt.TypedData);
            return Task.FromResult(ProcessedMessageStatus.Success);
        };
        using var cts = new CancellationTokenSource();

        var executeTask = consumer.ExecuteAsync(cts.Token);
        await producer.PublishMessageAsync(
            MotorCloudEvent.CreateTestCloudEvent(Encoding.UTF8.GetBytes(expectedMessage)),
            cts.Token
        );
        var received =
            await Task.WhenAny(tcs.Task, executeTask, Task.Delay(TimeSpan.FromSeconds(30))) == tcs.Task
                ? await tcs.Task
                : null;
        await cts.CancelAsync();
        await executeTask;
        await producer.StopAsync(cts.Token);
        await consumer.StopAsync(cts.Token);

        Assert.NotNull(received);
        Assert.Equal(expectedMessage, Encoding.UTF8.GetString(received));
    }

    [Fact(Timeout = TestTimeout)]
    public async Task Consume_ProduceAndConsumeJsonFormat_ThrowsUnhandledCloudEventFormatException()
    {
        const string expectedMessage = "hello-json";
        var queueName = _randomizerString.Generate()!;
        var producer = GetProducer<string>(queueName, CloudEventFormat.Json);
        await producer.StartAsync();

        await Assert.ThrowsAsync<UnhandledCloudEventFormatException>(async () =>
        {
            await producer.PublishMessageAsync(
                MotorCloudEvent.CreateTestCloudEvent(Encoding.UTF8.GetBytes(expectedMessage))
            );
        });
        await producer.StopAsync();
    }

    [Fact(Timeout = TestTimeout)]
    public async Task ExecuteAsync_CalledWithoutStartAsync_ThrowsException()
    {
        var queueName = _randomizerString.Generate()!;
        var consumer = GetConsumer<string>(queueName);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await Assert.ThrowsAsync<InvalidOperationException>(() => consumer.ExecuteAsync(cts.Token));
    }

    [Fact(Timeout = TestTimeout)]
    public async Task Consume_CallbackReturnsFailureStatus_StopsApplicationAndLogsError()
    {
        var queueName = _randomizerString.Generate()!;
        var lifetimeMock = new Mock<IHostApplicationLifetime>();
        var producer = GetProducer<string>(queueName, CloudEventFormat.Protocol);
        var consumer = GetConsumer<string>(queueName, applicationLifetime: lifetimeMock.Object);
        await producer.StartAsync();
        await consumer.StartAsync();
        consumer.ConsumeCallbackAsync = (_, _) => Task.FromResult(ProcessedMessageStatus.CriticalFailure);
        using var cts = new CancellationTokenSource();

        var executeTask = consumer.ExecuteAsync(cts.Token);
        await producer.PublishMessageAsync(
            MotorCloudEvent.CreateTestCloudEvent(Encoding.UTF8.GetBytes("fail-me")),
            cts.Token
        );
        await executeTask;
        await producer.StopAsync(cts.Token);
        await consumer.StopAsync(cts.Token);

        lifetimeMock.Verify(x => x.StopApplication(), Times.AtLeastOnce);
    }

    [Fact(Timeout = TestTimeout)]
    public async Task Consume_UnhandledExceptionInCallback_StopsApplicationAndLogsCritical()
    {
        var queueName = _randomizerString.Generate()!;
        var lifetimeMock = new Mock<IHostApplicationLifetime>();
        var loggerMock = new Mock<ILogger<PgMqMessageConsumer<string>>>();
        var producer = GetProducer<string>(queueName, CloudEventFormat.Protocol);
        var consumer = GetConsumer(queueName, loggerMock.Object, lifetimeMock.Object);
        await producer.StartAsync();
        await consumer.StartAsync();
        consumer.ConsumeCallbackAsync = (_, _) => throw new InvalidOperationException("boom");
        using var cts = new CancellationTokenSource();

        var executeTask = consumer.ExecuteAsync(cts.Token);
        await producer.PublishMessageAsync(
            MotorCloudEvent.CreateTestCloudEvent(Encoding.UTF8.GetBytes("boom")),
            cts.Token
        );
        await executeTask;
        await cts.CancelAsync();
        await producer.StopAsync(cts.Token);
        await consumer.StopAsync(cts.Token);

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

    [Fact(Timeout = TestTimeout)]
    public async Task Consume_MultipleMessages_AllConsumedAndDeletedFromQueue()
    {
        const int messageCount = 5;
        var queueName = _randomizerString.Generate()!;
        var producer = GetProducer<string>(queueName, CloudEventFormat.Protocol);
        var consumer = GetConsumer<string>(queueName);
        await producer.StartAsync();
        await consumer.StartAsync();
        var received = new List<string>();
        var tcs = new TaskCompletionSource();
        consumer.ConsumeCallbackAsync = (evt, _) =>
        {
            lock (received)
            {
                received.Add(Encoding.UTF8.GetString(evt.TypedData));
                if (received.Count == messageCount)
                {
                    tcs.TrySetResult();
                }
            }
            return Task.FromResult(ProcessedMessageStatus.Success);
        };
        for (var i = 0; i < messageCount; i++)
        {
            await producer.PublishMessageAsync(
                MotorCloudEvent.CreateTestCloudEvent(Encoding.UTF8.GetBytes($"msg-{i}"))
            );
        }
        using var cts = new CancellationTokenSource();

        var executeTask = consumer.ExecuteAsync(cts.Token);
        await Task.WhenAny(tcs.Task, executeTask, Task.Delay(TimeSpan.FromSeconds(45), cts.Token));
        await cts.CancelAsync();
        await executeTask;
        await producer.StopAsync(CancellationToken.None);
        await consumer.StopAsync(CancellationToken.None);
        var rawClient = new NpgmqClient(_pgConnectionStringBuilder.ConnectionString);
        await rawClient.InitAsync(CancellationToken.None);
        var leftover = await rawClient.ReadAsync<byte[]>(queueName, 1, CancellationToken.None);

        Assert.Equal(messageCount, received.Count);
        Assert.Null(leftover);
    }

    [Fact(Timeout = TestTimeout)]
    public async Task Consume_TokenCancelledDuringPoll_ExitsGracefully()
    {
        var queueName = _randomizerString.Generate()!;
        var lifetimeMock = new Mock<IHostApplicationLifetime>();
        var consumer = GetConsumer<string>(queueName, applicationLifetime: lifetimeMock.Object);
        consumer.ConsumeCallbackAsync = (_, _) => Task.FromResult(ProcessedMessageStatus.Success);
        await consumer.StartAsync();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        await consumer.ExecuteAsync(cts.Token);
        await consumer.StopAsync(cts.Token);

        lifetimeMock.Verify(x => x.StopApplication(), Times.Never);
    }

    [Fact(Timeout = TestTimeout)]
    public async Task Consume_MessageDeletedOnSuccess_NotRedelivered()
    {
        var queueName = _randomizerString.Generate()!;
        var producer = GetProducer<string>(queueName, CloudEventFormat.Protocol);
        var consumer = GetConsumer<string>(queueName, visibilityTimeoutInSeconds: 3);
        await producer.StartAsync();
        await consumer.StartAsync();
        var callCount = 0;
        consumer.ConsumeCallbackAsync = (_, _) =>
        {
            Interlocked.Increment(ref callCount);
            return Task.FromResult(ProcessedMessageStatus.Success);
        };
        await producer.PublishMessageAsync(MotorCloudEvent.CreateTestCloudEvent("delete-me"u8.ToArray()));
        using var cts = new CancellationTokenSource();

        var executeTask = consumer.ExecuteAsync(cts.Token);
        await cts.CancelAsync();
        await executeTask;
        await producer.StopAsync(CancellationToken.None);
        await consumer.StopAsync(CancellationToken.None);
        var rawClient = new NpgmqClient(_pgConnectionStringBuilder.ConnectionString);
        await rawClient.InitAsync(CancellationToken.None);
        var leftover = await rawClient.ReadAsync<byte[]>(queueName, 1, CancellationToken.None);

        Assert.Null(leftover);
        Assert.Equal(1, callCount);
    }

    [Fact(Timeout = TestTimeout)]
    public async Task Consume_SuccessfulMessage_ProtocolFormat_CloudEventAttributesRoundTrip()
    {
        var queueName = _randomizerString.Generate()!;
        var expectedSource = new Uri("motor://test-source");
        var expectedId = Guid.NewGuid().ToString();
        var producer = GetProducer<string>(queueName, CloudEventFormat.Protocol);
        var consumer = GetConsumer<string>(queueName);
        await producer.StartAsync();
        await consumer.StartAsync();
        var tcs = new TaskCompletionSource<MotorCloudEvent<byte[]>>();
        consumer.ConsumeCallbackAsync = (evt, _) =>
        {
            tcs.TrySetResult(evt);
            return Task.FromResult(ProcessedMessageStatus.Success);
        };
        using var cts = new CancellationTokenSource();
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

        var executeTask = consumer.ExecuteAsync(cts.Token);
        await producer.PublishMessageAsync(cloudEvent, cts.Token);
        var receivedEvent =
            await Task.WhenAny(tcs.Task, executeTask, Task.Delay(TimeSpan.FromSeconds(30), cts.Token)) == tcs.Task
                ? await tcs.Task
                : null;
        await cts.CancelAsync();
        await executeTask;
        await producer.StopAsync(cts.Token);
        await consumer.StopAsync(cts.Token);

        Assert.NotNull(receivedEvent);
        Assert.Equal(expectedId, receivedEvent.Id);
        Assert.Equal(expectedSource, receivedEvent.Source);
    }

    [Fact(Timeout = TestTimeout)]
    public async Task Consume_TokenCancelledDuringCallback_MessageImmediatelyVisibleAfterCancellation()
    {
        const int highVisibilityTimeoutSeconds = 60;
        var queueName = _randomizerString.Generate()!;
        var producer = GetProducer<string>(queueName, CloudEventFormat.Protocol);
        var consumer = GetConsumer<string>(queueName, visibilityTimeoutInSeconds: highVisibilityTimeoutSeconds);
        await producer.StartAsync();
        await consumer.StartAsync();

        using var cts = new CancellationTokenSource();
        var callbackStarted = new TaskCompletionSource();
        consumer.ConsumeCallbackAsync = async (_, token) =>
        {
            callbackStarted.TrySetResult();
            await Task.Delay(Timeout.Infinite, token);
            return ProcessedMessageStatus.Success;
        };

        await producer.PublishMessageAsync(
            MotorCloudEvent.CreateTestCloudEvent("cancel-me"u8.ToArray()),
            CancellationToken.None
        );

        var executeTask = consumer.ExecuteAsync(cts.Token);
        await Task.WhenAny(callbackStarted.Task, Task.Delay(TimeSpan.FromSeconds(30)));
        await cts.CancelAsync();
        await executeTask;
        await consumer.StopAsync(CancellationToken.None);
        var rawClient = new NpgmqClient(_pgConnectionStringBuilder.ConnectionString);
        await rawClient.InitAsync(CancellationToken.None);
        var requeued = await rawClient.ReadAsync<byte[]>(queueName, 1, CancellationToken.None);

        Assert.True(callbackStarted.Task.IsCompleted, "Callback should have started before timeout");
        Assert.NotNull(requeued);
    }

    private PgMqMessageProducer<T> GetProducer<T>(string queueName, CloudEventFormat cloudEventFormat)
        where T : notnull
    {
        var options = BuildPgOptions(
            new PgMqPublisherOptions
            {
                QueueName = queueName,
                Host = string.Empty,
                Database = string.Empty,
                Username = string.Empty,
                Password = string.Empty,
            }
        );
        var producerOptions = MSOptions.Create(new PublisherOptions { CloudEventFormat = cloudEventFormat });
        return new PgMqMessageProducer<T>(
            MSOptions.Create(options),
            producerOptions,
            new NpgmqClient(options.ToConnectionString())
        );
    }

    private PgMqMessageConsumer<T> GetConsumer<T>(
        string queueName,
        ILogger<PgMqMessageConsumer<T>>? logger = null,
        IHostApplicationLifetime? applicationLifetime = null,
        int visibilityTimeoutInSeconds = 30
    )
        where T : notnull
    {
        var options = BuildPgOptions(
            new PgMqConsumerOptions
            {
                QueueName = queueName,
                VisibilityTimeoutInSeconds = visibilityTimeoutInSeconds,
                PollTimeoutSeconds = 2,
                PollIntervalMilliseconds = 100,
                Host = string.Empty,
                Database = string.Empty,
                Username = string.Empty,
                Password = string.Empty,
            }
        );
        logger ??= Mock.Of<ILogger<PgMqMessageConsumer<T>>>();
        applicationLifetime ??= Mock.Of<IHostApplicationLifetime>();
        return new PgMqMessageConsumer<T>(
            options,
            logger,
            applicationLifetime,
            GetApplicationNameService(),
            new NpgmqClient(options.ToConnectionString())
        );
    }

    private T BuildPgOptions<T>(T options)
        where T : PgOptions =>
        options with
        {
            Host = _pgConnectionStringBuilder.Host ?? "localhost",
            Port = _pgConnectionStringBuilder.Port,
            Database = _pgConnectionStringBuilder.Database ?? string.Empty,
            Username = _pgConnectionStringBuilder.Username ?? string.Empty,
            Password = _pgConnectionStringBuilder.Password ?? string.Empty,
        };

    private static IApplicationNameService GetApplicationNameService(string source = "motor://test")
    {
        var mock = new Mock<IApplicationNameService>();
        mock.Setup(t => t.GetSource()).Returns(new Uri(source));
        mock.Setup(t => t.GetFullName()).Returns("test");
        return mock.Object;
    }
}
