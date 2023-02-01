using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Motor.Extensions.Hosting.RabbitMQ;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Xunit;

namespace Motor.Extensions.Hosting.RabbitMQ_IntegrationTest;

public class RabbitMQFixture : IAsyncLifetime
{
    private const int RabbitMqPort = 5672;
    public RabbitMQFixture()
    {
        var conf = new RabbitMqTestcontainerConfiguration("rabbitmq:3.11.7")
        {
            Password = "guest",
            Username = "guest"
        };
        Container = new TestcontainersBuilder<RabbitMqTestcontainer>()
            .WithPortBinding(RabbitMqPort, true)
            .WithMessageBroker(conf)
            .Build();
    }

    public IConnection Connection => CreateConnection();

    public IRabbitMQConnectionFactory<T> ConnectionFactory<T>() =>
        new RabbitMQConnectionFactory<T>(CreateConnectionFactory());

    public int Port => Container.GetMappedPublicPort(RabbitMqPort);
    public string Hostname => Container.Hostname;

    private RabbitMqTestcontainer Container { get; }

    public async Task InitializeAsync()
    {
        await Container.StartAsync();
        Policy
            .Handle<BrokerUnreachableException>()
            .WaitAndRetry(10, i => TimeSpan.FromSeconds(Math.Pow(2, i)))
            .Execute(CreateConnection);
    }

    public Task DisposeAsync()
    {
        return Container.StopAsync();
    }

    private IConnectionFactory CreateConnectionFactory() => new ConnectionFactory
    {
        Uri = new Uri(Container.ConnectionString)
    };

    private IConnection CreateConnection() => CreateConnectionFactory().CreateConnection();
}
