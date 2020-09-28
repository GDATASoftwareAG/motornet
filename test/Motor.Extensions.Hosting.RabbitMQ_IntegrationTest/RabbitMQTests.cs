using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CloudNative.CloudEvents;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.RabbitMQ;
using Motor.Extensions.TestUtilities;
using Microsoft.Extensions.Hosting;
using Moq;
using Motor.Extensions.Diagnostics.Tracing;
using OpenTracing;
using OpenTracing.Mock;
using Xunit;
using RMQ = RabbitMQ.Client;

namespace Motor.Extensions.Hosting.RabbitMQ_IntegrationTest
{
    [Collection("RabbitMQ")]
    public class RabbitMQTests : IClassFixture<RabbitMQFixture>
    {
        private readonly RabbitMQFixture _fixture;
        private readonly Random _random = new Random();
        public RabbitMQTests(RabbitMQFixture fixture) => _fixture = fixture;

        [Fact]
        public async Task ConsumerStartAsync_WithQueueName_QueueExists()
        {
            var builder = RabbitMQTestBuilder
                .CreateWithoutQueueDeclare(_fixture)
                .WithConsumerCallback((context, bytes) => Task.FromResult(ProcessedMessageStatus.Success))
                .Build();
            var consumer = builder.GetConsumer<string>();

            await consumer.StartAsync();

            Assert.True(builder.IsConsumerQueueDeclared());
        }

        [Fact]
        public async Task ConsumerStartAsync_ConsumeMessage_ConsumedEqualsPublished()
        {
            const byte priority = 111;
            var message = new byte[] {1, 2, 3};
            byte? consumedPriority = null;
            var consumedMessage = (byte[]) null;
            var builder = RabbitMQTestBuilder
                .CreateWithQueueDeclare(_fixture)
                .WithSinglePublishedMessage(priority, message)
                .WithConsumerCallback((motorEvent, token) =>
                {
                    consumedPriority = motorEvent.Extension<RabbitMQPriorityExtension>().Priority;
                    consumedMessage = motorEvent.TypedData;
                    return Task.FromResult(ProcessedMessageStatus.Success);
                })
                .Build();
            var consumer = builder.GetConsumer<string>();

            await consumer.StartAsync();

            await Task.Delay(TimeSpan.FromSeconds(2));
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

            var span = CreateStartSpan(builder.Tracer);
            const byte priority = 222;
            var message = new byte[] {3, 2, 1};
            var extensions = new List<ICloudEventExtension>
            {
                new JaegerTracingExtension(span.Context),
                new RabbitMQPriorityExtension(priority),
            };

            await publisher.PublishMessageAsync(
                MotorCloudEvent.CreateTestCloudEvent(message, extensions: extensions.ToArray()));

            var results = await builder.GetMessageFromQueue();
            Assert.Equal(priority, results.Extension<RabbitMQPriorityExtension>().Priority);
            Assert.Equal(span.Context.SpanId, results.Extension<JaegerTracingExtension>().SpanContext.SpanId);
            Assert.Equal(span.Context.TraceId, results.Extension<JaegerTracingExtension>().SpanContext.TraceId);
            Assert.Equal(message, results.Data);
        }

        [Fact]
        public async Task ConsumerStartAsync_OneMessageInQueueAndConsumeCallbackSuccess_QueueEmptyAfterAck()
        {
            var builder = RabbitMQTestBuilder
                .CreateWithoutQueueDeclare(_fixture)
                .WithSingleRandomPublishedMessage()
                .WithConsumerCallback((context, bytes) => Task.FromResult(ProcessedMessageStatus.Success))
                .Build();
            var consumer = builder.GetConsumer<string>();

            await consumer.StartAsync();

            await Task.Delay(TimeSpan.FromSeconds(2));
            Assert.Equal((uint) 0, builder.MessageInConsumerQueue());
        }

        [Fact]
        public async Task ConsumerStartAsync_CheckParallelProcessing_EnsureAllMessagesAreConsumed()
        {
            const int raceConditionTimeout = 2;
            const int messageProcessingTimeSeconds = 2;

            var builder = RabbitMQTestBuilder
                .CreateWithQueueDeclare(_fixture)
                .WithConsumerCallback(async (context, bytes) =>
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
            Assert.Equal((uint) 0, builder.MessageInConsumerQueue());
        }

        [Fact]
        public async Task
            ConsumerStartAsync_OneMessageInQueueAndConsumeCallbackReturnsTempFailureAndAfterwardsSuccess_MessageConsumedTwice()
        {
            var consumerCounter = 0;
            var builder = RabbitMQTestBuilder
                .CreateWithQueueDeclare(_fixture)
                .WithSingleRandomPublishedMessage()
                .WithConsumerCallback((context, bytes) =>
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
                .WithConsumerCallback((context, bytes) => Task.FromResult(ProcessedMessageStatus.InvalidInput))
                .Build();
            var consumer = builder.GetConsumer<string>();

            await consumer.StartAsync();

            await Task.Delay(TimeSpan.FromSeconds(2));
            Assert.Equal((uint) 0, builder.MessageInConsumerQueue());
        }

        [Fact]
        public async Task ConsumerStartAsync_MessageWithoutPreviousSpan_NoSpanContextExtracted()
        {
            var consumedSpanContext = (ISpanContext) null;
            var builder = RabbitMQTestBuilder
                .CreateWithQueueDeclare(_fixture)
                .WithSingleRandomPublishedMessage(withSpan: false)
                .WithConsumerCallback((motorEvent, bytes) =>
                {
                    consumedSpanContext = motorEvent.Extension<JaegerTracingExtension>().SpanContext;
                    return Task.FromResult(ProcessedMessageStatus.Success);
                })
                .Build();
            var consumer = builder.GetConsumer<string>();

            await consumer.StartAsync();

            await Task.Delay(TimeSpan.FromSeconds(2));
            Assert.Null(consumedSpanContext);
        }

        [Fact]
        public async Task ConsumerStartAsync_MessageWithPreviousSpan_SpanContextExtracted()
        {
            var consumedSpanContext = (ISpanContext) null;
            var builder = RabbitMQTestBuilder
                .CreateWithQueueDeclare(_fixture)
                .WithSingleRandomPublishedMessage()
                .WithConsumerCallback((motorEvent, bytes) =>
                {
                    consumedSpanContext = motorEvent.Extension<JaegerTracingExtension>().SpanContext;
                    return Task.FromResult(ProcessedMessageStatus.Success);
                })
                .Build();
            var consumer = builder.GetConsumer<string>();

            await consumer.StartAsync();

            await Task.Delay(TimeSpan.FromSeconds(2));
            Assert.Equal(builder.LastPublishedSpanContext.TraceId, consumedSpanContext?.TraceId);
            Assert.Equal(builder.LastPublishedSpanContext.SpanId, consumedSpanContext?.SpanId);
        }

        [Fact]
        public async Task ConsumerStartAsync_ConsumeCallbackAsyncThrows_CriticalExitCalled()
        {
            var builder = RabbitMQTestBuilder
                .CreateWithQueueDeclare(_fixture)
                .WithSingleRandomPublishedMessage()
                .WithConsumerCallback((context, bytes) => throw new Exception())
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
                .WithConsumerCallback((context, bytes) => Task.FromResult(ProcessedMessageStatus.CriticalFailure))
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
                .WithConsumerCallback(async (context, bytes) =>
                {
                    await Task.Delay(4000);
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

        private static ISpan CreateStartSpan(MockTracer tracer)
        {
            var span = tracer.BuildSpan("test").Start();
            span.Finish();
            return span;
        }
    }
}
