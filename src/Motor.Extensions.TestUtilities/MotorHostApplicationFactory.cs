using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.TestHost;
using Motor.Extensions.Utilities.Abstractions;
using Xunit;

namespace Motor.Extensions.TestUtilities;

public class MotorHostApplicationFactory<TStartup> : IAsyncLifetime where TStartup : IMotorStartup
{
    private TestServer? _server;

    public Task InitializeAsync()
    {
        _server = new MotorTestServerBuilder<TStartup>().Build();
        return Task.CompletedTask;
    }

    public HttpClient CreateClient() => _server?.CreateClient()!;

    public Task DisposeAsync()
    {
        _server?.Dispose();
        return Task.CompletedTask;
    }
}
