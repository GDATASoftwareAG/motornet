using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Motor.Extensions.Diagnostics.Metrics.Abstractions;
using Motor.Extensions.Utilities.Abstractions;
using Prometheus.Client.AspNetCore;

namespace Motor.Extensions.Diagnostics.Metrics
{
    public static class PrometheusHostBuilderExtensions
    {
        public static IMotorHostBuilder ConfigurePrometheus(this IMotorHostBuilder hostBuilder)
        {
            hostBuilder
                .ConfigureServices((_, services) =>
                {
                    services.AddSingleton(typeof(IMetricsFactory<>), typeof(MetricsFactory<>));
                    services.AddSingleton<IMotorMetricsFactory, MotorMetricsFactory>();
                });
            return hostBuilder;
        }

        public static IApplicationBuilder UsePrometheusServer(this IApplicationBuilder applicationBuilder,
            bool useDefaultCollectors = true)
        {
            return applicationBuilder.UsePrometheusServer(prometheusOptions =>
            {
                prometheusOptions.UseDefaultCollectors = useDefaultCollectors;
            });
        }
    }
}
