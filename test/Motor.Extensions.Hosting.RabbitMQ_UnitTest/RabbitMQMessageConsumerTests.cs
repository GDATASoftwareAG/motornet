using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CloudNative.CloudEvents;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.RabbitMQ;
using Motor.Extensions.Hosting.RabbitMQ.Config;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OpenTracing.Mock;
using RabbitMQ.Client;
using Xunit;

namespace Motor.Extensions.Hosting.RabbitMQ_UnitTest
{
    public class RabbitMQMessageConsumerTests
    {
        private const int DefaultPrefetchCount = 777;
        
        
        private ILogger<RabbitMQMessageConsumer<string>> FakeLogger => new Mock<ILogger<RabbitMQMessageConsumer<string>>>().Object;
        private MockTracer FakeTracer => new MockTracer();
        private IHostApplicationLifetime FakeApplicationLifetime => new Mock<IHostApplicationLifetime>().Object;

        [Fact]
        public async Task StartAsync_CallbackNotConfigured_Throw()
        {
            var consumer = GetRabbitMQMessageConsumer();

            await Assert.ThrowsAsync<InvalidOperationException>(() => consumer.StartAsync());
        }

        [Fact]
        public async Task StartAsync_AlreadyStarted_Throw()
        {
            var consumer = GetRabbitMQMessageConsumer();
            SetConsumerCallback(consumer);
            await consumer.StartAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() => consumer.StartAsync());
        }

        [Fact]
        public async Task StartAsync_CallbackConfigured_ReturnCompletedTask()
        {
            var consumer = GetRabbitMQMessageConsumer();
            SetConsumerCallback(consumer);

            var actual = consumer.StartAsync();
            await actual;

            Assert.Equal(Task.CompletedTask, actual);
        }

        [Fact]
        public async Task StartAsync_CallbackConfigured_ConnectionFactoryIsSet()
        {
            var cfg = GetConfig();
            var rabbitConnectionFactoryMock = GetDefaultConnectionFactoryMock();
            var consumer = GetRabbitMQMessageConsumer(rabbitConnectionFactoryMock.Object, cfg);
            SetConsumerCallback(consumer);

            await consumer.StartAsync();

            rabbitConnectionFactoryMock.Verify(x => x.From(cfg), Times.Exactly(1));
        }

        [Fact]
        public async Task StartAsync_CallbackConfigured_ChannelConfigured()
        {
            var channelMock = new Mock<IModel>();
            var rabbitConnectionFactoryMock = GetDefaultConnectionFactoryMock(channelMock: channelMock);
            var consumer = GetRabbitMQMessageConsumer(rabbitConnectionFactoryMock.Object);
            SetConsumerCallback(consumer);

            await consumer.StartAsync();

            channelMock.Verify(x => x.BasicQos(0, DefaultPrefetchCount, false), Times.Exactly(1));
        }

        [Fact]
        public async Task StartAsync_CallbackConfigured_QueueDeclaredAndBind()
        {
            var channelMock = new Mock<IModel>();
            var rabbitConnectionFactoryMock = GetDefaultConnectionFactoryMock(channelMock: channelMock);
            var cfg = GetConfig();
            var expectedArguments = GetExpectedArgumentsFromConfig(cfg.Queue);
            var consumer = GetRabbitMQMessageConsumer(rabbitConnectionFactoryMock.Object, cfg);
            SetConsumerCallback(consumer);

            await consumer.StartAsync();

            channelMock.Verify(x =>
                    x.QueueDeclare(cfg.Queue.Name, cfg.Queue.Durable, false, cfg.Queue.AutoDelete, expectedArguments),
                Times.Exactly(1));
            channelMock.Verify(
                x => x.QueueBind(cfg.Queue.Name, cfg.Queue.Bindings.First().Exchange,
                    cfg.Queue.Bindings.First().RoutingKey, cfg.Queue.Bindings.First().Arguments), Times.Exactly(1));
        }

