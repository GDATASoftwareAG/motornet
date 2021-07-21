using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using Prometheus.Client;
using Prometheus.Client.AspNetCore;
using Prometheus.Client.Collectors;

namespace Motor.Extensions.Diagnostics.Metrics
{
    public interface IMotorMetricsFactory
    {
        MetricFactory Factory { get; }
        ICollectorRegistry CollectorRegistry { get; }
    }

    public class MotorMetricsFactory : IMotorMetricsFactory
    {
        private readonly MetricFactory _metricFactory;
        private readonly ICollectorRegistry _collectorRegistry;

        public MotorMetricsFactory(
            IOptions<PrometheusOptions> options,
            IApplicationNameService nameService
        )
        {
            var prometheusConfig = options.Value ?? throw new ArgumentNullException(nameof(options), "Prometheus config doesn't exist.");
            _collectorRegistry = prometheusConfig.CollectorRegistryInstance ?? Prometheus.Client.Metrics.DefaultCollectorRegistry;

            _collectorRegistry.GetOrAdd(
                ThreadPoolGauge.ThreadPoolGaugeConfig,
                _ => new ThreadPoolGauge()
            );
            _metricFactory = new MetricFactory(_collectorRegistry);

            var counter = _metricFactory.CreateCounter(
                "motor_extensions_hosting_build_info",
                "A metric with a constant '1' value labeled by version, libversion, and framework from which the service was built.",
                "version", "libversion", "framework"
            );
            counter.WithLabels(
                nameService.GetVersion(),
                nameService.GetLibVersion(),
                RuntimeInformation.FrameworkDescription
            ).Inc();
        }

        public MetricFactory Factory => _metricFactory;
        public ICollectorRegistry CollectorRegistry => _collectorRegistry;
    }
}
