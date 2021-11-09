using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using CloudNative.CloudEvents.SystemTextJson;
using Microsoft.Extensions.Options;
using Moq;
using Motor.Extensions.Diagnostics.Telemetry;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.RabbitMQ;
using Motor.Extensions.Hosting.RabbitMQ.Options;
using Motor.Extensions.TestUtilities;
using RabbitMQ.Client;
using Xunit;

namespace Motor.Extensions.Hosting.RabbitMQ_UnitTest;

public class RabbitMQMessagePublisherTests
{
    private const string DefaultExchange = "exchange";

    [Fact]
    public async Task PublishMessageAsync_WithConfig_ConnectionFactoryIsSet()
    {
        var mock = GetDefaultConnectionFactoryMock<string>();
        var config = GetConfig();
        var publisher = GetPublisher(mock.Object, config);

        await publisher.PublishMessageAsync(MotorCloudEvent.CreateTestCloudEvent(Array.Empty<byte>()));

        mock.VerifyGet(x => x.CurrentChannel, Times.Exactly(1));
    }

    [Fact]
    public async Task PublishMessageAsync_WithConfig_ConnectionEstablished()
    {
        var rabbitConnectionFactoryMock = GetDefaultConnectionFactoryMock<string>();
        var publisher = GetPublisher(rabbitConnectionFactoryMock.Object, GetConfig());

        await publisher.PublishMessageAsync(MotorCloudEvent.CreateTestCloudEvent(Array.Empty<byte>()));

        rabbitConnectionFactoryMock.VerifyGet(x => x.CurrentChannel, Times.Exactly(1));
    }

    [Fact]
    public async Task PublishMessageAsync_WithConfig_ChannelEstablished()
    {
        var rabbitConnectionFactoryMock = GetDefaultConnectionFactoryMock<string>();
        var publisher = GetPublisher(rabbitConnectionFactoryMock.Object, GetConfig());

        await publisher.PublishMessageAsync(MotorCloudEvent.CreateTestCloudEvent(Array.Empty<byte>()));

        rabbitConnectionFactoryMock.Verify(x => x.CurrentChannel, Times.Exactly(1));
    }

    [Fact]
    public async Task PublishMessageAsync_WithConfig_BasicPropertiesAreCreated()
    {
        var modelMock = new Mock<IModel>();
        var rabbitConnectionFactoryMock = GetDefaultConnectionFactoryMock<string>(modelMock: modelMock);
        var publisher = GetPublisher(rabbitConnectionFactoryMock.Object, GetConfig());

        await publisher.PublishMessageAsync(MotorCloudEvent.CreateTestCloudEvent(Array.Empty<byte>()));

        modelMock.Verify(x => x.CreateBasicProperties(), Times.Exactly(1));
    }

    [Fact]
    public async Task PublishMessageAsync_WithConfig_BasicPropertiesAreSet()
    {
        var basicProperties = Mock.Of<IBasicProperties>();
        var modelMock = new Mock<IModel>();
        modelMock.Setup(x => x.CreateBasicProperties()).Returns(basicProperties);
        var rabbitConnectionFactoryMock =
            GetDefaultConnectionFactoryMock<string>(modelMock: modelMock, basicProperties: basicProperties);

        var publisher = GetPublisher(rabbitConnectionFactoryMock.Object, GetConfig());
        const byte priority = 1;

        var activity = new Activity(nameof(RabbitMQMessagePublisherTests));
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();

        var motorCloudEvent = MotorCloudEvent.CreateTestCloudEvent(Array.Empty<byte>());
        motorCloudEvent.SetActivity(activity);
        motorCloudEvent.SetRabbitMQPriority(priority);

        await publisher.PublishMessageAsync(motorCloudEvent);

        Assert.Equal(2, basicProperties.DeliveryMode);
        Assert.Equal(priority, basicProperties.Priority);
        var traceparent = Encoding.UTF8.GetString((byte[])basicProperties.Headers[
            $"{BasicPropertiesExtensions.CloudEventPrefix}{DistributedTracingExtension.TraceParentAttribute.Name}"]);
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
            GetDefaultConnectionFactoryMock<string>(modelMock: modelMock, basicProperties: basicProperties);
        var config = GetConfig();
        var publisher = GetPublisher(rabbitConnectionFactoryMock.Object, config);
        var message = Array.Empty<byte>();

        await publisher.PublishMessageAsync(MotorCloudEvent.CreateTestCloudEvent(message));

        modelMock.Verify(x => x.BasicPublish(config.PublishingTarget.Exchange,
            config.PublishingTarget.RoutingKey, true, basicProperties, message));
    }

