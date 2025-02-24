using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Motor.Extensions.Hosting.RabbitMQ;
using Motor.Extensions.Hosting.RabbitMQ.Options;
using RabbitMQ.Client;
using Xunit;

namespace Motor.Extensions.Hosting.RabbitMQ_UnitTest;

public class RabbitMQConnectionFactoryTests
{
    [Fact]
    public void FromConsumerConfig_NullConfig_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            RabbitMQConnectionFactory<string>.From<RabbitMQConsumerOptions<string>>(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("  ")]
    [InlineData(null)]
    public void FromConsumerConfig_NullOrEmptyHost_Throws(string? host)
    {
        var cfg = GetConsumerConfig(host!, "user", "password", "vHost", "name", "exchange", "routingKey");

        Assert.Throws<ValidationException>(() => RabbitMQConnectionFactory<string>.From(cfg));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("  ")]
    [InlineData(null)]
    public void FromConsumerConfig_NullOrEmptyUser_Throws(string? user)
    {
        var cfg = GetConsumerConfig("host", user!, "password", "vHost", "name", "exchange", "routingKey");

        Assert.Throws<ValidationException>(() => RabbitMQConnectionFactory<string>.From(cfg));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("  ")]
    [InlineData(null)]
    public void FromConsumerConfig_NullOrEmptyPassword_Throws(string? password)
    {
        var cfg = GetConsumerConfig("host", "user", password!, "vHost", "name", "exchange", "routingKey");

        Assert.Throws<ValidationException>(() => RabbitMQConnectionFactory<string>.From(cfg));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("  ")]
    [InlineData(null)]
    public void FromConsumerConfig_NullOrEmptyVirtualHost_Throws(string? vHost)
    {
        var cfg = GetConsumerConfig("host", "user", "password", vHost!, "name", "exchange", "routingKey");

        Assert.Throws<ValidationException>(() => RabbitMQConnectionFactory<string>.From(cfg));
    }

    [Fact]
    public void FromConsumerConfig_NoQueueConfig_Throws()
    {
        var cfg = new RabbitMQConsumerOptions<string>
        {
            Host = "host",
            VirtualHost = "vHost",
            User = "user",
            Password = "password",
            Queue = null!
        };

        Assert.Throws<ValidationException>(() => RabbitMQConnectionFactory<string>.From(cfg));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("  ")]
    [InlineData(null)]
    public void FromConsumerConfig_NullOrEmptyQueueName_Throws(string? name)
    {
        var cfg = GetConsumerConfig("host", "user", "password", "vHost", name!, "exchange", "routingKey");

        Assert.Throws<ValidationException>(() => RabbitMQConnectionFactory<string>.From(cfg));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("  ")]
    [InlineData(null)]
    public void FromConsumerConfig_NullOrEmptyExchange_Throws(string? exchange)
    {
        var cfg = GetConsumerConfig("host", "user", "password", "vHost", "qName", exchange!, "routingKey");

        Assert.Throws<ValidationException>(() => RabbitMQConnectionFactory<string>.From(cfg));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("  ")]
    [InlineData(null)]
    public void FromConsumerConfig_NullOrEmptyRoutingKey_Throws(string? routingKey)
    {
        var cfg = GetConsumerConfig("host", "user", "password", "vHost", "qName", "exchange", routingKey!);

        Assert.Throws<ValidationException>(() => RabbitMQConnectionFactory<string>.From(cfg));
    }

    [Fact]
    public void FromConsumerConfig_CorrectConfig_ContainsAllValues()
    {
        const string host = "host";
        const string user = "User";
        const string password = "password";
        const string vHost = "vHost";
        const int port = 1000;
        var cfg = GetConsumerConfig(host, user, password, vHost, "test", "exchange", "test");
        cfg = cfg with { Port = port };

        var connectionFactory = RabbitMQConnectionFactory<string>.From(cfg);

        Assert.Equal(user, connectionFactory.UserName);
        Assert.Equal(password, connectionFactory.Password);
        Assert.Equal(vHost, connectionFactory.VirtualHost);
    }

    [Fact]
    public void FromPublisherConfig_NullConfig_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            RabbitMQConnectionFactory<string>.From<RabbitMQPublisherOptions<string>>(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("  ")]
    [InlineData(null)]
    public void FromPublisherConfig_NullOrEmptyHost_Throws(string? host)
    {
        var cfg = GetPublisherConfig(host!, "user", "password", "vHost", "exchange", "routingKey");

        Assert.Throws<ValidationException>(() => RabbitMQConnectionFactory<string>.From(cfg));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("  ")]
    [InlineData(null)]
    public void FromPublisherConfig_NullOrEmptyUser_Throws(string? user)
    {
        var cfg = GetPublisherConfig("host", user!, "password", "vHost", "exchange", "routingKey");

        Assert.Throws<ValidationException>(() => RabbitMQConnectionFactory<string>.From(cfg));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("  ")]
    [InlineData(null)]
    public void FromPublisherConfig_NullOrEmptyPassword_Throws(string? password)
    {
        var cfg = GetPublisherConfig("host", "user", password!, "vHost", "exchange", "routingKey");

        Assert.Throws<ValidationException>(() => RabbitMQConnectionFactory<string>.From(cfg));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("  ")]
    [InlineData(null)]
    public void FromPublisherConfig_NullOrEmptyVirtualHost_Throws(string? vHost)
    {
        var cfg = GetPublisherConfig("host", "user", "password", vHost!, "exchange", "routingKey");

        Assert.Throws<ValidationException>(() => RabbitMQConnectionFactory<string>.From(cfg));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("  ")]
    [InlineData(null)]
    public void FromPublisherConfig_NullOrEmptyExchange_Throws(string? exchange)
    {
        var cfg = GetPublisherConfig("host", "user", "password", "vHost", exchange!, "routingKey");

        Assert.Throws<ValidationException>(() => RabbitMQConnectionFactory<string>.From(cfg));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("  ")]
    [InlineData(null)]
    public void FromPublisherConfig_NullOrEmptyRoutingKey_Throws(string? routingKey)
    {
        var cfg = GetPublisherConfig("host", "user", "password", "vHost", "exchange", routingKey!);

        Assert.Throws<ValidationException>(() => RabbitMQConnectionFactory<string>.From(cfg));
    }

