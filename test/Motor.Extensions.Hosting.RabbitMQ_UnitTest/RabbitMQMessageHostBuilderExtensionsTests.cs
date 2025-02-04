using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Motor.Extensions.Conversion.Abstractions;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using Motor.Extensions.Hosting.Consumer;
using Motor.Extensions.Hosting.Publisher;
using Motor.Extensions.Hosting.RabbitMQ;
using Motor.Extensions.Utilities;
using Motor.Extensions.Utilities.Abstractions;
using Xunit;

namespace Motor.Extensions.Hosting.RabbitMQ_UnitTest;

public class RabbitMQMessageHostBuilderExtensionsTests
{
    private static IMotorHostBuilder GetHostBuilder()
    {
        return MotorHost.CreateDefaultBuilder()
            .ConfigureSingleOutputService<string, string>()
            .ConfigureServices((_, services) =>
            {
                services.AddTransient(_ =>
                {
                    var mock = new Mock<IApplicationNameService>();
                    mock.Setup(t => t.GetVersion()).Returns("test");
                    mock.Setup(t => t.GetLibVersion()).Returns("test");
                    mock.Setup(t => t.GetSource()).Returns(new Uri("motor://test"));
                    return mock.Object;
                });
            });
    }

    [Fact]
    public void AddRabbitMQWithConfig_ConsumersWithDifferentConfigAdded_ConsumersWithDifferentConfigPresent()
    {
        var consumers = GetHostBuilder()
            .ConfigureConsumer<string>((_, builder) =>
            {
                builder.AddRabbitMQWithConfig(GetConfigWithVHost("Host1"));
                builder.AddDeserializer<StringDeserializer>();
            })
            .ConfigureConsumer<string>((_, builder) =>
            {
                builder.AddRabbitMQWithConfig(GetConfigWithVHost("Host2"));
                builder.AddDeserializer<StringDeserializer>();
            })
            .Build().Services
            .GetServices<IMessageConsumer<string>>().ToList();

        Assert.Contains(consumers, c => ((RabbitMQMessageConsumer<string>)c).ConnectionFactory.VirtualHost == "Host1");
        Assert.Contains(consumers, c => ((RabbitMQMessageConsumer<string>)c).ConnectionFactory.VirtualHost == "Host2");
    }

    [Fact]
    public void AddRabbitMQWithConfig_PublishersWithDifferentConfigAdded_PublishersWithDifferentConfigPresent()
    {
        var publishers = GetHostBuilder()
            .ConfigurePublisher<string>((_, builder) =>
            {
                builder.AddRabbitMQWithConfig(GetConfigWithVHost("Host1"));
                builder.AddSerializer<StringSerializer>();
            })
            .ConfigurePublisher<string>((_, builder) =>
            {
                builder.AddRabbitMQWithConfig(GetConfigWithVHost("Host2"));
                builder.AddSerializer<StringSerializer>();
            })
            .Build().Services
            .GetServices<RabbitMQMessagePublisher<string>>().ToList();

        Assert.Contains(publishers, c => c.ConnectionFactory.VirtualHost == "Host1");
        Assert.Contains(publishers, c => c.ConnectionFactory.VirtualHost == "Host2");
    }

    [Fact]
    public void AddRabbitMQWithConfig_ConsumerAndPublisherWithDifferentConfigAdded_ConsumerAndPublisherWithDifferentConfigPresent()
    {
        var host = GetHostBuilder()
            .ConfigureConsumer<string>((_, builder) =>
            {
                builder.AddRabbitMQWithConfig(GetConfigWithVHost("Host1"));
                builder.AddDeserializer<StringDeserializer>();
            })
            .ConfigurePublisher<string>((_, builder) =>
            {
                builder.AddRabbitMQWithConfig(GetConfigWithVHost("Host2"));
                builder.AddSerializer<StringSerializer>();
            })
            .Build();

        var consumers = host.Services.GetServices<IMessageConsumer<string>>().ToList();
        var publishers = host.Services.GetServices<RabbitMQMessagePublisher<string>>().ToList();

        Assert.Contains(consumers, c => ((RabbitMQMessageConsumer<string>)c).ConnectionFactory.VirtualHost == "Host1");
        Assert.Contains(publishers, c => c.ConnectionFactory.VirtualHost == "Host2");
    }

    private static IConfiguration GetConfigWithVHost(string vHost)
    {
        var configDict = new Dictionary<string, string?>
            {
                { "Host", "localhost" },
                { "User", "guest" },
                { "Password", "guest" },
                { "Queue:Name", "Test" },
                { "VirtualHost", vHost },
                { "PublishingTarget:RoutingKey", "routing.key" },
                { "PublishingTarget:Exchange", "amq.topic" }
            };
        return new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();
    }

    private class StringSerializer : IMessageSerializer<string>
    {
        public byte[] Serialize(string message)
        {
            return Encoding.UTF8.GetBytes(message);
        }
    }

    private class StringDeserializer : IMessageDeserializer<string>
    {
        public string Deserialize(byte[] message)
        {
            return Encoding.UTF8.GetString(message);
        }

    }
}
