using System.Threading.Tasks;
using Testcontainers.Kafka;
using Xunit;

namespace Motor.Extensions.Hosting.Kafka_IntegrationTest;

public class KafkaFixture : IAsyncLifetime
{
    private readonly KafkaContainer _container = new KafkaBuilder().Build();

    public string BootstrapServers => _container.GetBootstrapAddress();

    public Task InitializeAsync()
    {
        return _container.StartAsync();
    }

    public Task DisposeAsync()
    {
        return _container.DisposeAsync().AsTask();
    }
}
