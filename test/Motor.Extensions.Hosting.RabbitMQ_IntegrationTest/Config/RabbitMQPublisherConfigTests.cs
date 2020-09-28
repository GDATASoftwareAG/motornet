using Microsoft.Extensions.Configuration;
using Motor.Extensions.Hosting.RabbitMQ.Config;
using Xunit;

namespace Motor.Extensions.Hosting.RabbitMQ_IntegrationTest.Config
{
    public class RabbitMQPublisherConfigTests
    {
        private IConfiguration GetJsonConfig(string configName)
        {
            return new ConfigurationBuilder()
                .AddJsonFile($"configs/{configName}.json")
                .Build();
        }

        [Fact]
        public void BindPublisherConfig_ConfigWithoutQueue_ContainsAllValues()
        {
            var config = GetJsonConfig("publisher-no-target");
            var publisherConfig = new RabbitMQPublisherConfig<string>();

            config.Bind(publisherConfig);

            Assert.Equal("hostname", publisherConfig.Host);
            Assert.Equal(10000, publisherConfig.Port);
            Assert.Equal("username", publisherConfig.User);
            Assert.Equal("password", publisherConfig.Password);
            Assert.Equal("vhost", publisherConfig.VirtualHost);
        }

        [Fact]
        public void BindPublisherConfig_ConfigWithTarget_ContainsPublishingTarget()
        {
            var config = GetJsonConfig("publisher-with-target");
            var publisherConfig = new RabbitMQPublisherConfig<string>();

            config.Bind(publisherConfig);

            Assert.NotNull(publisherConfig.PublishingTarget);
            Assert.Equal("exchange", publisherConfig.PublishingTarget.Exchange);
            Assert.Equal("routing", publisherConfig.PublishingTarget.RoutingKey);
            Assert.Equal("teststring", publisherConfig.PublishingTarget.Arguments["string"]);
        }
    }
}
