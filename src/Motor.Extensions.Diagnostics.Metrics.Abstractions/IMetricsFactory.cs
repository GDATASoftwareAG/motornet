using System.Collections.Generic;
using Prometheus.Client;

namespace Motor.Extensions.Diagnostics.Metrics.Abstractions;

public interface IMetricsFactory<out T> : IMetricFactory
{
    IEnumerable<string> Names { get; }
}
