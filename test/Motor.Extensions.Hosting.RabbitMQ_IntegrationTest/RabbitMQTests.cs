using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.RabbitMQ;
using Motor.Extensions.Hosting.RabbitMQ.Options;
using Motor.Extensions.TestUtilities;
using Xunit;
using Opts = Microsoft.Extensions.Options.Options;

namespace Motor.Extensions.Hosting.RabbitMQ_IntegrationTest;

[Collection("RabbitMQ")]
public class RabbitMQTests : IClassFixture<RabbitMQFixture>
{
    private readonly RabbitMQFixture _fixture;

    public RabbitMQTests(RabbitMQFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ConsumerStartAsync_WithQueueName_QueueExists()
    {
        var builder = await RabbitMQTestBuilder
            .CreateWithoutQueueDeclare(_fixture)
            .WithConsumerCallback((_, _) => Task.FromResult(ProcessedMessageStatus.Success), false)
            .BuildAsync();
        var consumer = await builder.GetConsumerAsync<string>();

        await consumer.StartAsync();

        Assert.True(await builder.IsConsumerQueueDeclaredAsync());
    }

    [Fact]
    public async Task ConsumerStartAsync_WithQueueName_DlxQueueExists()
    {
        var builder = await RabbitMQTestBuilder
            .CreateWithoutQueueDeclare(_fixture)
            .WithDeadLetterExchange()
            .WithConsumerCallback((_, _) => Task.FromResult(ProcessedMessageStatus.Success), false)
            .BuildAsync();
        var consumer = await builder.GetConsumerAsync<string>();

        await consumer.StartAsync();

        Assert.True(await builder.IsConsumerQueueDeclaredAsync());
    }

    [Fact]
    public async Task ConsumerStartAsync_ConsumerWithDlxRejectMessage_MessageIsInDlxQueue()
    {
        var message = new byte[] { 1, 2, 3 };
        var builder = await RabbitMQTestBuilder
            .CreateWithQueueDeclare(_fixture)
            .WithDeadLetterExchange()
            .WithConsumerCallback((_, _) => Task.FromResult(ProcessedMessageStatus.Failure))
            .WithSinglePublishedMessage(145, message)
            .BuildAsync();
        var consumer = await builder.GetConsumerAsync<string>();

        await consumer.StartAsync();

        var results = await builder.GetMessageFromQueueAsync(builder.DlxQueueName);
        Assert.Equal(message, results.TypedData);
    }

    [Theory(Timeout = 50000)]
    [InlineData(false, 0)]
    [InlineData(true, 1)]
    public async Task ConsumerStartAsync_CallbackInvalidInput_VerifyCorrectBehaviorOnInvalidInput(bool republishOnInvalidInput, uint expectedNumberOfMessagesInDlxQueue)
    {
        var taskCompletionSource = new TaskCompletionSource();
        var builder = await RabbitMQTestBuilder
            .CreateWithQueueDeclare(_fixture)
            .WithDeadLetterExchange()
            .WithRepublishToDeadLetterExchangeOnInvalidInput(republishOnInvalidInput)
            .WithSingleRandomPublishedMessage()
            .WithConsumerCallback((_, _) =>
            {
                taskCompletionSource.TrySetResult();
                return Task.FromResult(ProcessedMessageStatus.InvalidInput);
            })
            .BuildAsync();
        var consumer = await builder.GetConsumerAsync<string>();

        await consumer.StartAsync();

        // Wait until processing begins
        await taskCompletionSource.Task;
        // Give RabbitMQConsumer enough time to acknowledge message
        await Task.Delay(TimeSpan.FromSeconds(1));
        Assert.Equal((uint)0, await builder.MessagesInQueueAsync(builder.QueueName));
        Assert.Equal(expectedNumberOfMessagesInDlxQueue, await builder.MessagesInQueueAsync(builder.DlxQueueName));
    }

    [Fact(Timeout = 50000)]
    public async Task ConsumerStartAsync_ConsumeMessage_ConsumedEqualsPublished()
    {
        const byte priority = 111;
        var message = new byte[] { 1, 2, 3 };
        var taskCompletionSource = new TaskCompletionSource();
        byte? consumedPriority = null;
        var consumedMessage = (byte[])null;
        var builder = await RabbitMQTestBuilder
            .CreateWithQueueDeclare(_fixture)
            .WithSinglePublishedMessage(priority, message)
            .WithConsumerCallback((motorEvent, _) =>
            {
                consumedPriority = motorEvent.GetRabbitMQPriority();
                consumedMessage = motorEvent.TypedData;
                taskCompletionSource.TrySetResult();
                return Task.FromResult(ProcessedMessageStatus.Success);
            })
            .BuildAsync();
        var consumer = await builder.GetConsumerAsync<string>();

        await consumer.StartAsync();

        // Wait until processing begins
        await taskCompletionSource.Task;
        // Give RabbitMQConsumer enough time to acknowledge message
        await Task.Delay(TimeSpan.FromSeconds(1));
        Assert.Equal(priority, consumedPriority);
        Assert.Equal(message, consumedMessage);
    }


    [Fact]
    public async Task PublisherPublishMessageAsync_SomeMessage_PublishedEqualsConsumed()
    {
        var builder = await RabbitMQTestBuilder
            .CreateWithQueueDeclare(_fixture)
            .BuildAsync();
        var publisher = await builder.GetPublisherAsync<byte[]>();
        await publisher.StartAsync();

        const byte priority = 222;
        var message = new byte[] { 3, 2, 1 };

        var cloudEvent = MotorCloudEvent.CreateTestCloudEvent(message);
        cloudEvent.SetRabbitMQPriority(priority);

        await publisher.PublishMessageAsync(cloudEvent);

        var results = await builder.GetMessageFromQueueAsync(builder.QueueName);
        Assert.Equal(priority, results.GetRabbitMQPriority());
        Assert.Equal(message, results.Data);
    }

    [Fact(Timeout = 50000)]
    public async Task ConsumerStartAsync_OneMessageInQueueAndConsumeCallbackSuccess_QueueEmptyAfterAck()
    {

        var taskCompletionSource = new TaskCompletionSource();
        var builder = await RabbitMQTestBuilder
            .CreateWithoutQueueDeclare(_fixture)
            .WithSingleRandomPublishedMessage()
            .WithConsumerCallback((_, _) =>
            {
                taskCompletionSource.TrySetResult();
                return Task.FromResult(ProcessedMessageStatus.Success);
            })
            .BuildAsync();
        var consumer = await builder.GetConsumerAsync<string>();

        await consumer.StartAsync();

        // Wait until processing begins
        await taskCompletionSource.Task;
        // Give RabbitMQConsumer enough time to acknowledge message
        await Task.Delay(TimeSpan.FromSeconds(1));
        Assert.Equal((uint)0, await builder.MessagesInQueueAsync(builder.QueueName));
    }

    [Fact]
    public async Task ConsumerStartAsync_CheckParallelProcessing_EnsureAllMessagesAreConsumed()
    {
        const int messageProcessingTimeSeconds = 2;
        const ushort messageCount = 99;
        var synchronousExecutionTime = TimeSpan.FromSeconds(messageProcessingTimeSeconds * messageCount);
        var builder = await RabbitMQTestBuilder
            .CreateWithQueueDeclare(_fixture)
            .WithConsumerCallback(async (_, ct) =>
            {
                await Task.CompletedTask;
                await Task.Delay(TimeSpan.FromSeconds(messageProcessingTimeSeconds), ct);
                return ProcessedMessageStatus.Success;
            })
            .WithMultipleRandomPublishedMessage(messageCount)
            .BuildAsync();
        var consumer = await builder.GetConsumerAsync<string>();

        await consumer.StartAsync();
        var timeout = Task.Delay(synchronousExecutionTime);

        while (await builder.MessagesInQueueAsync(builder.QueueName) != 0 && !timeout.IsCompleted)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(10));
        }
        // Give RabbitMQConsumer enough time to acknowledge message
        await Task.Delay(TimeSpan.FromSeconds(1));

