using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Xunit;

namespace Motor.Extensions.Hosting.SQS_IntegrationTest;

public class SQSFixture : IAsyncLifetime
{
    private readonly TestcontainersContainer _sqsContainer;
    private const int SQSPort = 9324;

    public SQSFixture()
    {
        _sqsContainer = new TestcontainersBuilder<TestcontainersContainer>()
            .WithImage("roribio16/alpine-sqs:1.2.0")
            .WithPortBinding(SQSPort, true)
            .Build();
    }

    public string BaseSQSUrl => $"http://{_sqsContainer.Hostname}:{_sqsContainer.GetMappedPublicPort(SQSPort)}";

    public async Task InitializeAsync()
    {
        await _sqsContainer.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _sqsContainer.StopAsync();
    }
}
