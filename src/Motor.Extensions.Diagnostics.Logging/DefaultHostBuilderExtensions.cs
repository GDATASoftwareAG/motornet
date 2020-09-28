using System;
using Motor.Extensions.Utilities.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Formatting.Json;

namespace Motor.Extensions.Diagnostics.Logging
{
    public static class DefaultHostBuilderExtensions
    {
        public static IMotorHostBuilder ConfigureSerilog(this IMotorHostBuilder hostBuilder, Action<HostBuilderContext, LoggerConfiguration>? configuration = null)
        {
            return hostBuilder
                .UseSerilog((hostingContext, loggerConfiguration) =>
                {
                    loggerConfiguration
                        .ReadFrom.Configuration(hostingContext.Configuration)
                        .Enrich.FromLogContext()
                        .WriteTo.Console(new JsonFormatter(renderMessage: true));
                    configuration?.Invoke(hostingContext, loggerConfiguration);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));
                }) as IMotorHostBuilder;
        }
    }
}
