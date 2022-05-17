using System;
using Microsoft.Extensions.Configuration;
using Motor.Extensions.Hosting.RabbitMQ.Options;
using RabbitMQ.Client;
using Xunit;

namespace Motor.Extensions.Hosting.RabbitMQ_IntegrationTest.Options;

public class RabbitMQConsumerOptionsTests
{
    private IConfiguration GetJsonConfig(string configName)
    {
        return new ConfigurationBuilder()
            .AddJsonFile($"configs/{configName}.json")
            .Build();
    }

    [Fact]
    public void BindConsumerOptions_ConfigWithoutQueue_ContainsAllValues()
    {
        var config = GetJsonConfig("consumer-no-queue");
        var consumerOptions = new RabbitMQConsumerOptions<string>();

        config.Bind(consumerOptions);

        Assert.Equal("hostname", consumerOptions.Host);
        Assert.Equal(10000, consumerOptions.Port);
        Assert.Equal("username", consumerOptions.User);
        Assert.Equal("password", consumerOptions.Password);
        Assert.Equal("vhost", consumerOptions.VirtualHost);
        Assert.Equal(10000, consumerOptions.PrefetchCount);
    }

    [Fact]
    public void BindConsumerOptions_ConfigWithQueueWithNoBindings_ContainsQueue()
    {
        var jsonConfig = GetJsonConfig("consumer-queue");
        var consumerOptions = new RabbitMQConsumerOptions<string>();

        jsonConfig.Bind(consumerOptions);

        Assert.NotNull(consumerOptions.Queue);
        Assert.Empty(consumerOptions.Queue.Bindings);
        Assert.Equal("test", consumerOptions.Queue.Name);
        Assert.False(consumerOptions.Queue.Durable);
        Assert.True(consumerOptions.Queue.AutoDelete);
        Assert.Equal(10, consumerOptions.Queue.MaxLength);
        Assert.Equal(20, consumerOptions.Queue.MaxPriority);
        Assert.Equal(30, consumerOptions.Queue.MessageTtl);
        Assert.Equal("teststring", consumerOptions.Queue.Arguments["string"]);
    }

    [Fact]
    public void BindConsumerOptions_ConfigWithRoutingKey_ContainsRoutingKey()
    {
        var config = GetJsonConfig("consumer-routing-key");
        var consumerOptions = new RabbitMQConsumerOptions<string>();

        config.Bind(consumerOptions);

        Assert.NotNull(consumerOptions.Queue.Bindings);
        Assert.Equal("exchange", consumerOptions.Queue.Bindings[0].Exchange);
        Assert.Equal("routing", consumerOptions.Queue.Bindings[0].RoutingKey);
        Assert.Equal("teststring", consumerOptions.Queue.Bindings[0].Arguments["string"]);
    }

    [Fact]
    public void BindConsumerOptions_ConfigWithDefaultValues_ContainDefaultValues()
    {
        var config = GetJsonConfig("consumer-default-values");
        var consumerOptions = new RabbitMQConsumerOptions<string>();

        config.Bind(consumerOptions);

        Assert.Equal(AmqpTcpEndpoint.UseDefaultPort, consumerOptions.Port);
        Assert.Equal(0, TimeSpan.FromSeconds(60).CompareTo(consumerOptions.RequestedHeartbeat));
        Assert.Equal("", consumerOptions.VirtualHost);
        Assert.Equal(10, consumerOptions.PrefetchCount);
        Assert.True(consumerOptions.Queue.Durable);
        Assert.False(consumerOptions.Queue.AutoDelete);
        Assert.Equal(1_000_000, consumerOptions.Queue.MaxLength);
        Assert.Equal(255, consumerOptions.Queue.MaxPriority);
        Assert.Equal(86_400_000, consumerOptions.Queue.MessageTtl);
    }

    [Fact]
    public void BindConsumerOptions_ConfigWithQueueButWithoutDeadLetterExchange_DeadLetterExchangeIsNull()
    {
        var jsonConfig = GetJsonConfig("consumer-queue");
        var consumerOptions = new RabbitMQConsumerOptions<string>();

        jsonConfig.Bind(consumerOptions);

        Assert.Null(consumerOptions.Queue.DeadLetterExchange);
    }

    [Fact]
    public void BindConsumerOptions_ConfigWithDeadLetterExchange_ContainsExpectedValues()
    {
        var jsonConfig = GetJsonConfig("consumer-queue-with-dlx");
        var consumerOptions = new RabbitMQConsumerOptions<string>();

        jsonConfig.Bind(consumerOptions);

        Assert.Equal("testDlx", consumerOptions.Queue.DeadLetterExchange?.Name);
        Assert.Equal(5, consumerOptions.Queue.DeadLetterExchange?.MessageTtl);
        Assert.Equal(54321, consumerOptions.Queue.DeadLetterExchange?.MaxLength);
        Assert.Equal(1234, consumerOptions.Queue.DeadLetterExchange?.MaxLengthBytes);
        Assert.Equal(100, consumerOptions.Queue.DeadLetterExchange?.MaxPriority);
        Assert.Equal("some.exchange", consumerOptions.Queue.DeadLetterExchange.Binding.Exchange);
        Assert.Equal("some.routing.key", consumerOptions.Queue.DeadLetterExchange.Binding.RoutingKey);
    }

    [Fact]
    public void BindConsumerOptions_ConfigWithDeadLetterExchange_ContainsDefaultLimits()
    {
        var jsonConfig = GetJsonConfig("consumer-queue-with-dlx-default-limits");
        var consumerOptions = new RabbitMQConsumerOptions<string>();

        jsonConfig.Bind(consumerOptions);

        Assert.Empty(consumerOptions.Queue.DeadLetterExchange?.Name);
        Assert.Equal(86_400_000, consumerOptions.Queue.DeadLetterExchange?.MessageTtl);
        Assert.Equal(1_000_000, consumerOptions.Queue.DeadLetterExchange?.MaxLength);
        Assert.Equal(200 * 1024 * 1024, consumerOptions.Queue.DeadLetterExchange?.MaxLengthBytes);
        Assert.Equal(255, consumerOptions.Queue.DeadLetterExchange?.MaxPriority);
    }
}
