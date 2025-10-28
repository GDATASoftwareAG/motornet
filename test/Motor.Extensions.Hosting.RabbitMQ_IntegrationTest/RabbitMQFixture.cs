using System;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using Motor.Extensions.Hosting.RabbitMQ;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Testcontainers.RabbitMq;
using Xunit;

namespace Motor.Extensions.Hosting.RabbitMQ_IntegrationTest;

public class RabbitMQFixture : IAsyncLifetime
{
    private const int RabbitMqPort = 5672;

    private readonly RabbitMqContainer _container = new RabbitMqBuilder()
        .WithUsername("guest")
        .WithPassword("guest")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Server startup complete"))
        .Build();

    public Task InitializeAsync()
    {
        return _container.StartAsync();
    }

    public Task DisposeAsync()
    {
        return _container.DisposeAsync().AsTask();
    }

    public async Task<IConnection> ConnectionAsync() => await CreateConnectionAsync();

    public IRabbitMQConnectionFactory<T> ConnectionFactory<T>() =>
        new RabbitMQConnectionFactory<T>(CreateConnectionFactory());

    public int Port => _container.GetMappedPublicPort(RabbitMqPort);
    public string Hostname => _container.Hostname;

    private IConnectionFactory CreateConnectionFactory() =>
        new ConnectionFactory { Uri = new Uri(_container.GetConnectionString()) };

    private async Task<IConnection> CreateConnectionAsync()
    {
        return await Policy
            .Handle<BrokerUnreachableException>()
            .WaitAndRetryAsync(5, i => TimeSpan.FromSeconds(Math.Pow(2, i)))
            .ExecuteAsync(async () => await CreateConnectionFactory().CreateConnectionAsync());
    }
}
