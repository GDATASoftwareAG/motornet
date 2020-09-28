using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CloudNative.CloudEvents;
using Microsoft.Extensions.Options;
using Moq;
using Motor.Extensions.Diagnostics.Tracing;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.RabbitMQ;
using Motor.Extensions.Hosting.RabbitMQ.Config;
using Motor.Extensions.TestUtilities;
using OpenTracing;
using OpenTracing.Mock;
using RabbitMQ.Client;
using Xunit;

namespace Motor.Extensions.Hosting.RabbitMQ_UnitTest
{
    public class RabbitMQMessagePublisherTests
    {
        private MockTracer FakeTracer => new MockTracer();


        [Fact]
        public async Task PublishMessageAsync_WithConfig_ConnectionFactoryIsSet()
        {
            var mock = GetDefaultConnectionFactoryMock();
            var config = GetConfig();
            var tracer = FakeTracer;
            var publisher = GetPublisher(tracer, mock.Object, config);

            await publisher.PublishMessageAsync(MotorCloudEvent.CreateTestCloudEvent(new byte [0]));

            mock.Verify(x => x.From(config), Times.Exactly(1));
        }

        [Fact]
        public async Task PublishMessageAsync_WithConfig_ConnectionEstablished()
        {
            var connectionFactoryMock = new Mock<IConnectionFactory>();
            var rabbitConnectionFactoryMock = GetDefaultConnectionFactoryMock(connectionFactoryMock);
            var tracer = FakeTracer;
            var publisher = GetPublisher(tracer, rabbitConnectionFactoryMock.Object, GetConfig());

            await publisher.PublishMessageAsync(MotorCloudEvent.CreateTestCloudEvent(new byte[0]));

            connectionFactoryMock.Verify(x => x.CreateConnection(), Times.Exactly(1));
        }

        [Fact]
        public async Task PublishMessageAsync_WithConfig_ChannelEstablished()
        {
            var connectionMock = new Mock<IConnection>();
            var rabbitConnectionFactoryMock = GetDefaultConnectionFactoryMock(connectionMock: connectionMock);
            var tracer = FakeTracer;
            var publisher = GetPublisher(tracer, rabbitConnectionFactoryMock.Object, GetConfig());

            await publisher.PublishMessageAsync(MotorCloudEvent.CreateTestCloudEvent(new byte[0]));

            connectionMock.Verify(x => x.CreateModel(), Times.Exactly(1));
        }

        [Fact]
        public async Task PublishMessageAsync_WithConfig_BasicPropertiesAreCreated()
        {
            var modelMock = new Mock<IModel>();
            var rabbitConnectionFactoryMock = GetDefaultConnectionFactoryMock(modelMock: modelMock);
            var tracer = FakeTracer;
            var publisher = GetPublisher(tracer, rabbitConnectionFactoryMock.Object, GetConfig());

            await publisher.PublishMessageAsync(MotorCloudEvent.CreateTestCloudEvent(new byte[0]));

            modelMock.Verify(x => x.CreateBasicProperties(), Times.Exactly(1));
        }

        [Fact]
        public async Task PublishMessageAsync_WithConfig_BasicPropertiesAreSet()
        {
            var basicProperties = Mock.Of<IBasicProperties>();
            var modelMock = new Mock<IModel>();
            modelMock.Setup(x => x.CreateBasicProperties()).Returns(basicProperties);
            var rabbitConnectionFactoryMock =
                GetDefaultConnectionFactoryMock(modelMock: modelMock, basicProperties: basicProperties);
            var tracer = FakeTracer;
            var start = tracer.BuildSpan(nameof(PublishMessageAsync_WithConfig_ConnectionFactoryIsSet)).Start();
            var publisher = GetPublisher(tracer, rabbitConnectionFactoryMock.Object, GetConfig());
            const byte priority = 1;
            var motorCloudEvent = MotorCloudEvent.CreateTestCloudEvent(new byte[0],
                extensions: new List<ICloudEventExtension>
                {
                    new RabbitMQPriorityExtension(priority), new JaegerTracingExtension(start.Context)
                }.ToArray());

            await publisher.PublishMessageAsync(motorCloudEvent);

            Assert.Equal(2, basicProperties.DeliveryMode);
            Assert.Equal(priority, basicProperties.Priority);
            Assert.Equal(start.Context.SpanId,
                Encoding.UTF8.GetString((byte[]) basicProperties.Headers["x-open-tracing-spanid"]));
            Assert.Equal(start.Context.TraceId,
                Encoding.UTF8.GetString((byte[]) basicProperties.Headers["x-open-tracing-traceid"]));
        }

