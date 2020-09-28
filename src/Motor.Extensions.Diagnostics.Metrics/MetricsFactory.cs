using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Motor.Extensions.Diagnostics.Metrics.Abstractions;
using Motor.Extensions.Hosting.Abstractions;
using Microsoft.Extensions.Options;
using Prometheus.Client;
using Prometheus.Client.Abstractions;
using Prometheus.Client.AspNetCore;
using Prometheus.Client.Collectors;

namespace Motor.Extensions.Diagnostics.Metrics
{
    public sealed class MetricsFactory<T> : IMetricsFactory<T>
    {
        private readonly MetricFactory _metricFactory;
        private readonly HashSet<string> _names = new HashSet<string>();

        public IEnumerable<string> Names => _names;

        public MetricsFactory(IOptions<PrometheusOptions> options, IApplicationNameService nameService)
        {
            var prometheusConfig = options?.Value ??
                                               throw new ArgumentNullException(nameof(options), "Prometheus config doesn't exist.");
            prometheusConfig.CollectorRegistryInstance ??= new CollectorRegistry();
            _metricFactory = new MetricFactory(prometheusConfig.CollectorRegistryInstance);
            
            var counter = _metricFactory.CreateCounter("motor_extensions_hosting_build_info",
                "A metric with a constant '1' value labeled by version, libversion, and framework from which the service was built.",
                "version", "libversion", "framework");
            counter.WithLabels(nameService.GetVersion(), nameService.GetLibVersion(), RuntimeInformation.FrameworkDescription).Inc();
        }

        public IMetricFamily<ICounter> CreateCounter(string name, string help, params string[] labels)
        {
            var metricsName = PrependNamespace(name);
            return _metricFactory.CreateCounter(metricsName, help, labels);
        }

        public IMetricFamily<IGauge> CreateGauge(string name, string help, params string[] labels)
        {
            var metricsName = PrependNamespace(name);
            return _metricFactory.CreateGauge(metricsName, help, labels);
        }

        public IMetricFamily<IHistogram> CreateHistogram(string name, string help, params string[] labels)
        {
            var metricsName = PrependNamespace(name);
            return _metricFactory.CreateHistogram(metricsName, help, labels);
        }

        public IMetricFamily<ISummary> CreateSummary(string name, string help, params string[] labels)
        {
            var metricsName = PrependNamespace(name);
            return _metricFactory.CreateSummary(metricsName, help, labels);
        }

        private string PrependNamespace(string name)
        {
            var metricsName =  $"{typeof(T).Namespace?.Replace(".", "_").ToLower()}_{name}";
            _names.Add(metricsName);
            return metricsName;
        }
    }
}
