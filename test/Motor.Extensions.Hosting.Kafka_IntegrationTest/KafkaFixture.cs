using System.Threading.Tasks;
using TestContainers.Container.Abstractions;
using TestContainers.Container.Abstractions.Hosting;
using Xunit;

namespace Motor.Extensions.Hosting.Kafka_IntegrationTest
{
    public class KafkaFixture : IAsyncLifetime
    {
        private readonly GenericContainer _zookeeper;
        private GenericContainer _kafka;

        public KafkaFixture()
        {
            _zookeeper = new ContainerBuilder<GenericContainer>()
                .ConfigureDockerImageName("confluentinc/cp-zookeeper:5.4.0")
                .ConfigureContainer((_, container) =>
                {
                    container.Env["ZOOKEEPER_CLIENT_PORT"] = "2181";
                    container.Env["ZOOKEEPER_TICK_TIME"] = "2000";
                    
                    container.ExposedPorts.Add(2181);
                    container.PortBindings.Add(2181, 2181);
                })
                //.WithWaitStrategy(Wait.ForUnixContainer())
                .Build();
        }

        public string Hostname => _kafka.GetDockerHostIpAddress();

        public async Task InitializeAsync()
        {
            await _zookeeper.StartAsync();

            var gateway = "172.17.0.1";

            _kafka =  new ContainerBuilder<GenericContainer>()
                .ConfigureDockerImageName("confluentinc/cp-kafka:5.4.0")
                .ConfigureContainer((_, container) =>
                {
                    container.Env["KAFKA_BROKER_ID"] = "1";
                    container.Env["KAFKA_ZOOKEEPER_CONNECT"] = $"{gateway}:2181";
                    container.Env["KAFKA_LISTENER_SECURITY_PROTOCOL_MAP"] = "PLAINTEXT:PLAINTEXT,PLAINTEXT_HOST:PLAINTEXT";
                    container.Env["KAFKA_INTER_BROKER_LISTENER_NAME"] = "PLAINTEXT";
                    container.Env["KAFKA_ADVERTISED_LISTENERS"] = $"PLAINTEXT://{gateway}:9092";
                    container.Env["KAFKA_AUTO_CREATE_TOPICS_ENABLE"] = "true";
                    container.Env["KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR"] = "1";
                    container.Env["KAFKA_GROUP_INITIAL_REBALANCE_DELAY_MS"] = "100";
                    container.Env["KAFKA_LOG_RETENTION_HOURS"] = "1";
                    
                    container.ExposedPorts.Add(9092);
                    container.PortBindings.Add(9092, 9092);
                })
                .Build();
            await _kafka.StartAsync();
        }

        public async Task DisposeAsync()
        {
            await Task.WhenAll(_kafka.StopAsync(), _zookeeper.StopAsync());
        }
    }
}
