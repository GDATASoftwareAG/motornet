using Confluent.Kafka;
using ConsumeAndPublishWithKafka.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Motor.Extensions.Hosting.Kafka.Options;
using Motor.Extensions.TestUtilities;

namespace ConsumeAndPublishWithKafka_IntegrationTest;

public class SingleOutputServiceTest(KafkaFixture fixture) : IClassFixture<KafkaFixture>
{
    private const string ConsumedTopic = "my.input";
    private const string PublishedTopic = "my.output";

    [Fact]
    public async Task EndToEnd_Test()
    {
        Environment.SetEnvironmentVariable("USE_MOTOR_HOST", "true");
        const string testMessage = """{"FancyText": "Baz","FancyNumber": 1337}""";
        await fixture.CreateTopicAsync(ConsumedTopic);
        await fixture.CreateTopicAsync(PublishedTopic);
        using var consumer = fixture.ConsumerFor(PublishedTopic);
        await using var testHost = MotorTestHost
            .BasedOn<Program>()
            .Configure<KafkaConsumerOptions<InputMessage>>(o =>
            {
                o.BootstrapServers = fixture.BootstrapServers;
                o.AutoOffsetReset = AutoOffsetReset.Earliest;
            })
            .Configure<KafkaPublisherOptions<OutputMessage>>(o =>
            {
                o.BootstrapServers = fixture.BootstrapServers;
            })
            .Build();
        await testHost.WaitUntilHealthy();

        await fixture.ProduceAsync(ConsumedTopic, testMessage);

        const string expected = """{"NotSoFancyText":"zaB","NotSoFancyNumber":-1337}""";
        var actual = consumer.Consume().Message.Value;
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task EndToEnd_Test_NoMotorHost()
    {
        Environment.SetEnvironmentVariable("USE_MOTOR_HOST", "false");
        const string testMessage = """{"FancyText": "Baz","FancyNumber": 1337}""";
        await fixture.CreateTopicAsync(ConsumedTopic);
        await fixture.CreateTopicAsync(PublishedTopic);
        using var consumer = fixture.ConsumerFor(PublishedTopic);

        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.Configure(_ => { });
            builder.ConfigureServices(services =>
            {
                services.Configure<KafkaConsumerOptions<InputMessage>>(o =>
                {
                    o.BootstrapServers = fixture.BootstrapServers;
                    o.AutoOffsetReset = AutoOffsetReset.Earliest;
                });

                services.Configure<KafkaPublisherOptions<OutputMessage>>(o =>
                {
                    o.BootstrapServers = fixture.BootstrapServers;
                });
            });
        });

        factory.Server.AllowSynchronousIO = false;

        await fixture.ProduceAsync(ConsumedTopic, testMessage);

        const string expected = """{"NotSoFancyText":"zaB","NotSoFancyNumber":-1337}""";
        var actual = consumer.Consume().Message.Value;
        Assert.Equal(expected, actual);
    }
}
