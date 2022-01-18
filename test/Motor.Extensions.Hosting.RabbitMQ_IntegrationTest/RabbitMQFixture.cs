using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using Motor.Extensions.Hosting.RabbitMQ;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using TestContainers.Container.Abstractions;
using TestContainers.Container.Abstractions.Hosting;
using TestContainers.Container.Abstractions.Models;
using Xunit;

namespace Motor.Extensions.Hosting.RabbitMQ_IntegrationTest;

public enum ConnectionMode
{
    Plain,
    Tls
}

public class RabbitMQFixture : IAsyncLifetime
{
    private const string RabbitMqTlsConfigFile = "42-rabbitmq-tls.conf";
    private const int AmqpsPort = 5671;
    private const int AmqpPort = 5672;

    private readonly SslOption _sslOption;

    private readonly IDictionary<ConnectionMode, Lazy<IConnection>> _connLazies;

    public RabbitMQFixture()
    {
        _sslOption = new SslOption
        {
            Enabled = true,
            Version = SslProtocols.Tls12,
            ServerName = "localhost",
            AcceptablePolicyErrors = SslPolicyErrors.RemoteCertificateChainErrors |
                                     SslPolicyErrors.RemoteCertificateNameMismatch
        };

        _connLazies = new Dictionary<ConnectionMode, Lazy<IConnection>>
        {
            {
                ConnectionMode.Plain,
                new Lazy<IConnection>(CreateConnection)
            },
            {
                ConnectionMode.Tls,
                new Lazy<IConnection>(CreateTlsConnection)
            }
        };

        var testDir = Directory.GetCurrentDirectory();
        Container = new ContainerBuilder<GenericContainer>()
            .ConfigureDockerImageName("docker.io/rabbitmq:3.9.12")
            .ConfigureContainer((_, container) =>
            {
                container.AutoRemove = false;
                container.Env["RABBITMQ_DEFAULT_USER"] = "guest";
                container.Env["RABBITMQ_DEFAULT_PASS"] = "guest";
                container.BindMounts.Add(new Bind
                {
                    AccessMode = AccessMode.ReadOnly,
                    ContainerPath = "/tls",
                    HostPath = Path.Join(testDir, "ca", "certs")
                });
                container.BindMounts.Add(new Bind
                {
                    AccessMode = AccessMode.ReadOnly,
                    ContainerPath = $"/etc/rabbitmq/conf.d/{RabbitMqTlsConfigFile}",
                    HostPath = Path.Join(testDir, "configs", RabbitMqTlsConfigFile)
                });
                container.ExposedPorts.Add(5671);
                container.ExposedPorts.Add(5672);
            })
            .Build();
    }

    public IConnection Connection => CreateConnection();

    public IRabbitMQConnectionFactory<T> ConnectionFactory<T>() => new RabbitMQConnectionFactory<T>(CreateConnectionFactory(AmqpPort, new SslOption()));

    public int Port => Container.GetMappedPort(AmqpPort);

    public string Hostname => Container.GetDockerHostIpAddress();

    private GenericContainer Container { get; }

    public IConnection CreateConnection(ConnectionMode mode) => mode switch
    {
        ConnectionMode.Plain or ConnectionMode.Tls => _connLazies[mode].Value,
        _ => CreateConnectionFactory(AmqpsPort, _sslOption).CreateConnection()
    };

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
        using var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        return Container.StopAsync(tokenSource.Token);
    }

    private IConnectionFactory CreateConnectionFactory(int containerPort, SslOption sslOption) => new ConnectionFactory
    {
        Uri = new Uri($"amqp://guest:guest@{Hostname}:{Container.GetMappedPort(containerPort)}"),
        Ssl = sslOption
    };

    private IConnection CreateConnection() => CreateConnectionFactory(AmqpPort, new SslOption()).CreateConnection();
    private IConnection CreateTlsConnection() => CreateConnectionFactory(AmqpsPort, _sslOption).CreateConnection();
}
