using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Motor.Extensions.Utilities;
using Motor.Extensions.Utilities.Abstractions;
using Xunit;

namespace Motor.Extensions.TestUtilities;

public class MotorHostApplicationFactory<TStartup> : IAsyncLifetime
    where TStartup : IMotorStartup
{
    private TestServer? _server;

    public Task InitializeAsync()
    {
        var useSetting = new Dictionary<string, string>
        {
            { MotorHostDefaults.EnablePrometheusEndpointKey, false.ToString() },
        };

        var webHostBuilder = new WebHostBuilder();
        MotorHostBuilderHelper.ConfigureWebHost(webHostBuilder, s => useSetting.GetValueOrDefault(s), typeof(TStartup));
        webHostBuilder.ConfigureServices(collection =>
        {
            collection.AddHealthChecks();
        });

        _server = new TestServer(webHostBuilder);
        return Task.CompletedTask;
    }

    public HttpClient CreateClient() => _server?.CreateClient()!;

    public Task DisposeAsync()
    {
        _server?.Dispose();
        return Task.CompletedTask;
    }
}
