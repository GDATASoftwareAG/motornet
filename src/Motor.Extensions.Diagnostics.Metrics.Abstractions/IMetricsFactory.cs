using System.Collections.Generic;
using Prometheus.Client.Abstractions;

namespace Motor.Extensions.Diagnostics.Metrics.Abstractions
{
    public interface IMetricsFactory<T>
    {
        IEnumerable<string> Names { get; }
        IMetricFamily<ICounter> CreateCounter(string name, string help, params string[] labels);
        IMetricFamily<IGauge> CreateGauge(string name, string help, params string[] labels);
        IMetricFamily<IHistogram> CreateHistogram(string name, string help, params string[] labels);
        IMetricFamily<ISummary> CreateSummary(string name, string help, params string[] labels);
    }
}
