using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Motor.Extensions.ContentEncoding.Abstractions;
using Motor.Extensions.Diagnostics.Logging;
using Motor.Extensions.Diagnostics.Metrics;
using Motor.Extensions.Diagnostics.Telemetry;
using Motor.Extensions.Hosting;
using Motor.Extensions.Hosting.CloudEvents;
using Motor.Extensions.Http;
using Motor.Extensions.Utilities.Abstractions;

namespace Motor.Extensions.Utilities;

public static class MotorHost
{
    private static IMotorHostBuilder ToMotorHostBuilder(
        this IHostBuilder hostBuilder,
        bool enableConfigureWebDefaults = true
    )
    {
        return new MotorHostBuilder(hostBuilder, enableConfigureWebDefaults);
    }

    public static IMotorHostBuilder CreateDefaultBuilder(
        Assembly? assembly = null,
        bool enableConfigureWebDefaults = true,
        string applicationName = "ApplicationName",
        string contentEncoding = "ContentEncoding"
    )
    {
        assembly ??= Assembly.GetCallingAssembly();

        var hostBuilder = Host.CreateDefaultBuilder().ToMotorHostBuilder(enableConfigureWebDefaults);

        return (IMotorHostBuilder)
            hostBuilder
                .ConfigureServices(
                    (ctx, collection) =>
                    {
                        collection.Configure<DefaultApplicationNameOptions>(
                            ctx.Configuration.GetSection(applicationName)
                        );
                        collection.Configure<ContentEncodingOptions>(ctx.Configuration.GetSection(contentEncoding));
                        collection.AddTransient<IApplicationNameService>(provider =>
                        {
                            var options = provider.GetRequiredService<IOptions<DefaultApplicationNameOptions>>();
                            return new DefaultApplicationNameService(assembly, options);
                        });
                    }
                )
                .ConfigureSerilog()
                .ConfigureOpenTelemetry()
                .ConfigurePrometheus()
                .ConfigureDefaultHttpClient()
                .UseConsoleLifetime();
    }
}
