using System.Threading.Tasks;
using Xunit;

namespace Motor.Extensions.Hosting.SQS_IntegrationTest;

public class SQSFixture : IAsyncLifetime
{
    private readonly SQSContainer _container = new SQSBuilder().Build();

    public string BaseSQSUrl => _container.BaseSQSUrl;

    public Task InitializeAsync()
    {
        return _container.StartAsync();
    }

    public Task DisposeAsync()
    {
        return _container.DisposeAsync().AsTask();
    }
}