        Assert.Equal((uint)0, await builder.MessagesInQueueAsync(builder.QueueName));
    }

    [Fact(Timeout = 50000)]
    public async Task
        ConsumerStartAsync_OneMessageInQueueAndConsumeCallbackReturnsTempFailureAndAfterwardsSuccess_MessageConsumedTwice()
    {
        var taskCompletionSource = new TaskCompletionSource();
        var consumerCounter = 0;
        var builder = await RabbitMQTestBuilder
            .CreateWithQueueDeclare(_fixture)
            .WithSingleRandomPublishedMessage()

            .WithConsumerCallback((_, _) =>
            {
                consumerCounter++;
                if (consumerCounter == 1)
                {
                    return Task.FromResult(ProcessedMessageStatus.TemporaryFailure);
                }
                taskCompletionSource.TrySetResult();
                return Task.FromResult(ProcessedMessageStatus.Success);
            })
            .BuildAsync();
        var consumer = await builder.GetConsumerAsync<string>();

        await consumer.StartAsync();

        // Wait until second processing begins
        await taskCompletionSource.Task;
        // Give RabbitMQConsumer enough time to acknowledge message
        await Task.Delay(TimeSpan.FromSeconds(1));
        Assert.Equal(2, consumerCounter);
    }

    [Fact]
    public async Task ConsumerStartAsync_OneMessageInQueueAndConsumeCallbackInvalidInput_QueueEmptyAfterReject()
    {
        var taskCompletionSource = new TaskCompletionSource();
        var builder = await RabbitMQTestBuilder
            .CreateWithQueueDeclare(_fixture)
            .WithSingleRandomPublishedMessage()
            .WithConsumerCallback((_, _) =>
            {
                taskCompletionSource.TrySetResult();
                return Task.FromResult(ProcessedMessageStatus.InvalidInput);
            })
            .BuildAsync();
        var consumer = await builder.GetConsumerAsync<string>();

        await consumer.StartAsync();

        // Wait until processing begins
        await taskCompletionSource.Task;
        // Give RabbitMQConsumer enough time to acknowledge message
        await Task.Delay(TimeSpan.FromSeconds(1));
        Assert.Equal((uint)0, await builder.MessagesInQueueAsync(builder.QueueName));
    }


    [Fact]
    public async Task ConsumerStartAsync_ConsumeCallbackAsyncThrows_CriticalExitCalled()
    {
        var taskCompletionSource = new TaskCompletionSource();
        var builder = await RabbitMQTestBuilder
            .CreateWithQueueDeclare(_fixture)
            .WithSingleRandomPublishedMessage()
            .WithConsumerCallback((_, _) =>
            {
                taskCompletionSource.TrySetResult();
                throw new Exception();
            })
            .BuildAsync();

        var applicationLifetimeMock = new Mock<IHostApplicationLifetime>();
        var consumer = await builder.GetConsumerAsync<string>(applicationLifetimeMock.Object);

        await consumer.StartAsync();

        // Wait until processing begins
        await taskCompletionSource.Task;
        // Give RabbitMQConsumer enough time to acknowledge message
        await Task.Delay(TimeSpan.FromSeconds(1));
        applicationLifetimeMock.VerifyUntilTimeoutAsync(t => t.StopApplication(), Times.Once);
    }

    [Fact]
    public async Task ConsumerStartAsync_ConsumeCallbackReturnsACriticalStatus_CriticalExitCalled()
    {
        var taskCompletionSource = new TaskCompletionSource();
        var builder = await RabbitMQTestBuilder
            .CreateWithQueueDeclare(_fixture)
            .WithSingleRandomPublishedMessage()
            .WithConsumerCallback((_, _) =>
            {
                taskCompletionSource.TrySetResult();
                return Task.FromResult(ProcessedMessageStatus.CriticalFailure);
            })
            .BuildAsync();

        var applicationLifetimeMock = new Mock<IHostApplicationLifetime>();
        var consumer = await builder.GetConsumerAsync<string>(applicationLifetimeMock.Object);

        await consumer.StartAsync();

        // Wait until processing begins
        await taskCompletionSource.Task;
        // Give RabbitMQConsumer enough time to acknowledge message
        await Task.Delay(TimeSpan.FromSeconds(1));
        applicationLifetimeMock.VerifyUntilTimeoutAsync(t => t.StopApplication(), Times.Once);
    }

    [Fact]
    public async Task ConsumerStopAsync_ConsumeCallbackShouldStopAsEarlyAsPossible_NoStopApplicationCalled()
    {
        var builder = await RabbitMQTestBuilder
            .CreateWithQueueDeclare(_fixture)
            .WithConsumerCallback(async (_, token) =>
            {
                await Task.Delay(4000, token);
                return ProcessedMessageStatus.CriticalFailure;
            })
            .WithMultipleRandomPublishedMessage()
            .BuildAsync();

        var consumer = await builder.GetConsumerAsync<string>();

        await consumer.StartAsync();
        await consumer.StopAsync();

        await Task.Delay(TimeSpan.FromSeconds(3));
        Assert.Equal(RabbitMQTestBuilder.PrefetchCount, await builder.MessagesInQueueAsync(builder.QueueName));
    }

    [Fact]
    public async Task QueueMonitor_GetCurrentState_QueueEmpty_ReadyMessagesZero()
    {
        var builder = await RabbitMQTestBuilder
            .CreateWithQueueDeclare(_fixture)
            .BuildAsync();

        var monitor = new RabbitMQQueueMonitor<string>(
            Mock.Of<ILogger<RabbitMQQueueMonitor<string>>>(),
            Opts.Create(new RabbitMQConsumerOptions<string>
            {
                Queue =
                {
                    Name = builder.QueueName
                }
            }),
            _fixture.ConnectionFactory<string>()
        );

        var state = await monitor.GetCurrentStateAsync();
        Assert.Equal(builder.QueueName, state.QueueName);
        Assert.Equal(0, state.ConsumerCount);
        Assert.Equal(0, state.ReadyMessages);
    }

    [Fact]
    public async Task QueueMonitor_GetCurrentState_SingleMessage_ReadyMessagesOne()
    {
        var builder = await RabbitMQTestBuilder
            .CreateWithQueueDeclare(_fixture)
            .WithSingleRandomPublishedMessage()
            .BuildAsync();

        var monitor = new RabbitMQQueueMonitor<string>(
            Mock.Of<ILogger<RabbitMQQueueMonitor<string>>>(),
            Opts.Create(new RabbitMQConsumerOptions<string>
            {
                Queue =
                {
                    Name = builder.QueueName
                }
            }),
            _fixture.ConnectionFactory<string>()
        );

        var state = await monitor.GetCurrentStateAsync();
        Assert.Equal(builder.QueueName, state.QueueName);
        Assert.Equal(0, state.ConsumerCount);
        Assert.Equal(1, state.ReadyMessages);
    }

    [Fact]
    public async Task QueueMonitor_GetCurrentState_MultipleMessages_ReadyMessagesGreaterZero()
    {
        var builder = await RabbitMQTestBuilder
            .CreateWithQueueDeclare(_fixture)
            .WithMultipleRandomPublishedMessage()
            .BuildAsync();

        var monitor = new RabbitMQQueueMonitor<string>(
            Mock.Of<ILogger<RabbitMQQueueMonitor<string>>>(),
            Opts.Create(new RabbitMQConsumerOptions<string>
            {
                Queue =
                {
                    Name = builder.QueueName
                }
            }),
            _fixture.ConnectionFactory<string>()
        );

        var state = await monitor.GetCurrentStateAsync();
        Assert.Equal(builder.QueueName, state.QueueName);
        Assert.Equal(0, state.ConsumerCount);
        Assert.Equal(RabbitMQTestBuilder.PrefetchCount, state.ReadyMessages);
    }

    [Fact]
    public async Task QueueMonitor_GetCurrentState_ActiveConsumer_ConsumerCountGreaterZero()
    {
        var builder = await RabbitMQTestBuilder
            .CreateWithQueueDeclare(_fixture)
            .WithMultipleRandomPublishedMessage()
            .BuildAsync();

        var random = new Random();
        var consumer = await builder.GetConsumerAsync<string>();
        consumer.ConsumeCallbackAsync = async (_, token) =>
        {
            await Task.Delay(random.Next(500), token);
            return ProcessedMessageStatus.Success;
        };

        await consumer.StartAsync();

        var monitor = new RabbitMQQueueMonitor<string>(
            Mock.Of<ILogger<RabbitMQQueueMonitor<string>>>(),
            Opts.Create(new RabbitMQConsumerOptions<string>
            {
                Queue =
                {
                    Name = builder.QueueName
                }
            }),
            _fixture.ConnectionFactory<string>()
        );

        var state = await monitor.GetCurrentStateAsync();
        Assert.Equal(builder.QueueName, state.QueueName);
        Assert.Equal(1, state.ConsumerCount);
        Assert.InRange(state.ReadyMessages, 0, RabbitMQTestBuilder.PrefetchCount);
    }
}
