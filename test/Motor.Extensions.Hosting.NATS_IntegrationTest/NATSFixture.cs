using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Xunit;

namespace Motor.Extensions.Hosting.NATS_IntegrationTest;

public class NATSFixture : IAsyncLifetime
{
    private readonly TestcontainersContainer _container;
    private const int NATSPort = 4222;

    public NATSFixture()
    {
        _container = new TestcontainersBuilder<TestcontainersContainer>()
            .WithImage("nats:2.9.11")
            .WithPortBinding(NATSPort, true)
            .Build();
    }

    public string Hostname => _container.Hostname;
    public int Port => _container.GetMappedPublicPort(NATSPort);

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.StopAsync();
    }
}
