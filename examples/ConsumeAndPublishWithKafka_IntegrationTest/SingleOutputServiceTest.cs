using Confluent.Kafka;
using ConsumeAndPublishWithKafka.Model;
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
        const string testMessage = """{"FancyText": "Baz","FancyNumber": 1337}""";
        await fixture.CreateTopicAsync(ConsumedTopic);
        await fixture.CreateTopicAsync(PublishedTopic);
        using var consumer = fixture.ConsumerFor(PublishedTopic);
        await using var testHost = MotorTestHost.BasedOn<Program>()
            .Configure<KafkaConsumerOptions<InputMessage>>(o =>
            {
                o.BootstrapServers = fixture.BootstrapServers;
                o.AutoOffsetReset = AutoOffsetReset.Earliest;
            }).Configure<KafkaPublisherOptions<OutputMessage>>(o =>
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
}
