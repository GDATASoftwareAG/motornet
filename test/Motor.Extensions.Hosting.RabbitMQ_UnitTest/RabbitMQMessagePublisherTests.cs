using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
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
        await publisher.StartAsync();

        await publisher.PublishMessageAsync(MotorCloudEvent.CreateTestCloudEvent(Array.Empty<byte>()));

        mock.Verify(x => x.CurrentChannelAsync(), Times.Exactly(1));
    }

    [Fact]
    public async Task PublishMessageAsync_WithConfig_ConnectionEstablished()
    {
        var rabbitConnectionFactoryMock = GetDefaultConnectionFactoryMock<string>();
        var publisher = GetPublisher(rabbitConnectionFactoryMock.Object, GetConfig());
        await publisher.StartAsync();

        await publisher.PublishMessageAsync(MotorCloudEvent.CreateTestCloudEvent(Array.Empty<byte>()));

        rabbitConnectionFactoryMock.Verify(x => x.CurrentChannelAsync(), Times.Exactly(1));
    }

    [Fact]
    public async Task PublishMessageAsync_WithConfig_ChannelEstablished()
    {
        var rabbitConnectionFactoryMock = GetDefaultConnectionFactoryMock<string>();
        var publisher = GetPublisher(rabbitConnectionFactoryMock.Object, GetConfig());
        await publisher.StartAsync();

        await publisher.PublishMessageAsync(MotorCloudEvent.CreateTestCloudEvent(Array.Empty<byte>()));

        rabbitConnectionFactoryMock.Verify(x => x.CurrentChannelAsync(), Times.Exactly(1));
    }

    [Fact]
    public async Task PublishMessageAsync_WithConfig_BasicPropertiesAreSet()
    {
        var ct = CancellationToken.None;
        var channelMock = new Mock<IChannel>();
        var rabbitConnectionFactoryMock =
            GetDefaultConnectionFactoryMock<string>(channelMock: channelMock);

        var publisher = GetPublisher(rabbitConnectionFactoryMock.Object, GetConfig());
        await publisher.StartAsync(ct);
        const byte priority = 1;

        var activity = new Activity(nameof(RabbitMQMessagePublisherTests));
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();

        var motorCloudEvent = MotorCloudEvent.CreateTestCloudEvent(Array.Empty<byte>());
        motorCloudEvent.SetActivity(activity);
        motorCloudEvent.SetRabbitMQPriority(priority);

        await publisher.PublishMessageAsync(motorCloudEvent, ct);

        var matchesExpectedProperties = (BasicProperties basicProperties) =>
        {
            var traceParent = Encoding.UTF8.GetString(((byte[])basicProperties.Headers?[
                $"{BasicPropertiesExtensions.CloudEventPrefix}{DistributedTracingExtension.TraceParentAttribute.Name}"]!));
            var activityContext = ActivityContext.Parse(traceParent, null);

            return (
                activity.Context.TraceId == activityContext.TraceId
                && activity.Context.SpanId == activityContext.SpanId
                && activity.Context.TraceFlags == activityContext.TraceFlags
                && basicProperties is { DeliveryMode: DeliveryModes.Persistent, Priority: priority }
            );
        };
        channelMock.Verify(mock => mock.BasicPublishAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.Is<BasicProperties>(properties => matchesExpectedProperties(properties)),
            Array.Empty<byte>(),
            ct
            ), Times.Once);
    }

    [Fact]
    public async Task PublishMessageAsync_WithConfig_MessagePublished()
    {
        var ct = CancellationToken.None;
        var channelMock = new Mock<IChannel>();
        var rabbitConnectionFactoryMock =
            GetDefaultConnectionFactoryMock<string>(channelMock: channelMock);
        var config = GetConfig();
        var publisher = GetPublisher(rabbitConnectionFactoryMock.Object, config);
        await publisher.StartAsync(ct);
        var message = Array.Empty<byte>();

        await publisher.PublishMessageAsync(MotorCloudEvent.CreateTestCloudEvent(message), ct);

        channelMock.Verify(x => x.BasicPublishAsync(
            config.PublishingTarget.Exchange,
            config.PublishingTarget.RoutingKey,
            true,
            It.IsAny<BasicProperties>(),
            message,
            ct)
        );
    }

    [Fact]
    public async Task PublishMessageAsync_WithCloudEventFormatJson_MessagePublished()
    {
        var ct = CancellationToken.None;
        var channelMock = new Mock<IChannel>();
        var rabbitConnectionFactoryMock =
            GetDefaultConnectionFactoryMock<string>(channelMock: channelMock);
        var config = GetConfig();
        var publisher = GetPublisher(rabbitConnectionFactoryMock.Object, config, cloudEventFormat: CloudEventFormat.Json);
        await publisher.StartAsync(ct);
        var message = MotorCloudEvent.CreateTestCloudEvent(new byte[] { });

        await publisher.PublishMessageAsync(message, ct);

        var jsonEventFormatter = new JsonEventFormatter();
        var bytes = jsonEventFormatter.EncodeStructuredModeMessage(message.ConvertToCloudEvent(), out _);
        channelMock.Verify(x => x.BasicPublishAsync(
            config.PublishingTarget.Exchange,
            config.PublishingTarget.RoutingKey,
            true,
            It.IsAny<BasicProperties>(),
            It.Is<ReadOnlyMemory<byte>>(t => t.ToArray().SequenceEqual(bytes.ToArray())),
            ct)
        );
    }

    [Fact]
    public async Task PublishMessageAsync_CloudEventWithRoutingKeyExtension_MessagePublished()
    {
        const string customExchange = "cloud-event-exchange";
        const string customRoutingKey = "cloud-event-routing-key";

        var ct = CancellationToken.None;
        var channelMock = new Mock<IChannel>();
        var rabbitConnectionFactoryMock = GetDefaultConnectionFactoryMock<string>(channelMock: channelMock);
        var publisher = GetPublisher(rabbitConnectionFactoryMock.Object);
        await publisher.StartAsync(ct);

        var motorCloudEvent = MotorCloudEvent.CreateTestCloudEvent(Array.Empty<byte>());
        motorCloudEvent.SetRabbitMQBinding(customExchange, customRoutingKey);
        await publisher.PublishMessageAsync(motorCloudEvent, ct);

        channelMock.Verify(x => x.BasicPublishAsync(
            customExchange,
            customRoutingKey,
            true,
            It.IsAny<BasicProperties>(),
            It.IsAny<ReadOnlyMemory<byte>>(),
            ct)
        );
    }

    [Fact]
    public async Task PublishMessageAsync_CloudEventWithRoutingKeyExtensionAndOverwriteExchange_MessagePublished()
    {
        const string customExchange = "cloud-event-exchange";
        const string customRoutingKey = "cloud-event-routing-key";

        var ct = CancellationToken.None;
        var channelMock = new Mock<IChannel>();
        var rabbitConnectionFactoryMock = GetDefaultConnectionFactoryMock<string>(channelMock: channelMock);
        var publisher = GetPublisher(rabbitConnectionFactoryMock.Object, overwriteExchange: true);
        await publisher.StartAsync(ct);

        var motorCloudEvent = MotorCloudEvent.CreateTestCloudEvent(Array.Empty<byte>());
        motorCloudEvent.SetRabbitMQBinding(customExchange, customRoutingKey);
        await publisher.PublishMessageAsync(motorCloudEvent, ct);

        channelMock.Verify(x => x.BasicPublishAsync(
            DefaultExchange,
            customRoutingKey,
            true,
            It.IsAny<BasicProperties>(),
            It.IsAny<ReadOnlyMemory<byte>>(),
            ct)
        );
    }

    private static IRawMessagePublisher<string> GetPublisher(
        IRabbitMQConnectionFactory<string>? factory = null,
        RabbitMQPublisherOptions<string>? options = null,
        bool overwriteExchange = false,
        CloudEventFormat cloudEventFormat = CloudEventFormat.Protocol)
    {
        options ??= GetConfig(overwriteExchange);
        factory ??= GetDefaultConnectionFactoryMock<string>().Object;

        var optionsMock = Options.Create(options);
        return new RabbitMQMessagePublisher<string>(
            factory,
            optionsMock,
            Options.Create(new PublisherOptions { CloudEventFormat = cloudEventFormat }),
            new JsonEventFormatter()
        );
    }

    private static RabbitMQPublisherOptions<string> GetConfig(bool overwriteExchange = false)
    {
        return new RabbitMQPublisherOptions<string>
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

    private static Mock<IRabbitMQConnectionFactory<T>> GetDefaultConnectionFactoryMock<T>(
        Mock<IConnection>? connectionMock = null,
        Mock<IChannel>? channelMock = null
    )
    {
        var rabbitConnectionFactoryMock = new Mock<IRabbitMQConnectionFactory<T>>();
        connectionMock ??= new Mock<IConnection>();
        channelMock ??= new Mock<IChannel>();

        connectionMock
            .Setup(x => x.CreateChannelAsync(It.IsAny<CreateChannelOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(channelMock.Object);

        rabbitConnectionFactoryMock.Setup(f => f.CurrentConnectionAsync()).ReturnsAsync(connectionMock.Object);
        rabbitConnectionFactoryMock.Setup(f => f.CurrentChannelAsync()).ReturnsAsync(channelMock.Object);
        rabbitConnectionFactoryMock.Setup(f => f.CreateConnectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(connectionMock.Object);
        rabbitConnectionFactoryMock.Setup(f => f.CreateChannelAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(channelMock.Object);

        return rabbitConnectionFactoryMock;
    }
}
