using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Motor.Extensions.Diagnostics.Sentry;
using Motor.Extensions.Utilities.Abstractions;
using Sentry;
using Serilog;
using Serilog.Formatting.Json;

namespace Motor.Extensions.Diagnostics.Logging;

public static class DefaultHostBuilderExtensions
{
    public static IMotorHostBuilder ConfigureSerilog(this IMotorHostBuilder hostBuilder,
        Action<HostBuilderContext, LoggerConfiguration>? configuration = null)
    {
        return (IMotorHostBuilder)hostBuilder
            .ConfigureSentry()
            .UseSerilog((hostingContext, loggerConfiguration) =>
            {
                loggerConfiguration
                    .ReadFrom.Configuration(hostingContext.Configuration)
                    .Enrich.FromLogContext()
                    .WriteTo.Console(new JsonFormatter(renderMessage: true));
                configuration?.Invoke(hostingContext, loggerConfiguration);
            })
            .ConfigureServices((_, services) =>
            {
                services.AddLogging(loggingBuilder =>
                {
                    var builder = loggingBuilder
                        .AddSerilog(dispose: true);
                    if (SentrySdk.IsEnabled)
                    {
                        builder.AddSentry(o => o.InitializeSdk = false);
                    }
                });
            });
    }
}
