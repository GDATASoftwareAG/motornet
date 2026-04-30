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
using Npgsql;
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
    private readonly NpgsqlConnectionStringBuilder _pgConnectionStringBuilder;

    public PgMqIntegrationTests(PostgresFixture fixture)
    {
        _randomizerString = RandomizerFactory.GetRandomizer(new FieldOptionsTextRegex { Pattern = @"^[a-z]{10}" });
        _pgConnectionStringBuilder = new NpgsqlConnectionStringBuilder(fixture.ConnectionString);
    }

    [Fact(Timeout = 50000)]
    public async Task Consume_ProduceAndConsumeProtocolFormat_ConsumedDataEqualsPublished()
    {
        const string expectedMessage = "hello-protocol";
        var queueName = _randomizerString.Generate()!;
        var producer = GetProducer<string>(queueName, CloudEventFormat.Protocol);
        var consumer = GetConsumer<string>(queueName);
        await producer.StartAsync();
        await consumer.StartAsync();
        byte[]? received = null;
        var tcs = new TaskCompletionSource();
        consumer.ConsumeCallbackAsync = (evt, _) =>
        {
            received = evt.TypedData;
            tcs.TrySetResult();
            return Task.FromResult(ProcessedMessageStatus.Success);
        };
        using var cts = new CancellationTokenSource();
        var executeTask = consumer.ExecuteAsync(cts.Token);
        await producer.PublishMessageAsync(
            MotorCloudEvent.CreateTestCloudEvent(Encoding.UTF8.GetBytes(expectedMessage))
        );
        await Task.WhenAny(tcs.Task, executeTask, Task.Delay(TimeSpan.FromSeconds(30)));
        await cts.CancelAsync();
        await executeTask;
        await consumer.StopAsync();
        Assert.NotNull(received);
        Assert.Equal(expectedMessage, Encoding.UTF8.GetString(received!));
    }

    [Fact(Timeout = 50000)]
    public async Task Consume_ProduceAndConsumeJsonFormat_ConsumedDataEqualsPublished()
    {
        const string expectedMessage = "hello-json";
        var queueName = _randomizerString.Generate()!;
        var producer = GetProducer<string>(queueName, CloudEventFormat.Json);
        var consumer = GetConsumer<string>(queueName);
        await producer.StartAsync();
        await consumer.StartAsync();
        byte[]? received = null;
        var tcs = new TaskCompletionSource();
        consumer.ConsumeCallbackAsync = (evt, _) =>
        {
            received = evt.TypedData;
            tcs.TrySetResult();
            return Task.FromResult(ProcessedMessageStatus.Success);
        };
        using var cts = new CancellationTokenSource();
        var executeTask = consumer.ExecuteAsync(cts.Token);
        await producer.PublishMessageAsync(
            MotorCloudEvent.CreateTestCloudEvent(Encoding.UTF8.GetBytes(expectedMessage))
        );
        await Task.WhenAny(tcs.Task, executeTask, Task.Delay(TimeSpan.FromSeconds(30)));
        await cts.CancelAsync();
        await executeTask;
        await consumer.StopAsync();
        Assert.NotNull(received);
        Assert.Equal(expectedMessage, Encoding.UTF8.GetString(received!));
    }

    [Fact(Timeout = 10000)]
    public async Task ExecuteAsync_CalledWithoutStartAsync_LogsErrorAndReturnsImmediately()
    {
        var queueName = _randomizerString.Generate()!;
        var loggerMock = new Mock<ILogger<PgMqMessageConsumer<string>>>();
        var consumer = GetConsumer(queueName, loggerMock.Object);
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

    [Fact(Timeout = 50000)]
    public async Task Consume_CallbackReturnsFailureStatus_StopsApplicationAndLogsError()
    {
        var queueName = _randomizerString.Generate()!;
        var lifetimeMock = new Mock<IHostApplicationLifetime>();
        var producer = GetProducer<string>(queueName, CloudEventFormat.Protocol);
        var consumer = GetConsumer<string>(
            queueName,
                        applicationLifetime: lifetimeMock.Object
        );
        await producer.StartAsync();
        await consumer.StartAsync();
        consumer.ConsumeCallbackAsync = (_, _) => Task.FromResult(ProcessedMessageStatus.CriticalFailure);
        using var cts = new CancellationTokenSource();
        var executeTask = consumer.ExecuteAsync(cts.Token);
        await producer.PublishMessageAsync(MotorCloudEvent.CreateTestCloudEvent(Encoding.UTF8.GetBytes("fail-me")));
        await Task.WhenAny(executeTask, Task.Delay(TimeSpan.FromSeconds(30)));
        await cts.CancelAsync();
        await executeTask;
        await consumer.StopAsync();
        lifetimeMock.Verify(x => x.StopApplication(), Times.AtLeastOnce);
    }

    [Fact(Timeout = 50000)]
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
        await producer.PublishMessageAsync(MotorCloudEvent.CreateTestCloudEvent(Encoding.UTF8.GetBytes("boom")));
        await Task.WhenAny(executeTask, Task.Delay(TimeSpan.FromSeconds(30)));
        await cts.CancelAsync();
        await executeTask;
        await consumer.StopAsync();
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

    [Fact(Timeout = 60000)]
    public async Task Consume_MultipleMessages_AllConsumedSequentiallyAndAcknowledged()
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
        await Task.WhenAny(tcs.Task, executeTask, Task.Delay(TimeSpan.FromSeconds(45)));
        Assert.Equal(messageCount, received.Count);
        await cts.CancelAsync();
        await executeTask;
        await consumer.StopAsync();
        var rawClient = new NpgmqClient(_pgConnectionStringBuilder.ConnectionString);
        await rawClient.InitAsync();
        var leftover = await rawClient.ReadAsync<byte[]>(queueName, 1);
        Assert.Null(leftover);
    }

    [Fact(Timeout = 20000)]
    public async Task Consume_TokenCancelledDuringPoll_ExitsGracefully()
    {
        var queueName = _randomizerString.Generate()!;
        var lifetimeMock = new Mock<IHostApplicationLifetime>();
        var consumer = GetConsumer<string>(
            queueName,
                        applicationLifetime: lifetimeMock.Object
        );
        consumer.ConsumeCallbackAsync = (_, _) => Task.FromResult(ProcessedMessageStatus.Success);
        await consumer.StartAsync();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await consumer.ExecuteAsync(cts.Token);
        await consumer.StopAsync();
        lifetimeMock.Verify(x => x.StopApplication(), Times.Never);
    }

    [Fact(Timeout = 30000)]
    public async Task Consume_MessageDeletedOnSuccess_NotRedelivered()
    {
        var queueName = _randomizerString.Generate()!;
        var producer = GetProducer<string>(queueName, CloudEventFormat.Protocol);
        var consumer = GetConsumer<string>(queueName, visibilityTimeoutInSeconds: 3);
        await producer.StartAsync();
        await consumer.StartAsync();
        var callCount = 0;
        var tcs = new TaskCompletionSource();
        consumer.ConsumeCallbackAsync = (_, _) =>
        {
            Interlocked.Increment(ref callCount);
            tcs.TrySetResult();
            return Task.FromResult(ProcessedMessageStatus.Success);
        };
        await producer.PublishMessageAsync(MotorCloudEvent.CreateTestCloudEvent("delete-me"u8.ToArray()));
        using var cts = new CancellationTokenSource();
        var executeTask = consumer.ExecuteAsync(cts.Token);
        await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(15)));
        await cts.CancelAsync();
        await executeTask;
        await consumer.StopAsync();
        var rawClient = new NpgmqClient(_pgConnectionStringBuilder.ConnectionString);
        await rawClient.InitAsync();
        var leftover = await rawClient.ReadAsync<byte[]>(queueName, 1);
        Assert.Null(leftover);
        Assert.Equal(1, callCount);
    }

    [Fact(Timeout = 50000)]
    public async Task Consume_SuccessfulMessage_ProtocolFormat_CloudEventAttributesRoundTrip()
    {
        var queueName = _randomizerString.Generate()!;
        var expectedSource = new Uri("motor://test-source");
        var expectedId = Guid.NewGuid().ToString();
        var producer = GetProducer<string>(queueName, CloudEventFormat.Protocol);
        var consumer = GetConsumer<string>(queueName);
        await producer.StartAsync();
        await consumer.StartAsync();
        MotorCloudEvent<byte[]>? receivedEvent = null;
        var tcs = new TaskCompletionSource();
        consumer.ConsumeCallbackAsync = (evt, _) =>
        {
            receivedEvent = evt;
            tcs.TrySetResult();
            return Task.FromResult(ProcessedMessageStatus.Success);
        };
        using var cts = new CancellationTokenSource();
        var executeTask = consumer.ExecuteAsync(cts.Token);
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
        await cts.CancelAsync();
        await executeTask;
        await consumer.StopAsync();
        Assert.NotNull(receivedEvent);
        Assert.Equal(expectedId, receivedEvent!.Id);
        Assert.Equal(expectedSource, receivedEvent.Source);
    }

    private PgMqMessageProducer<T> GetProducer<T>(string queueName, CloudEventFormat cloudEventFormat)
        where T : notnull
    {
        var options = new PgMqPublisherOptions<T>
        {
            Host = _pgConnectionStringBuilder.Host ?? "localhost",
            Port = _pgConnectionStringBuilder.Port,
            Database = _pgConnectionStringBuilder.Database ?? string.Empty,
            Username = _pgConnectionStringBuilder.Username ?? string.Empty,
            Password = _pgConnectionStringBuilder.Password ?? string.Empty,
            QueueName = queueName,
        };
        var producerOptions = MSOptions.Create(new PublisherOptions { CloudEventFormat = cloudEventFormat });
        var npgmqClient = new NpgmqClient(options.ToConnectionString());
        return new PgMqMessageProducer<T>(MSOptions.Create(options), producerOptions, new JsonEventFormatter(), npgmqClient);
    }

    private PgMqMessageConsumer<T> GetConsumer<T>(
        string queueName,
        ILogger<PgMqMessageConsumer<T>>? logger = null,
        IHostApplicationLifetime? applicationLifetime = null,
        int visibilityTimeoutInSeconds = 30
    )
        where T : notnull
    {
        var options = new PgMqConsumerOptions<T>
        {
            Host = _pgConnectionStringBuilder.Host ?? "localhost",
            Port = _pgConnectionStringBuilder.Port,
            Database = _pgConnectionStringBuilder.Database ?? string.Empty,
            Username = _pgConnectionStringBuilder.Username ?? string.Empty,
            Password = _pgConnectionStringBuilder.Password ?? string.Empty,
            QueueName = queueName,
            VisibilityTimeoutInSeconds = visibilityTimeoutInSeconds,
            PollTimeoutSeconds = 2,
            PollIntervalMilliseconds = 100,
        };
        logger ??= Mock.Of<ILogger<PgMqMessageConsumer<T>>>();
        applicationLifetime ??= Mock.Of<IHostApplicationLifetime>();
        return new PgMqMessageConsumer<T>(
            options,
            new JsonEventFormatter(),
            logger,
            applicationLifetime,
            GetApplicationNameService(),
            new NpgmqClient(options.ToConnectionString())
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
