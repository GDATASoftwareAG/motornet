using Microsoft.Extensions.Configuration;
using Motor.Extensions.Hosting.RabbitMQ.Options;
using Xunit;

namespace Motor.Extensions.Hosting.RabbitMQ_IntegrationTest.Options;

public class RabbitMQPublisherConfigTests
{
    private IConfiguration GetJsonOptions(string configName)
    {
        return new ConfigurationBuilder()
            .AddJsonFile($"configs/{configName}.json")
            .Build();
    }

    [Fact]
    public void BindPublisherOptions_ConfigWithoutQueue_ContainsAllValues()
    {
        var config = GetJsonOptions("publisher-no-target");
        var publisherConfig = new RabbitMQPublisherOptions<string>();

        config.Bind(publisherConfig);

        Assert.Equal("hostname", publisherConfig.Host);
        Assert.Equal(10000, publisherConfig.Port);
        Assert.Equal("username", publisherConfig.User);
        Assert.Equal("password", publisherConfig.Password);
        Assert.Equal("vhost", publisherConfig.VirtualHost);
    }

    [Fact]
    public void BindPublisherOptions_ConfigWithTarget_ContainsPublishingTarget()
    {
        var options = GetJsonOptions("publisher-with-target");
        var publisherOptions = new RabbitMQPublisherOptions<string>();

        options.Bind(publisherOptions);

        Assert.NotNull(publisherOptions.PublishingTarget);
        Assert.Equal("exchange", publisherOptions.PublishingTarget.Exchange);
        Assert.Equal("routing", publisherOptions.PublishingTarget.RoutingKey);
        Assert.Equal("teststring", publisherOptions.PublishingTarget.Arguments["string"]);
    }
}
