using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CloudNative.CloudEvents;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Motor.Extensions.Diagnostics.Tracing;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.RabbitMQ;
using Motor.Extensions.Hosting.RabbitMQ.Config;
using Motor.Extensions.TestUtilities;
using OpenTracing;
using OpenTracing.Mock;
using OpenTracing.Propagation;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RandomDataGenerator.FieldOptions;
using RandomDataGenerator.Randomizers;
using Xunit;

namespace Motor.Extensions.Hosting.RabbitMQ_IntegrationTest
{
    public class RabbitMQTestBuilder
    {
        public const ushort PrefetchCount = 100;

        private static readonly Random _random = new Random();
        public readonly MockTracer Tracer = new MockTracer();
        private Func<MotorCloudEvent<byte[]>, CancellationToken, Task<ProcessedMessageStatus>> Callback;
        private bool createQueue;
        private RabbitMQFixture Fixture;
        private bool isBuilt;
        public ISpanContext LastPublishedSpanContext;
        private readonly IList<Tuple<byte, byte[], bool>> messages = new List<Tuple<byte, byte[], bool>>();
        private string QueueName;
        private string RoutingKey;

        public static RabbitMQTestBuilder CreateWithoutQueueDeclare(RabbitMQFixture fixture)
        {
            var randomizerString = RandomizerFactory.GetRandomizer(new FieldOptionsTextRegex {Pattern = @"^[A-Z]{10}"});

            return new RabbitMQTestBuilder
            {
                QueueName = randomizerString.Generate(),
                RoutingKey = randomizerString.Generate(),
                Fixture = fixture
            };
        }

        public static RabbitMQTestBuilder CreateWithQueueDeclare(RabbitMQFixture fixture)
        {
            var q = CreateWithoutQueueDeclare(fixture);
            q.createQueue = true;
            return q;
        }

        public RabbitMQTestBuilder WithConsumerCallback(
            Func<MotorCloudEvent<byte[]>, CancellationToken, Task<ProcessedMessageStatus>> callback)
        {
            Callback = callback;
            createQueue = true;
            return this;
        }

        public RabbitMQTestBuilder WithSinglePublishedMessage(byte priority, byte[] message, bool withSpan = true)
        {
            messages.Add(new Tuple<byte, byte[], bool>(priority, message, withSpan));
            return this;
        }

        public RabbitMQTestBuilder WithSingleRandomPublishedMessage(bool withSpan = true)
        {
            var bytes = new byte[10];
            _random.NextBytes(bytes);
            return WithSinglePublishedMessage(100, bytes, withSpan);
        }

        public RabbitMQTestBuilder WithMultipleRandomPublishedMessage(ushort number = PrefetchCount)
        {
            for (var i = 0; i < number; i++) WithSingleRandomPublishedMessage();

            return this;
        }

        public RabbitMQTestBuilder Build(int retries = 3)
        {
            if (createQueue)
            {
                var config = GetConsumerConfig<string>();
                using (var channel = Fixture.Connection.CreateModel())
                {
                    DeclareQueue(config, channel);
                    foreach (var message in messages) PublishSingleMessage(channel, message, config);
                }

                Policy
                    .Handle<Exception>()
                    .WaitAndRetry(retries, retryAttempt =>
                        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                    )
                    .Execute(() =>
                    {
                        var messageInConsumerQueue = MessageInConsumerQueue();
                        Assert.Equal((uint) messages.Count, messageInConsumerQueue);
                    });
            }

            isBuilt = true;
            return this;
        }

        private void PublishSingleMessage<T>(IModel channel, Tuple<byte, byte[], bool> message,
            RabbitMQConsumerConfig<T> config)
        {
            var properties = channel.CreateBasicProperties();
            properties.DeliveryMode = 2;
            properties.Priority = message.Item1;
            properties.Headers = new Dictionary<string, object>();

            if (message.Item3)
            {
                var span = Tracer.BuildSpan("test").Start();
                span.Finish();
                LastPublishedSpanContext = span.Context;
                properties.Headers = new Dictionary<string, object>
                {
                    {"x-open-tracing-spanid", span.Context.SpanId},
                    {"x-open-tracing-traceid", span.Context.TraceId}
                };
            }

            var bindings = config.Queue.Bindings[0];
            channel.BasicPublish(bindings.Exchange, bindings.RoutingKey, true, properties, message.Item2);
        }

        private void DeclareQueue<T>(RabbitMQConsumerConfig<T> config, IModel channel)
        {
            var arguments = config.Queue.Arguments.ToDictionary(t => t.Key, t => t.Value);
            arguments.Add("x-max-priority", config.Queue.MaxPriority);
            arguments.Add("x-max-length", config.Queue.MaxLength);
            arguments.Add("x-max-length-bytes", config.Queue.MaxLengthBytes);
            arguments.Add("x-message-ttl", config.Queue.MessageTtl);
            channel.QueueDeclare(
                config.Queue.Name,
                config.Queue.Durable,
                false,
                config.Queue.AutoDelete,
                arguments
            );
            foreach (var routingKeyConfig in config.Queue.Bindings)
                channel.QueueBind(
                    config.Queue.Name,
                    routingKeyConfig.Exchange,
                    routingKeyConfig.RoutingKey,
                    routingKeyConfig.Arguments);
        }

