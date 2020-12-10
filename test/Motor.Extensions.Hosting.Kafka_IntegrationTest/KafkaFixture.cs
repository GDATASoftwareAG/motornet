using System.Threading.Tasks;
using TestContainers.Container.Abstractions;
using TestContainers.Container.Abstractions.Hosting;
using Xunit;

namespace Motor.Extensions.Hosting.Kafka_IntegrationTest
{
    public class KafkaFixture : IAsyncLifetime
    {
        private readonly GenericContainer _kafka;

        public KafkaFixture()
        {
            _kafka =  new ContainerBuilder<KafkaContainer>()
                .Build();
        }

        public string Hostname => _kafka.GetDockerHostIpAddress();
        public int Port => _kafka.GetMappedPort(KafkaContainer.KAFKA_PORT);

        public async Task InitializeAsync()
        {
            await _kafka.StartAsync();
        }

        public async Task DisposeAsync()
        {
            await _kafka.StopAsync();
        }
    }
}
