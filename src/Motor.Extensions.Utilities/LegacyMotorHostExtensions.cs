using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Motor.Extensions.Utilities.Abstractions;

namespace Motor.Extensions.Utilities;

public static class StartupExtensions
{
    extension(IHostBuilder builder)
    {
        public IHostBuilder UseStartup<T>()
            where T : IMotorStartup
        {
            var startup = Activator.CreateInstance<T>();

            builder.ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder.UseSetting(WebHostDefaults.ApplicationKey, typeof(T).Assembly.GetName().Name);
                webHostBuilder.Configure(startup.Configure);
                webHostBuilder.ConfigureServices(startup.ConfigureServices);
            });

            return builder;
        }

        [Obsolete("Use IServiceCollection.AddHealthChecks().AddCheck<T>(...) instead")]
        public IHostBuilder AddHealthCheck<T>(
            string name,
            HealthStatus? failureStatus = null,
            IEnumerable<string>? tags = null,
            TimeSpan? timeout = null
        )
            where T : class, IHealthCheck =>
            builder.ConfigureServices(
                (_, services) =>
                {
                    services.AddHealthChecks().AddCheck<T>(name, failureStatus, tags, timeout);
                }
            );
    }
}
