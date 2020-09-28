using System;
using System.Threading.Tasks;
using DotNet.Testcontainers.Containers.Builders;
using DotNet.Testcontainers.Containers.Configurations.MessageBrokers;
using DotNet.Testcontainers.Containers.Modules.MessageBrokers;
using RabbitMQ.Client;
using Xunit;

namespace Motor.Extensions.Hosting.RabbitMQ_IntegrationTest
{
    public class RabbitMQFixture : IAsyncLifetime
    {
        public IConnection Connection => CreateConnection();

        public int Port => Container.GetMappedPublicPort(5672);
        public string Hostname => Container.Hostname;
        
        private IConnection CreateConnection()
        {
            var connectionFactory2 = new ConnectionFactory { Uri = new Uri(Container.ConnectionString) };
            return connectionFactory2.CreateConnection();
        }

        RabbitMqTestcontainer Container { get; }

        public RabbitMQFixture() =>
            Container = new TestcontainersBuilder<RabbitMqTestcontainer>()
                .WithMessageBroker(new RabbitMqTestcontainerConfiguration
                {
                    Username = "guest",
                    Port = new Random().Next(20000, 22000),
                    Password = "guest"
                })
                .Build();

        public Task InitializeAsync() => Container.StartAsync();

        public Task DisposeAsync() => Container.StopAsync();
    }
}
