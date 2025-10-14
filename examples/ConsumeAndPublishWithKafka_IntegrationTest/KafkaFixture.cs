using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Testcontainers.Kafka;

namespace ConsumeAndPublishWithKafka_IntegrationTest;

public class KafkaFixture : IAsyncLifetime
{
    private readonly KafkaContainer _container = new KafkaBuilder().Build();

    public string BootstrapServers => _container.GetBootstrapAddress();

    private ProducerConfig? _producerConfig;
    private ConsumerConfig? _consumerConfig;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        _producerConfig = new ProducerConfig { BootstrapServers = _container.GetBootstrapAddress() };
        _consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _container.GetBootstrapAddress(),
            GroupId = "test",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };
    }

    public async Task CreateTopicAsync(string topicName)
    {
        using var adminClient = new AdminClientBuilder(
            new AdminClientConfig { BootstrapServers = _container.GetBootstrapAddress() }
        ).Build();

        var topicExists = adminClient.GetMetadata(TimeSpan.FromMinutes(1)).Topics.Any(t => t.Topic == topicName);
        if (!topicExists)
        {
            await adminClient.CreateTopicsAsync([new TopicSpecification { Name = topicName }]);
        }
    }

    public async Task ProduceAsync(string topic, string content)
    {
        using var producer = new ProducerBuilder<string, string>(_producerConfig).Build();
        await producer.ProduceAsync(topic, new Message<string, string> { Value = content });
    }

    public IConsumer<string, string> ConsumerFor(string topic)
    {
        var consumer = new ConsumerBuilder<string, string>(_consumerConfig).Build();
        consumer.Subscribe(topic);
        return consumer;
    }

    public Task DisposeAsync()
    {

        return _container.DisposeAsync().AsTask();
    }
}
