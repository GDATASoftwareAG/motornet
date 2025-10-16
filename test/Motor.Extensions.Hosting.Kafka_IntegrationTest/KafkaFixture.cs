using System;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Testcontainers.Kafka;
using Xunit;

namespace Motor.Extensions.Hosting.Kafka_IntegrationTest;

public class KafkaFixture : IAsyncLifetime
{
    private readonly KafkaContainer _container = new KafkaBuilder().WithImage("confluentinc/cp-kafka:6.1.9").Build();

    public string BootstrapServers => _container.GetBootstrapAddress();

    public async Task CreateTopicAsync(string topicName)
    {
        using var adminClient = new AdminClientBuilder(
            new AdminClientConfig { BootstrapServers = _container?.GetBootstrapAddress() }
        ).Build();

        var topicExists = adminClient.GetMetadata(TimeSpan.FromMinutes(1)).Topics.Any(t => t.Topic == topicName);
        if (!topicExists)
        {
            await adminClient.CreateTopicsAsync([new TopicSpecification { Name = topicName }]);
        }
    }
    
    public Task InitializeAsync()
    {
        return _container.StartAsync();
    }

    public Task DisposeAsync()
    {
        return _container.DisposeAsync().AsTask();
    }
}