        [Fact]
        public async Task StartAsync_ConsumerConfigWithHasTwoBindings_QueueIsBoundTwice()
        {
            var channelMock = new Mock<IModel>();
            var rabbitConnectionFactoryMock = GetDefaultConnectionFactoryMock(channelMock: channelMock);
            var cfg = GetConfig();
            var consumer = GetRabbitMQMessageConsumer(rabbitConnectionFactoryMock.Object, cfg);
            SetConsumerCallback(consumer);

            await consumer.StartAsync();

            channelMock.Verify(
                x => x.QueueBind(cfg.Queue.Name, cfg.Queue.Bindings.First().Exchange,
                    cfg.Queue.Bindings.First().RoutingKey, cfg.Queue.Bindings.First().Arguments), Times.Exactly(1));
            channelMock.Verify(
                x => x.QueueBind(cfg.Queue.Name, cfg.Queue.Bindings.Last().Exchange,
                    cfg.Queue.Bindings.Last().RoutingKey, cfg.Queue.Bindings.Last().Arguments), Times.Exactly(1));
        }

        [Fact]
        public async Task StartAsync_CallbackConfigured_ChannelConsumed()
        {
            var channelMock = new Mock<IModel>();
            var rabbitConnectionFactoryMock = GetDefaultConnectionFactoryMock(channelMock: channelMock);
            var consumer = GetRabbitMQMessageConsumer(rabbitConnectionFactoryMock.Object, GetConfig());
            SetConsumerCallback(consumer);

            await consumer.StartAsync();

            channelMock.Verify(x => x.BasicConsume(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(),
                It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>(),
                It.IsAny<IBasicConsumer>()));
        }

        [Fact]
        public async Task StartAsync_DeclareQueueWithoutMaxPriority_QueueDeclared()
        {
            var channelMock = new Mock<IModel>();
            var rabbitConnectionFactoryMock = GetDefaultConnectionFactoryMock(channelMock: channelMock);
            var cfg = GetConfig();
            cfg.Queue.MaxPriority = null;
            var expectedArguments = GetExpectedArgumentsFromConfig(cfg.Queue);
            expectedArguments.Remove("x-max-priority");
            var consumer = GetRabbitMQMessageConsumer(rabbitConnectionFactoryMock.Object, cfg);
            SetConsumerCallback(consumer);

            await consumer.StartAsync();

            channelMock.Verify(x =>
                    x.QueueDeclare(cfg.Queue.Name, cfg.Queue.Durable, false, cfg.Queue.AutoDelete, expectedArguments),
                Times.Exactly(1));
        }

        [Fact]
        public async Task StartAsync_DeclareQueueWithoutMaxLength_QueueDeclared()
        {
            var channelMock = new Mock<IModel>();
            var rabbitConnectionFactoryMock = GetDefaultConnectionFactoryMock(channelMock: channelMock);
            var cfg = GetConfig();
            cfg.Queue.MaxLength = null;
            var expectedArguments = GetExpectedArgumentsFromConfig(cfg.Queue);
            expectedArguments.Remove("x-max-length");
            var consumer = GetRabbitMQMessageConsumer(rabbitConnectionFactoryMock.Object, cfg);
            SetConsumerCallback(consumer);

            await consumer.StartAsync();

            channelMock.Verify(x =>
                    x.QueueDeclare(cfg.Queue.Name, cfg.Queue.Durable, false, cfg.Queue.AutoDelete, expectedArguments),
                Times.Exactly(1));
        }

        [Fact]
        public async Task StartAsync_DeclareQueueWithoutMessageTtl_QueueDeclared()
        {
            var channelMock = new Mock<IModel>();
            var rabbitConnectionFactoryMock = GetDefaultConnectionFactoryMock(channelMock: channelMock);
            var cfg = GetConfig();
            cfg.Queue.MessageTtl = null;
            var expectedArguments = GetExpectedArgumentsFromConfig(cfg.Queue);
            expectedArguments.Remove("x-message-ttl");
            var consumer = GetRabbitMQMessageConsumer(rabbitConnectionFactoryMock.Object, cfg);
            SetConsumerCallback(consumer);

            await consumer.StartAsync();

            channelMock.Verify(x =>
                    x.QueueDeclare(cfg.Queue.Name, cfg.Queue.Durable, false, cfg.Queue.AutoDelete, expectedArguments),
                Times.Exactly(1));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(123456)]
        [InlineData(10_000_000_000)]
        public async Task StartAsync_DeclareQueueWithMaxLengthBytes_QueueDeclared(long maxLengthBytes)
        {
            var channelMock = new Mock<IModel>();
            var rabbitConnectionFactoryMock = GetDefaultConnectionFactoryMock(channelMock: channelMock);
            var cfg = GetConfig();
            cfg.Queue.MaxLengthBytes = maxLengthBytes;
            var consumer = GetRabbitMQMessageConsumer(rabbitConnectionFactoryMock.Object, cfg);
            SetConsumerCallback(consumer);

            await consumer.StartAsync();

            channelMock.Verify(
                x => x.QueueDeclare(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(),
                    It.Is<IDictionary<string, object>>(dict => (long) dict["x-max-length-bytes"] == maxLengthBytes)),
                Times.Exactly(1));
        }

