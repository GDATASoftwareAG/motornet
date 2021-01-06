using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;
using Motor.Extensions.Hosting.Abstractions;
using Prometheus.Client;
using Prometheus.Client.AspNetCore;

namespace Motor.Extensions.Diagnostics.Metrics
{
    public interface IMotorMetricsFactory
    {
        MetricFactory Factory { get; }
    }

    public class MotorMetricsFactory : IMotorMetricsFactory
    {
        private readonly MetricFactory metricFactory;

        public MotorMetricsFactory(IOptions<PrometheusOptions> options, IApplicationNameService nameService)
        {
            var prometheusConfig = options.Value ??
                                   throw new ArgumentNullException(nameof(options), "Prometheus config doesn't exist.");
            prometheusConfig.CollectorRegistryInstance ??= Prometheus.Client.Metrics.DefaultCollectorRegistry;
            metricFactory = new MetricFactory(prometheusConfig.CollectorRegistryInstance);

            var counter = metricFactory.CreateCounter("motor_extensions_hosting_build_info",
                "A metric with a constant '1' value labeled by version, libversion, and framework from which the service was built.",
                "version", "libversion", "framework");
            counter.WithLabels(nameService.GetVersion(), nameService.GetLibVersion(),
                RuntimeInformation.FrameworkDescription).Inc();
        }

        public MetricFactory Factory => metricFactory;
    }
}
