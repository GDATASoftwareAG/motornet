using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.Extensions;
using Microsoft.Extensions.Options;
using Moq;
using Motor.Extensions.Diagnostics.Tracing;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.RabbitMQ;
using Motor.Extensions.Hosting.RabbitMQ.Options;
using Motor.Extensions.TestUtilities;
using RabbitMQ.Client;
using Xunit;

namespace Motor.Extensions.Hosting.RabbitMQ_UnitTest
{
    public class RabbitMQMessagePublisherTests
    {

        [Fact]
        public async Task PublishMessageAsync_WithConfig_ConnectionFactoryIsSet()
        {
            var mock = GetDefaultConnectionFactoryMock();
            var config = GetConfig();
            var publisher = GetPublisher(mock.Object, config);

            await publisher.PublishMessageAsync(MotorCloudEvent.CreateTestCloudEvent(new byte[0]));

            mock.Verify(x => x.From(config), Times.Exactly(1));
        }

        [Fact]
        public async Task PublishMessageAsync_WithConfig_ConnectionEstablished()
        {
            var connectionFactoryMock = new Mock<IConnectionFactory>();
            var rabbitConnectionFactoryMock = GetDefaultConnectionFactoryMock(connectionFactoryMock);
            var publisher = GetPublisher(rabbitConnectionFactoryMock.Object, GetConfig());

            await publisher.PublishMessageAsync(MotorCloudEvent.CreateTestCloudEvent(new byte[0]));

            connectionFactoryMock.Verify(x => x.CreateConnection(), Times.Exactly(1));
        }

        [Fact]
        public async Task PublishMessageAsync_WithConfig_ChannelEstablished()
        {
            var connectionMock = new Mock<IConnection>();
            var rabbitConnectionFactoryMock = GetDefaultConnectionFactoryMock(connectionMock: connectionMock);
            var publisher = GetPublisher(rabbitConnectionFactoryMock.Object, GetConfig());

            await publisher.PublishMessageAsync(MotorCloudEvent.CreateTestCloudEvent(new byte[0]));

            connectionMock.Verify(x => x.CreateModel(), Times.Exactly(1));
        }

        [Fact]
        public async Task PublishMessageAsync_WithConfig_BasicPropertiesAreCreated()
        {
            var modelMock = new Mock<IModel>();
            var rabbitConnectionFactoryMock = GetDefaultConnectionFactoryMock(modelMock: modelMock);
            var publisher = GetPublisher(rabbitConnectionFactoryMock.Object, GetConfig());

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

            var publisher = GetPublisher(rabbitConnectionFactoryMock.Object, GetConfig());
            const byte priority = 1;

            var openTelemetryExtension = new DistributedTracingExtension();

            var activity = new Activity(nameof(RabbitMQMessagePublisherTests));
            activity.SetIdFormat(ActivityIdFormat.W3C);
            activity.Start();
            openTelemetryExtension.SetActivity(activity);

            var motorCloudEvent = MotorCloudEvent.CreateTestCloudEvent(new byte[0],
                extensions: new List<ICloudEventExtension>
                {
                    new RabbitMQPriorityExtension(priority), openTelemetryExtension
                }.ToArray());

            await publisher.PublishMessageAsync(motorCloudEvent);

            Assert.Equal(2, basicProperties.DeliveryMode);
            Assert.Equal(priority, basicProperties.Priority);
            var traceparent = Encoding.UTF8.GetString((byte[])basicProperties.Headers[$"{BasicPropertiesExtensions.CloudEventPrefix}{DistributedTracingExtension.TraceParentAttributeName}"]).Trim('"');
            var activityContext = ActivityContext.Parse(traceparent, null);
            Assert.Equal(activity.Context.TraceId, activityContext.TraceId);
            Assert.Equal(activity.Context.SpanId, activityContext.SpanId);
            Assert.Equal(activity.Context.TraceFlags, activityContext.TraceFlags);
        }

        [Fact]
        public async Task PublishMessageAsync_WithConfig_MessagePublished()
        {
            var basicProperties = Mock.Of<IBasicProperties>();
            var modelMock = new Mock<IModel>();
            modelMock.Setup(x => x.CreateBasicProperties()).Returns(basicProperties);
            var rabbitConnectionFactoryMock =
                GetDefaultConnectionFactoryMock(modelMock: modelMock, basicProperties: basicProperties);
            var publisher = GetPublisher(rabbitConnectionFactoryMock.Object, GetConfig());
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
            var publisher = GetPublisher(rabbitConnectionFactoryMock.Object);
            var extensions = new List<ICloudEventExtension>
            {
                new RabbitMQBindingExtension(customExchange, customRoutingKey)
            };

            await publisher.PublishMessageAsync(
                MotorCloudEvent.CreateTestCloudEvent(new byte[0], extensions: extensions));

            modelMock.Verify(x => x.BasicPublish(customExchange,
                customRoutingKey, true, It.IsAny<IBasicProperties>(), It.IsAny<byte[]>()));
        }

        private ITypedMessagePublisher<byte[]> GetPublisher(
            IRabbitMQConnectionFactory connectionFactory = null,
            RabbitMQPublisherOptions<string> options = null)
        {
            connectionFactory ??= GetDefaultConnectionFactoryMock().Object;
            options ??= GetConfig();

            var configMock = new Mock<IOptions<RabbitMQPublisherOptions<string>>>();
            configMock.Setup(x => x.Value).Returns(options);
            return new RabbitMQMessagePublisher<string>(connectionFactory, configMock.Object, new JsonEventFormatter());
        }

        private RabbitMQPublisherOptions<string> GetConfig()
        {
            return new RabbitMQPublisherOptions<string>
            {
                Host = "host",
                VirtualHost = "vHost",
                User = "user",
                Password = "pw",
                PublishingTarget = new RabbitMQBindingOptions
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
            rabbitConnectionFactoryMock.Setup(x => x.From(It.IsAny<RabbitMQPublisherOptions<string>>()))
                .Returns(connectionFactoryMock.Object);
            connectionFactoryMock.Setup(x => x.CreateConnection()).Returns(connectionMock.Object);
            connectionMock.Setup(x => x.CreateModel()).Returns(modelMock.Object);
            modelMock.Setup(x => x.CreateBasicProperties())
                .Returns(basicProperties ?? new Mock<IBasicProperties>().Object);
            return rabbitConnectionFactoryMock;
        }
    }
}
