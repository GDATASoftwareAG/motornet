using System;
using Microsoft.Extensions.Configuration;
using Motor.Extensions.Hosting.RabbitMQ.Config;
using Xunit;

namespace Motor.Extensions.Hosting.RabbitMQ_IntegrationTest.Config
{
    public class RabbitMQConsumerConfigTests
    {
        private IConfiguration GetJsonConfig(string configName)
        {
            return new ConfigurationBuilder()
                .AddJsonFile($"configs/{configName}.json")
                .Build();
        }

        [Fact]
        public void BindConsumerConfig_ConfigWithoutQueue_ContainsAllValues()
        {
            var config = GetJsonConfig("consumer-no-queue");
            var consumerOptions = new RabbitMQConsumerConfig<string>();

            config.Bind(consumerOptions);

            Assert.Equal("hostname", consumerOptions.Host);
            Assert.Equal(10000, consumerOptions.Port);
            Assert.Equal("username", consumerOptions.User);
            Assert.Equal("password", consumerOptions.Password);
            Assert.Equal("vhost", consumerOptions.VirtualHost);
            Assert.Equal(10000, consumerOptions.PrefetchCount);
        }

        [Fact]
        public void BindConsumerConfig_ConfigWithQueueWithNoBindings_ContainsQueue()
        {
            var config = GetJsonConfig("consumer-queue");
            var consumerOptions = new RabbitMQConsumerConfig<string>();

            config.Bind(consumerOptions);

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
        public void BindConsumerConfig_ConfigWithRoutingKey_ContainsRoutingKey()
        {
            var config = GetJsonConfig("consumer-routing-key");
            var consumerOptions = new RabbitMQConsumerConfig<string>();

            config.Bind(consumerOptions);

            Assert.NotNull(consumerOptions.Queue.Bindings);
            Assert.Equal("exchange", consumerOptions.Queue.Bindings[0].Exchange);
            Assert.Equal("routing", consumerOptions.Queue.Bindings[0].RoutingKey);
            Assert.Equal("teststring", consumerOptions.Queue.Bindings[0].Arguments["string"]);
        }

        [Fact]
        public void BindConsumerConfig_ConfigWithDefaultValues_ContainDefaultValues()
        {
            var config = GetJsonConfig("consumer-default-values");
            var consumerOptions = new RabbitMQConsumerConfig<string>();

            config.Bind(consumerOptions);

            Assert.Equal(5672, consumerOptions.Port);
            Assert.Equal(0, TimeSpan.FromSeconds(60).CompareTo(consumerOptions.RequestedHeartbeat));
            Assert.Equal("", consumerOptions.VirtualHost);
            Assert.Equal(10, consumerOptions.PrefetchCount);
            Assert.True(consumerOptions.Queue.Durable);
            Assert.False(consumerOptions.Queue.AutoDelete);
            Assert.Equal(1_000_000, consumerOptions.Queue.MaxLength);
            Assert.Equal(255, consumerOptions.Queue.MaxPriority);
            Assert.Equal(86_400_000, consumerOptions.Queue.MessageTtl);
        }
    }
}
