using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Motor.Extensions.Diagnostics.Sentry;
using Sentry.Serilog;
using Serilog;
using Serilog.Formatting.Json;

namespace Motor.Extensions.Diagnostics.Logging;

public static class DefaultHostBuilderExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder ConfigureMotorLogging()
        {
            builder.ConfigureSentry();

            var sentryOptions =
                builder.Configuration.GetSection("Sentry").Get<SentrySerilogOptions>() ?? new SentrySerilogOptions();

            builder.Services.AddSerilog(
                (_, loggerConfiguration) =>
                {
                    loggerConfiguration
                        .ReadFrom.Configuration(builder.Configuration)
                        .Enrich.FromLogContext()
                        .WriteTo.Console(new JsonFormatter(renderMessage: true))
                        .WriteTo.Sentry(opts =>
                        {
                            opts.Dsn = sentryOptions.Dsn;
                            opts.MinimumEventLevel = sentryOptions.MinimumEventLevel;
                            opts.MinimumBreadcrumbLevel = sentryOptions.MinimumBreadcrumbLevel;
                            opts.InitializeSdk = false;
                        });
                }
            );

            return builder;
        }
    }

    extension(IHostBuilder builder)
    {
        public IHostBuilder ConfigureSerilog(Action<HostBuilderContext, LoggerConfiguration>? configuration = null) =>
            builder
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