        private RabbitMQConsumerConfig<T> GetConsumerConfig<T>()
        {
            return new RabbitMQConsumerConfig<T>
            {
                Host = "host",
                User = "guest",
                Password = "guest",
                VirtualHost = "/",
                Queue = new RabbitMQQueueConfig
                {
                    Name = QueueName,
                    Bindings = new[]
                    {
                        new RabbitMQBindingConfig
                        {
                            Exchange = "amq.topic",
                            RoutingKey = RoutingKey
                        }
                    }
                },
                PrefetchCount = PrefetchCount
            };
        }

        public IMessageConsumer<T> GetConsumer<T>(IHostApplicationLifetime applicationLifetime = null)
        {
            if (!isBuilt)
                throw new InvalidOperationException();
            var rabbitConnectionFactoryMock = new Mock<IRabbitMQConnectionFactory>();
            var connectionFactoryMock = new Mock<IConnectionFactory>();
            rabbitConnectionFactoryMock.Setup(x => x.From(It.IsAny<RabbitMQConsumerConfig<T>>()))
                .Returns(connectionFactoryMock.Object);
            connectionFactoryMock.Setup(x => x.CreateConnection()).Returns(Fixture.Connection);
            var optionsMock = new Mock<IOptions<RabbitMQConsumerConfig<T>>>();
            optionsMock.Setup(x => x.Value).Returns(GetConsumerConfig<T>());
            applicationLifetime ??= new Mock<IHostApplicationLifetime>().Object;
            var loggerMock = new Mock<ILogger<RabbitMQMessageConsumer<T>>>();

            var consumer = new RabbitMQMessageConsumer<T>(loggerMock.Object, rabbitConnectionFactoryMock.Object,
                optionsMock.Object, applicationLifetime, Tracer, null, new JsonEventFormatter())
            {
                ConsumeCallbackAsync = Callback
            };
            return consumer;
        }

        public bool IsConsumerQueueDeclared()
        {
            if (!isBuilt)
                throw new InvalidOperationException();
            using var channel = Fixture.Connection.CreateModel();
            channel.QueueDeclarePassive(QueueName);
            return true;
        }

        public uint MessageInConsumerQueue()
        {
            using var channel = Fixture.Connection.CreateModel();
            var queueDeclarePassive = channel.QueueDeclarePassive(QueueName);
            return queueDeclarePassive.MessageCount;
        }

        public ITypedMessagePublisher<byte[]> GetPublisher<T>()
        {
            if (!isBuilt)
                throw new InvalidOperationException();
            var rabbitConnectionFactoryMock = new Mock<IRabbitMQConnectionFactory>();
            var connectionFactoryMock = new Mock<IConnectionFactory>();
            rabbitConnectionFactoryMock.Setup(x => x.From(It.IsAny<RabbitMQPublisherConfig<T>>()))
                .Returns(connectionFactoryMock.Object);
            connectionFactoryMock.Setup(x => x.CreateConnection()).Returns(Fixture.Connection);
            var optionsMock = new Mock<IOptions<RabbitMQPublisherConfig<T>>>();
            optionsMock.Setup(x => x.Value).Returns(GetPublisherConfig<T>());
            return new RabbitMQMessagePublisher<T>(rabbitConnectionFactoryMock.Object, optionsMock.Object,
                new JsonEventFormatter(), Tracer);
        }

        private RabbitMQPublisherConfig<T> GetPublisherConfig<T>()
        {
            return new RabbitMQPublisherConfig<T>
            {
                Host = "host",
                User = "guest",
                Password = "guest",
                VirtualHost = "/",
                PublishingTarget = new RabbitMQBindingConfig
                {
                    Exchange = "amq.topic",
                    RoutingKey = RoutingKey
                }
            };
        }

        public async Task<MotorCloudEvent<byte[]>> GetMessageFromQueue()
        {
            ISpanContext spanContext = null;
            var message = (byte[]) null;
            var priority = (byte) 0;
            using (var channel = Fixture.Connection.CreateModel())
            {
                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (sender, args) =>
                {
                    spanContext = Tracer.Extract(BuiltinFormats.TextMap,
                        new RabbitMQHeadersMap(args.BasicProperties.Headers));
                    priority = args.BasicProperties.Priority;
                    message = args.Body.ToArray();
                };

                channel.BasicConsume(QueueName, false, consumer);
                await Task.Delay(TimeSpan.FromSeconds(2));
            }

            var extensions = new List<ICloudEventExtension>
            {
                new JaegerTracingExtension(spanContext),
                new RabbitMQPriorityExtension(priority)
            };

            return MotorCloudEvent.CreateTestCloudEvent(message, extensions: extensions.ToArray());
        }
    }
}
