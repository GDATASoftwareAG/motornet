using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Xunit;

namespace Motor.Extensions.Hosting.Kafka_IntegrationTest;

public class KafkaFixture : IAsyncLifetime
{
    private readonly KafkaTestcontainer _container;
    private const int KafkaPort = 9093;

    public KafkaFixture()
    {
        _container = new TestcontainersBuilder<KafkaTestcontainer>()
            .WithPortBinding(KafkaPort, true)
            .WithKafka(new KafkaTestcontainerConfiguration("confluentinc/cp-kafka:6.1.9"))
            .Build();
    }

    public string BootstrapServers => _container.BootstrapServers;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.StopAsync();
    }
}
