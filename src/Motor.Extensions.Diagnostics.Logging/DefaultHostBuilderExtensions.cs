using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Motor.Extensions.Diagnostics.Sentry;
using Motor.Extensions.Utilities.Abstractions;
using Sentry.Serilog;
using Serilog;
using Serilog.Formatting.Json;

namespace Motor.Extensions.Diagnostics.Logging;

public static class DefaultHostBuilderExtensions
{
    public static IMotorHostBuilder ConfigureSerilog(
        this IMotorHostBuilder hostBuilder,
        Action<HostBuilderContext, LoggerConfiguration>? configuration = null
    )
    {
        return (IMotorHostBuilder)
            hostBuilder
                .ConfigureSentry()
                .UseSerilog(
                    (hostingContext, loggerConfiguration) =>
                    {
                        var sentryOptions = new SentrySerilogOptions();
                        hostingContext.Configuration.GetSection("Sentry").Bind(sentryOptions);
                        loggerConfiguration
                            .ReadFrom.Configuration(hostingContext.Configuration)
                            .Enrich.FromLogContext()
                            .WriteTo.Console(new JsonFormatter(renderMessage: true))
                            .WriteTo.Sentry(opts =>
                            {
                                opts.Dsn = sentryOptions.Dsn;
                                opts.MinimumEventLevel = sentryOptions.MinimumEventLevel;
                                opts.MinimumBreadcrumbLevel = sentryOptions.MinimumBreadcrumbLevel;
                                opts.InitializeSdk = false;
                            });
                        configuration?.Invoke(hostingContext, loggerConfiguration);
                    }
                )
                .ConfigureServices(
                    (_, services) =>
                    {
                        services.AddLogging(loggingBuilder =>
                        {
                            loggingBuilder.AddSerilog(dispose: true);
                        });
                    }
                );
    }
}
