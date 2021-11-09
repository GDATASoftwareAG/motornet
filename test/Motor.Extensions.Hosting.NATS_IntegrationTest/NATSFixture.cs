using System.Threading.Tasks;
using TestContainers.Container.Abstractions;
using TestContainers.Container.Abstractions.Hosting;
using Xunit;

namespace Motor.Extensions.Hosting.NATS_IntegrationTest;

public class NATSFixture : IAsyncLifetime
{
    private readonly GenericContainer _container;

    public NATSFixture()
    {
        _container = new ContainerBuilder<NATSContainer>()
            .Build();
    }

    public string Hostname => _container.GetDockerHostIpAddress();
    public int Port => _container.GetMappedPort(NATSContainer.Port);

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.StopAsync();
    }
}
