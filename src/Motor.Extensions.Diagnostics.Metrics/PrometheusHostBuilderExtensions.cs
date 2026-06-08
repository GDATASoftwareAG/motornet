using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Motor.Extensions.Diagnostics.Metrics.Abstractions;
using Prometheus.Client.AspNetCore;

namespace Motor.Extensions.Diagnostics.Metrics;

public static class PrometheusHostBuilderExtensions
{
    extension(IHostBuilder builder)
    {
        public IHostBuilder ConfigurePrometheus()
        {
            builder.ConfigureServices(
                (_, services) =>
                {
                    services.AddSingleton(typeof(IMetricsFactory<>), typeof(MetricsFactory<>));
                    services.AddSingleton<IMotorMetricsFactory, MotorMetricsFactory>();
                }
            );
            return builder;
        }
    }

    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder ConfigurePrometheus()
        {
            builder.Services.AddSingleton(typeof(IMetricsFactory<>), typeof(MetricsFactory<>));
            builder.Services.AddSingleton<IMotorMetricsFactory, MotorMetricsFactory>();
            return builder;
        }
    }

    extension(IApplicationBuilder applicationBuilder)
    {
        public IApplicationBuilder UsePrometheusServer(bool useDefaultCollectors = true) =>
            applicationBuilder.UsePrometheusServer(prometheusOptions =>
            {
                prometheusOptions.UseDefaultCollectors = useDefaultCollectors;
            });
    }
}
