using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CloudNative.CloudEvents;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.RabbitMQ;
using Motor.Extensions.Hosting.RabbitMQ.Options;
using Motor.Extensions.TestUtilities;
using Xunit;
using RMQ = RabbitMQ.Client;
using Opts = Microsoft.Extensions.Options.Options;

namespace Motor.Extensions.Hosting.RabbitMQ_IntegrationTest
{
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
            var builder = RabbitMQTestBuilder
                .CreateWithoutQueueDeclare(_fixture)
                .WithConsumerCallback((_, _) => Task.FromResult(ProcessedMessageStatus.Success))
                .Build();
            var consumer = builder.GetConsumer<string>();

            await consumer.StartAsync();

            Assert.True(builder.IsConsumerQueueDeclared());
        }

        [Fact]
        public async Task ConsumerStartAsync_ConsumeMessage_ConsumedEqualsPublished()
        {
            const byte priority = 111;
            var message = new byte[] { 1, 2, 3 };
            byte? consumedPriority = null;
            var consumedMessage = (byte[])null;
            var builder = RabbitMQTestBuilder
                .CreateWithQueueDeclare(_fixture)
                .WithSinglePublishedMessage(priority, message)
                .WithConsumerCallback((motorEvent, _) =>
                {
                    consumedPriority = motorEvent.Extension<RabbitMQPriorityExtension>().Priority;
                    consumedMessage = motorEvent.TypedData;
                    return Task.FromResult(ProcessedMessageStatus.Success);
                })
                .Build();
            var consumer = builder.GetConsumer<string>();

            await consumer.StartAsync();

            await Task.Delay(TimeSpan.FromSeconds(5));
            Assert.NotNull(consumedMessage);
            Assert.Equal(priority, consumedPriority);
            Assert.Equal(message, consumedMessage);
        }


        [Fact]
        public async Task PublisherPublishMessageAsync_SomeMessage_PublishedEqualsConsumed()
        {
            var builder = RabbitMQTestBuilder
                .CreateWithQueueDeclare(_fixture)
                .Build();
            var publisher = builder.GetPublisher<byte[]>();

            const byte priority = 222;
            var message = new byte[] { 3, 2, 1 };
            var extensions = new List<ICloudEventExtension>
            {
                new RabbitMQPriorityExtension(priority)
            };

            await publisher.PublishMessageAsync(
                MotorCloudEvent.CreateTestCloudEvent(message, extensions: extensions.ToArray()));

            var results = await builder.GetMessageFromQueue();
            Assert.Equal(priority, results.Extension<RabbitMQPriorityExtension>().Priority);
            Assert.Equal(message, results.Data);
        }

        [Fact]
        public async Task ConsumerStartAsync_OneMessageInQueueAndConsumeCallbackSuccess_QueueEmptyAfterAck()
        {
            var builder = RabbitMQTestBuilder
                .CreateWithoutQueueDeclare(_fixture)
                .WithSingleRandomPublishedMessage()
                .WithConsumerCallback((_, _) => Task.FromResult(ProcessedMessageStatus.Success))
                .Build();
            var consumer = builder.GetConsumer<string>();

            await consumer.StartAsync();

            await Task.Delay(TimeSpan.FromSeconds(2));
            Assert.Equal((uint)0, builder.MessageInConsumerQueue());
        }

        [Fact]
        public async Task ConsumerStartAsync_CheckParallelProcessing_EnsureAllMessagesAreConsumed()
        {
            const int raceConditionTimeout = 2;
            const int messageProcessingTimeSeconds = 2;

            var builder = RabbitMQTestBuilder
                .CreateWithQueueDeclare(_fixture)
                .WithConsumerCallback(async (_, _) =>
                {
                    await Task.CompletedTask;
                    await Task.Delay(TimeSpan.FromSeconds(messageProcessingTimeSeconds));
                    return ProcessedMessageStatus.Success;
                })
                .WithMultipleRandomPublishedMessage()
                .Build();
            var consumer = builder.GetConsumer<string>();

            await consumer.StartAsync();

            await Task.Delay(TimeSpan.FromSeconds(messageProcessingTimeSeconds + raceConditionTimeout));
            Assert.Equal((uint)0, builder.MessageInConsumerQueue());
        }

        [Fact]
        public async Task
            ConsumerStartAsync_OneMessageInQueueAndConsumeCallbackReturnsTempFailureAndAfterwardsSuccess_MessageConsumedTwice()
        {
            var consumerCounter = 0;
            var builder = RabbitMQTestBuilder
                .CreateWithQueueDeclare(_fixture)
                .WithSingleRandomPublishedMessage()
                .WithConsumerCallback((_, _) =>
                {
                    consumerCounter++;
                    return Task.FromResult(consumerCounter == 1
                        ? ProcessedMessageStatus.TemporaryFailure
                        : ProcessedMessageStatus.Success);
                })
                .Build();
            var consumer = builder.GetConsumer<string>();

            await consumer.StartAsync();

            await Task.Delay(TimeSpan.FromSeconds(2));
            Assert.Equal(2, consumerCounter);
        }

        [Fact]
        public async Task ConsumerStartAsync_OneMessageInQueueAndConsumeCallbackInvalidInput_QueueEmptyAfterReject()
        {
            var builder = RabbitMQTestBuilder
                .CreateWithQueueDeclare(_fixture)
                .WithSingleRandomPublishedMessage()
                .WithConsumerCallback((_, _) => Task.FromResult(ProcessedMessageStatus.InvalidInput))
                .Build();
            var consumer = builder.GetConsumer<string>();

            await consumer.StartAsync();

            await Task.Delay(TimeSpan.FromSeconds(2));
            Assert.Equal((uint)0, builder.MessageInConsumerQueue());
        }


        [Fact]
        public async Task ConsumerStartAsync_ConsumeCallbackAsyncThrows_CriticalExitCalled()
        {
            var builder = RabbitMQTestBuilder
                .CreateWithQueueDeclare(_fixture)
                .WithSingleRandomPublishedMessage()
                .WithConsumerCallback((_, _) => throw new Exception())
                .Build();

            var applicationLifetimeMock = new Mock<IHostApplicationLifetime>();
            var consumer = builder.GetConsumer<string>(applicationLifetimeMock.Object);

            await consumer.StartAsync();

            await Task.Delay(TimeSpan.FromSeconds(3));
            applicationLifetimeMock.VerifyUntilTimeoutAsync(t => t.StopApplication(), Times.Once);
        }

        [Fact]
        public async Task ConsumerStartAsync_ConsumeCallbackReturnsACriticalStatus_CriticalExitCalled()
        {
            var builder = RabbitMQTestBuilder
                .CreateWithQueueDeclare(_fixture)
                .WithSingleRandomPublishedMessage()
                .WithConsumerCallback((_, _) => Task.FromResult(ProcessedMessageStatus.CriticalFailure))
                .Build();

            var applicationLifetimeMock = new Mock<IHostApplicationLifetime>();
            var consumer = builder.GetConsumer<string>(applicationLifetimeMock.Object);

            await consumer.StartAsync();

            applicationLifetimeMock.VerifyUntilTimeoutAsync(t => t.StopApplication(), Times.Once);
        }

        [Fact]
        public async Task ConsumerStopAsync_ConsumeCallbackShouldStopAsEarlyAsPossible_NoStopApplicationCalled()
        {
            var builder = RabbitMQTestBuilder
                .CreateWithQueueDeclare(_fixture)
                .WithConsumerCallback(async (_, token) =>
                {
                    await Task.Delay(4000, token);
                    return ProcessedMessageStatus.CriticalFailure;
                })
                .WithMultipleRandomPublishedMessage()
                .Build();

            var consumer = builder.GetConsumer<string>();

            await consumer.StartAsync();
            await consumer.StopAsync();

            await Task.Delay(TimeSpan.FromSeconds(3));
            Assert.Equal(RabbitMQTestBuilder.PrefetchCount, builder.MessageInConsumerQueue());
        }

        [Fact]
        public async Task QueueMonitor_GetCurrentState_QueueEmpty_EmptyResult()
        {
            var builder = RabbitMQTestBuilder
                .CreateWithQueueDeclare(_fixture)
                .Build();

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

            var state = await monitor.GetCurrentState();
            Assert.Equal(builder.QueueName, state.QueueName);
            Assert.Equal(0, state.ConsumerCount);
            Assert.InRange(state.ReadyMessages, 0, Int64.MaxValue);
        }

        [Fact]
        public async Task QueueMonitor_GetCurrentState_SingleMessage_CorrespondingResult()
        {
            var builder = RabbitMQTestBuilder
                .CreateWithQueueDeclare(_fixture)
                .WithSingleRandomPublishedMessage()
                .Build();

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

            var state = await monitor.GetCurrentState();
            Assert.Equal(builder.QueueName, state.QueueName);
            Assert.Equal(0, state.ConsumerCount);
            Assert.Equal(1, state.ReadyMessages);
        }

        [Fact]
        public async Task QueueMonitor_GetCurrentState_MultipleMessages_CorrespondingResult()
        {
            var builder = RabbitMQTestBuilder
                .CreateWithQueueDeclare(_fixture)
                .WithMultipleRandomPublishedMessage()
                .Build();

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

            var state = await monitor.GetCurrentState();
            Assert.Equal(builder.QueueName, state.QueueName);
            Assert.Equal(0, state.ConsumerCount);
            Assert.InRange(state.ReadyMessages, 2, Int64.MaxValue);
        }
    }
}
