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

namespace Motor.Extensions.Utilities;

public static class MotorDefaults
{
    extension(IHostApplicationBuilder builder)
    {
        public void AddApplicationNameService(string applicationName = "ApplicationName", Assembly? assembly = null)
        {
            assembly ??= Assembly.GetCallingAssembly();

            builder.Services.Configure<DefaultApplicationNameOptions>(
                builder.Configuration.GetSection(applicationName)
            );

            builder.Services.AddTransient<IApplicationNameService>(provider =>
            {
                var options = provider.GetRequiredService<IOptions<DefaultApplicationNameOptions>>();
                return new DefaultApplicationNameService(assembly, options);
            });
        }

        public IHostApplicationBuilder AddMotorDefaults(
            string applicationName = "ApplicationName",
            string contentEncoding = "ContentEncoding",
            Assembly? assembly = null
        )
        {
            builder.AddApplicationNameService(applicationName, assembly);
            builder.Services.Configure<ContentEncodingOptions>(builder.Configuration.GetSection(contentEncoding));
            builder.ConfigureMotorLogging();
            builder.ConfigureMotorTelemetry();
            builder.ConfigurePrometheus();

            return builder;
        }
    }
}
