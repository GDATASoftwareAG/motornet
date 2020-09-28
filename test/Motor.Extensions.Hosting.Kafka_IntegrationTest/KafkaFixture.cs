using System.Threading.Tasks;
using DotNet.Testcontainers.Containers.Builders;
using DotNet.Testcontainers.Containers.Modules;
using DotNet.Testcontainers.Containers.WaitStrategies;
using Xunit;

namespace Motor.Extensions.Hosting.Kafka_IntegrationTest
{
    public class KafkaFixture : IAsyncLifetime
    {
        private readonly TestcontainersContainer _zookeeper;
        private TestcontainersContainer _kafka;

        public KafkaFixture()
        {
            _zookeeper = new TestcontainersBuilder<TestcontainersContainer>()
                .WithImage("confluentinc/cp-zookeeper:5.4.0")
                .WithEnvironment("ZOOKEEPER_CLIENT_PORT", "2181")
                .WithEnvironment("ZOOKEEPER_TICK_TIME", "2000")
                .WithPortBinding(2181, 2181)
                .WithWaitStrategy(Wait.ForUnixContainer())
                .Build();
        }

        public string Hostname => _kafka.Hostname;

        public async Task InitializeAsync()
        {
            await _zookeeper.StartAsync();

            var gateway = _zookeeper.Hostname;
            if (gateway == "localhost")
                gateway = "172.17.0.1";

            _kafka = new TestcontainersBuilder<TestcontainersContainer>()
                .WithImage("confluentinc/cp-kafka:5.4.0")
                .WithPortBinding(9092, 9092)
                .WithEnvironment("KAFKA_BROKER_ID", "1")
                .WithEnvironment("KAFKA_ZOOKEEPER_CONNECT", $"{gateway}:2181")
                .WithEnvironment("KAFKA_LISTENER_SECURITY_PROTOCOL_MAP", "PLAINTEXT:PLAINTEXT,PLAINTEXT_HOST:PLAINTEXT")
                .WithEnvironment("KAFKA_INTER_BROKER_LISTENER_NAME", "PLAINTEXT")
                .WithEnvironment("KAFKA_ADVERTISED_LISTENERS", $"PLAINTEXT://{gateway}:9092")
                .WithEnvironment("KAFKA_AUTO_CREATE_TOPICS_ENABLE", "true")
                .WithEnvironment("KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR", "1")
                .WithEnvironment("KAFKA_GROUP_INITIAL_REBALANCE_DELAY_MS", "100")
                .WithEnvironment("KAFKA_LOG_RETENTION_HOURS", "1")
                .WithWaitStrategy(Wait.ForUnixContainer())
                .Build();
            await _kafka.StartAsync();
        }

        public async Task DisposeAsync()
        {
            await Task.WhenAll(_kafka.StopAsync(), _zookeeper.StopAsync());
        }
    }
}