    [Fact]
    public async Task PublishMessageAsync_CloudEventWithRoutingKeyExtension_MessagePublished()
    {
        const string customExchange = "cloud-event-exchange";
        const string customRoutingKey = "cloud-event-routing-key";

        var modelMock = new Mock<IModel>();
        modelMock.Setup(x => x.CreateBasicProperties()).Returns(Mock.Of<IBasicProperties>());
        var rabbitConnectionFactoryMock = GetDefaultConnectionFactoryMock<string>(modelMock: modelMock);
        var publisher = GetPublisher(rabbitConnectionFactoryMock.Object);

        var motorCloudEvent = MotorCloudEvent.CreateTestCloudEvent(Array.Empty<byte>());
        motorCloudEvent.SetRabbitMQBinding(customExchange, customRoutingKey);
        await publisher.PublishMessageAsync(motorCloudEvent);

        modelMock.Verify(x => x.BasicPublish(customExchange,
            customRoutingKey, true, It.IsAny<IBasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>()));
    }

    [Fact]
    public async Task PublishMessageAsync_CloudEventWithRoutingKeyExtensionAndOverwriteExchange_MessagePublished()
    {
        const string customExchange = "cloud-event-exchange";
        const string customRoutingKey = "cloud-event-routing-key";

        var modelMock = new Mock<IModel>();
        modelMock.Setup(x => x.CreateBasicProperties()).Returns(Mock.Of<IBasicProperties>());
        var rabbitConnectionFactoryMock = GetDefaultConnectionFactoryMock<string>(modelMock: modelMock);
        var publisher = GetPublisher(rabbitConnectionFactoryMock.Object, overwriteExchange: true);

        var motorCloudEvent = MotorCloudEvent.CreateTestCloudEvent(Array.Empty<byte>());
        motorCloudEvent.SetRabbitMQBinding(customExchange, customRoutingKey);
        await publisher.PublishMessageAsync(motorCloudEvent);

        modelMock.Verify(x => x.BasicPublish(DefaultExchange,
            customRoutingKey, true, It.IsAny<IBasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>()));
    }

    private IRawMessagePublisher<string> GetPublisher(
        IRabbitMQConnectionFactory<string> factory = null,
        RabbitMQPublisherOptions<string> options = null,
        bool overwriteExchange = false)
    {
        options ??= GetConfig(overwriteExchange);
        factory ??= GetDefaultConnectionFactoryMock<string>().Object;

        var configMock = new Mock<IOptions<RabbitMQPublisherOptions<string>>>();
        configMock.Setup(x => x.Value).Returns(options);
        return new RabbitMQMessagePublisher<string>(
            factory,
            configMock.Object,
            new JsonEventFormatter()
        );
    }

    private RabbitMQPublisherOptions<string> GetConfig(bool overwriteExchange = false)
    {
        return new()
        {
            Host = "host",
            VirtualHost = "vHost",
            User = "user",
            Password = "pw",
            OverwriteExchange = overwriteExchange,
            PublishingTarget = new RabbitMQBindingOptions
            {
                Exchange = DefaultExchange,
                RoutingKey = "routingKey"
            }
        };
    }

    private Mock<IRabbitMQConnectionFactory<T>> GetDefaultConnectionFactoryMock<T>(
        Mock<IConnection> connectionMock = null,
        Mock<IModel> modelMock = null,
        IBasicProperties basicProperties = null
    )
    {
        var rabbitConnectionFactoryMock = new Mock<IRabbitMQConnectionFactory<T>>();
        connectionMock ??= new Mock<IConnection>();
        modelMock ??= new Mock<IModel>();

        connectionMock
            .Setup(x => x.CreateModel())
            .Returns(modelMock.Object);

        modelMock
            .Setup(x => x.CreateBasicProperties())
            .Returns(basicProperties ?? new Mock<IBasicProperties>().Object);

        rabbitConnectionFactoryMock.Setup(f => f.CurrentConnection).Returns(connectionMock.Object);
        rabbitConnectionFactoryMock.Setup(f => f.CurrentChannel).Returns(modelMock.Object);
        rabbitConnectionFactoryMock.Setup(f => f.CreateConnection()).Returns(connectionMock.Object);
        rabbitConnectionFactoryMock.Setup(f => f.CreateModel()).Returns(modelMock.Object);

        return rabbitConnectionFactoryMock;
    }
}
