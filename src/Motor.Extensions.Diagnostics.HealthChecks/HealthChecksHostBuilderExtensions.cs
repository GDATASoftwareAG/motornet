using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Motor.Extensions.Diagnostics.HealthChecks
{
    public static class HealthChecksHostBuilderExtensions
    {
        public static IHostBuilder ConfigureHealthChecks(this IHostBuilder hostBuilder,
            Action<IHealthChecksBuilder> configure)
        {
            return hostBuilder
                .ConfigureServices(collection =>
                {
                    var healthChecksBuilder = collection.AddHealthChecks();
                    configure(healthChecksBuilder);
                });
        }
    }
}