        private static IDictionary<string, object> GetExpectedArgumentsFromConfig(RabbitMQQueueConfig config)
        {
            var expectedArguments = config.Arguments.ToDictionary(t => t.Key, t => t.Value);
            expectedArguments.Add("x-max-priority", config.MaxPriority);
            expectedArguments.Add("x-max-length", config.MaxLength);
            expectedArguments.Add("x-max-length-bytes", config.MaxLengthBytes);
            expectedArguments.Add("x-message-ttl", config.MessageTtl);
            return expectedArguments;
        }

        private IMessageConsumer<string> GetRabbitMQMessageConsumer(IRabbitMQConnectionFactory rabbitMqConnectionFactory = null,
            RabbitMQConsumerConfig<string> config = null, IHostApplicationLifetime applicationLifetime = null)
        {
            rabbitMqConnectionFactory ??= GetDefaultConnectionFactoryMock().Object;
            applicationLifetime ??= FakeApplicationLifetime;
            var optionsWrapper = new OptionsWrapper<RabbitMQConsumerConfig<string>>(config ?? GetConfig());
            
            return new RabbitMQMessageConsumer<string>(FakeLogger, rabbitMqConnectionFactory, optionsWrapper, 
                applicationLifetime, FakeTracer, GetApplicationNameService(), new JsonEventFormatter());
        }

        private IApplicationNameService GetApplicationNameService(string source = "test://non")
        {
            var mock = new Mock<IApplicationNameService>();
            mock.Setup(t => t.GetSource()).Returns(new Uri(source));
            return mock.Object;
        }

        private Mock<IRabbitMQConnectionFactory> GetDefaultConnectionFactoryMock(
            Mock<IConnectionFactory> connectionFactoryMock = null, Mock<IConnection> connectionMock = null,
            Mock<IModel> channelMock = null)
        {
            var rabbitConnectionFactoryMock = new Mock<IRabbitMQConnectionFactory>();
            connectionFactoryMock ??= new Mock<IConnectionFactory>();
            connectionMock ??= new Mock<IConnection>();
            channelMock ??= new Mock<IModel>();
            rabbitConnectionFactoryMock.Setup(x => x.From(It.IsAny<RabbitMQConsumerConfig<string>>()))
                .Returns(connectionFactoryMock.Object);
            connectionFactoryMock.Setup(x => x.CreateConnection()).Returns(connectionMock.Object);
            connectionMock.Setup(x => x.CreateModel()).Returns(channelMock.Object);
            return rabbitConnectionFactoryMock;
        }

        private RabbitMQConsumerConfig<string> GetConfig()
        {
            return new RabbitMQConsumerConfig<string>
            {
                Host = "someHost",
                Port = 12345,
                User = "user",
                Password = "pass",
                VirtualHost = "vHost",
                PrefetchCount = DefaultPrefetchCount,
                RequestedHeartbeat = TimeSpan.FromSeconds(111),
                Queue = new RabbitMQQueueConfig
                {
                    Name = "qName",
                    Durable = true,
                    AutoDelete = false,
                    Bindings = new[]
                    {
                        new RabbitMQBindingConfig
                        {
                            Exchange = "someExchange",
                            RoutingKey = "routingKey",
                            Arguments = new Dictionary<string, object>()
                        },
                        new RabbitMQBindingConfig
                        {
                            Exchange = "someOtherExchange",
                            RoutingKey = "someOtherRoutingKey",
                            Arguments = new Dictionary<string, object>()
                        }
                    }
                }
            };
        }

        private void SetConsumerCallback(IMessageConsumer<string> consumer)
        {
            consumer.ConsumeCallbackAsync = (context, bytes) => Task.FromResult(ProcessedMessageStatus.Success);
        }
    }
}
