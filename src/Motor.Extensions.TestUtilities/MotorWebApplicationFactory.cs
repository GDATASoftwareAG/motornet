using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Motor.Extensions.Utilities;
using Polly;

namespace Motor.Extensions.TestUtilities;

public class MotorWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup>
    where TStartup : class
{
    private readonly Action<IServiceCollection>? _overrideDependencies;
    private readonly Action<IServiceCollection>? _configureTestServices;

    public MotorWebApplicationFactory(
        Action<IServiceCollection>? overrideDependencies = null,
        Action<IServiceCollection>? configureTestServices = null
    )
    {
        _overrideDependencies = overrideDependencies;
        _configureTestServices = configureTestServices;
        Environment.SetEnvironmentVariable(
            $"{MotorHostDefaults.OptionsPrefix}{MotorHostDefaults.EnablePrometheusEndpointKey}",
            false.ToString()
        );
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            _overrideDependencies?.Invoke(services);
        });
        builder.ConfigureTestServices(services =>
        {
            _configureTestServices?.Invoke(services);
        });
        builder.UseEnvironment("Development");
    }

    public async Task<bool> IsHealthy()
    {
        var client = CreateClient();
        var response = await client.GetAsync("/health");
        return response.IsSuccessStatusCode;
    }

    public async Task WaitUntilHealthy()
    {
        CreateClient();

        var retryPolicy = Policy
            .HandleResult<bool>(healthy => !healthy)
            .WaitAndRetryAsync(10, retryAttempt => TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt)));

        await retryPolicy.ExecuteAsync(async () => await IsHealthy());
    }

    public override async ValueTask DisposeAsync()
    {
        try
        {
            await base.DisposeAsync();
        }
        catch (ObjectDisposedException)
        {
            /* Ignored due to Motor.NET issue #1283 */
        }
        GC.SuppressFinalize(this);
    }
}