        [Fact]
        public async Task PublishMessageAsync_WithConfig_MessagePublished()
        {
            var basicProperties = Mock.Of<IBasicProperties>();
            var modelMock = new Mock<IModel>();
            modelMock.Setup(x => x.CreateBasicProperties()).Returns(basicProperties);
            var rabbitConnectionFactoryMock =
                GetDefaultConnectionFactoryMock(modelMock: modelMock, basicProperties: basicProperties);
            var tracer = FakeTracer;
            var publisher = GetPublisher(tracer, rabbitConnectionFactoryMock.Object, GetConfig());
            var message = new byte[0];

            await publisher.PublishMessageAsync(MotorCloudEvent.CreateTestCloudEvent(message));

            modelMock.Verify(x => x.BasicPublish(GetConfig().PublishingTarget.Exchange,
                GetConfig().PublishingTarget.RoutingKey, true, basicProperties, message));
        }

        [Fact(Skip = "Needs It.IsAny<ReadOnlySpan<byte>>() but that is not allowed :(")]
        public async Task PublishMessageAsync_CloudEventWithRoutingKeyExtension_MessagePublished()
        {
            const string customExchange = "cloud-event-exchange";
            const string customRoutingKey = "cloud-event-routing-key";

            var modelMock = new Mock<IModel>();
            modelMock.Setup(x => x.CreateBasicProperties()).Returns(Mock.Of<IBasicProperties>());
            var rabbitConnectionFactoryMock = GetDefaultConnectionFactoryMock(modelMock: modelMock);
            var publisher = GetPublisher(FakeTracer, rabbitConnectionFactoryMock.Object);
            var extensions = new List<ICloudEventExtension>
            {
                new RabbitMQBindingConfigExtension(customExchange, customRoutingKey)
            };

            await publisher.PublishMessageAsync(
                MotorCloudEvent.CreateTestCloudEvent(new byte[0], extensions: extensions));

            modelMock.Verify(x => x.BasicPublish(customExchange,
                customRoutingKey, true, It.IsAny<IBasicProperties>(), It.IsAny<byte[]>()));
        }

        private ITypedMessagePublisher<byte[]> GetPublisher(ITracer tracer,
            IRabbitMQConnectionFactory connectionFactory = null,
            RabbitMQPublisherConfig<string> config = null)
        {
            connectionFactory ??= GetDefaultConnectionFactoryMock().Object;
            config ??= GetConfig();

            var configMock = new Mock<IOptions<RabbitMQPublisherConfig<string>>>();
            configMock.Setup(x => x.Value).Returns(config);
            return new RabbitMQMessagePublisher<string>(connectionFactory, configMock.Object, new JsonEventFormatter(),
                tracer);
        }

        private RabbitMQPublisherConfig<string> GetConfig()
        {
            return new RabbitMQPublisherConfig<string>
            {
                Host = "host",
                VirtualHost = "vHost",
                User = "user",
                Password = "pw",
                PublishingTarget = new RabbitMQBindingConfig
                {
                    Exchange = "exchange",
                    RoutingKey = "routingKey"
                }
            };
        }

        private Mock<IRabbitMQConnectionFactory> GetDefaultConnectionFactoryMock(
            Mock<IConnectionFactory> connectionFactoryMock = null, Mock<IConnection> connectionMock = null,
            Mock<IModel> modelMock = null, IBasicProperties basicProperties = null)
        {
            var rabbitConnectionFactoryMock = new Mock<IRabbitMQConnectionFactory>();
            connectionFactoryMock ??= new Mock<IConnectionFactory>();
            connectionMock ??= new Mock<IConnection>();
            modelMock ??= new Mock<IModel>();
            rabbitConnectionFactoryMock.Setup(x => x.From(It.IsAny<RabbitMQPublisherConfig<string>>()))
                .Returns(connectionFactoryMock.Object);
            connectionFactoryMock.Setup(x => x.CreateConnection()).Returns(connectionMock.Object);
            connectionMock.Setup(x => x.CreateModel()).Returns(modelMock.Object);
            modelMock.Setup(x => x.CreateBasicProperties())
                .Returns(basicProperties ?? new Mock<IBasicProperties>().Object);
            return rabbitConnectionFactoryMock;
        }
    }
}