    [Fact]
    public void FromPublisherConfig_CorrectConfig_ContainsAllValues()
    {
        const string host = "host";
        const string user = "User";
        const string password = "password";
        const string vHost = "vHost";
        const int port = 1000;
        var cfg = GetPublisherConfig(host, user, password, vHost, "exchange", "test");
        cfg = cfg with { Port = port };

        var connectionFactory = RabbitMQConnectionFactory<string>.From(cfg);

        Assert.Equal(user, connectionFactory.UserName);
        Assert.Equal(password, connectionFactory.Password);
        Assert.Equal(vHost, connectionFactory.VirtualHost);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task CreateConnection_ConnectionFactoryMock_AlwaysCreateNewConnection(int times)
    {
        var connectionFactoryMock = new Mock<IConnectionFactory>();
        var rabbitFactory = new RabbitMQConnectionFactory<string>(connectionFactoryMock.Object);
        var ct = CancellationToken.None;

        for (var i = 0; i < times; i++)
        {
            await rabbitFactory.CreateConnectionAsync(ct);
        }

        connectionFactoryMock.Verify(f => f.CreateConnectionAsync(ct), Times.Exactly(times));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task CurrentConnection_ConnectionFactoryMock_CreateConnectionOnlyOnce(int times)
    {
        var connectionMock = Mock.Of<IConnection>();
        var connectionFactoryMock = new Mock<IConnectionFactory>();
        var ct = CancellationToken.None;
        connectionFactoryMock
            .Setup(f => f.CreateConnectionAsync(ct))
            .ReturnsAsync(connectionMock);
        var rabbitFactory = new RabbitMQConnectionFactory<string>(connectionFactoryMock.Object);

        for (var i = 0; i < times; i++)
        {
            await rabbitFactory.CurrentConnectionAsync();
        }

        connectionFactoryMock.Verify(f => f.CreateConnectionAsync(ct), Times.Once);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task CreateChannel_ConnectionFactoryMock_AlwaysCreateNewChannel(int times)
    {
        var connectionMock = new Mock<IConnection>();
        var connectionFactoryMock = new Mock<IConnectionFactory>();
        var ct = CancellationToken.None;
        connectionFactoryMock
            .Setup(f => f.CreateConnectionAsync(ct))
            .ReturnsAsync(connectionMock.Object);
        var rabbitFactory = new RabbitMQConnectionFactory<string>(connectionFactoryMock.Object);

        for (var i = 0; i < times; i++)
        {
            await rabbitFactory.CreateChannelAsync(ct);
        }

        connectionMock.Verify(c => c.CreateChannelAsync(It.IsAny<CreateChannelOptions>(), ct), Times.Exactly(times));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task CurrentChannel_ConnectionFactoryMock_CreateChannelOnlyOnce(int times)
    {
        var connectionMock = new Mock<IConnection>();
        var connectionFactoryMock = new Mock<IConnectionFactory>();
        var channelMock = Mock.Of<IChannel>();
        var ct = CancellationToken.None;
        CreateChannelOptions? channelOptions = null;

        connectionFactoryMock
            .Setup(f => f.CreateConnectionAsync(ct))
            .ReturnsAsync(connectionMock.Object);

        connectionMock
            .Setup(c => c.CreateChannelAsync(channelOptions, ct))
            .ReturnsAsync(channelMock);

        var rabbitFactory = new RabbitMQConnectionFactory<string>(connectionFactoryMock.Object);

        for (var i = 0; i < times; i++)
        {
            await rabbitFactory.CurrentChannelAsync();
        }

        connectionMock.Verify(c => c.CreateChannelAsync(channelOptions, ct), Times.Once);
    }

    private static RabbitMQConsumerOptions<string> GetConsumerConfig(string host, string user, string password,
        string virtualHost,
        string name, string exchange, string routingKey)
    {
        return new RabbitMQConsumerOptions<string>
        {
            Host = host,
            VirtualHost = virtualHost,
            User = user,
            Password = password,
            Queue = new RabbitMQQueueOptions
            {
                Name = name,
                Bindings =
                [
                    new RabbitMQBindingOptions
                    {
                        Exchange = exchange,
                        RoutingKey = routingKey
                    }
                ]
            }
        };
    }

    private static RabbitMQPublisherOptions<string> GetPublisherConfig(string host, string user, string password,
        string virtualHost, string exchange, string routingKey)
    {
        return new RabbitMQPublisherOptions<string>
        {
            Host = host,
            Password = password,
            User = user,
            VirtualHost = virtualHost,
            PublishingTarget = new RabbitMQBindingOptions
            {
                Exchange = exchange,
                RoutingKey = routingKey
            }
        };
    }
}
