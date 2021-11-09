using System.Threading.Tasks;
using TestContainers.Container.Abstractions;
using TestContainers.Container.Abstractions.Hosting;
using Xunit;

namespace Motor.Extensions.Hosting.SQS_IntegrationTest;

public class SQSFixture : IAsyncLifetime
{
    private readonly GenericContainer _sqsContainer;

    public SQSFixture()
    {
        _sqsContainer = new ContainerBuilder<SQSContainer>()
            .Build();
    }

    public string Hostname => _sqsContainer.GetDockerHostIpAddress();
    public int Port => _sqsContainer.GetMappedPort(SQSContainer.SQS_PORT);

    public async Task InitializeAsync()
    {
        await _sqsContainer.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _sqsContainer.StopAsync();
    }
}
