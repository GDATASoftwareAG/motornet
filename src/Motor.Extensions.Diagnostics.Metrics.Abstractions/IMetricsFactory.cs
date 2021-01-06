using System;
using System.Collections.Generic;
using Prometheus.Client.Abstractions;

namespace Motor.Extensions.Diagnostics.Metrics.Abstractions
{
    public interface IMetricsFactory<T> : IMetricFactory
    {
        IEnumerable<string> Names { get; }
    }
}
