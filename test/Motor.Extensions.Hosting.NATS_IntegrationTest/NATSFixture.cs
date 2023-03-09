using System.Threading.Tasks;
using Xunit;

namespace Motor.Extensions.Hosting.NATS_IntegrationTest;

public class NATSFixture : IAsyncLifetime
{
    private readonly NATSContainer _container = new NATSBuilder().Build();

    public string ConnectionString => $"{_container.Hostname}:{_container.GetMappedPublicPort(NATSBuilder.DefaultPort)}";

    public Task InitializeAsync()
    {
        return _container.StartAsync();
    }

    public Task DisposeAsync()
    {
        return _container.DisposeAsync().AsTask();
    }
}
