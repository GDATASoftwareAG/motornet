using System;
using System.Collections.Generic;
using System.Threading;
using Prometheus.Client;
using Prometheus.Client.Collectors;
using Prometheus.Client.MetricsWriter;

namespace Motor.Extensions.Diagnostics.Metrics;

internal class ThreadPoolGauge : ICollector
{
    private static readonly string[] Labels =
    {
            "ThreadPoolOption"
        };

    internal static readonly MetricConfiguration ThreadPoolGaugeConfig = new(
        "motor_extensions_diagnostics_metrics_threadpoolstate",
        "Expose information about the internal .NET global ThreadPool",
        Labels,
        false,
        TimeSpan.Zero
    );

    public void Collect(IMetricsWriter writer)
    {
        ThreadPool.GetMaxThreads(out var maxWorkerThreads, out var maxCompletionThreads);
        ThreadPool.GetMinThreads(out var minWorkerThreads, out var minCompletionThreads);
        ThreadPool.GetAvailableThreads(out var availableWorkerThreads, out var availableCompletionThreads);

        writer.WriteMetricHeader(ThreadPoolGaugeConfig.Name, MetricType.Gauge, ThreadPoolGaugeConfig.Help);
        WriteSample(writer, minWorkerThreads, nameof(minWorkerThreads));
        WriteSample(writer, minCompletionThreads, nameof(minCompletionThreads));
        WriteSample(writer, maxWorkerThreads, nameof(maxWorkerThreads));
        WriteSample(writer, maxCompletionThreads, nameof(maxCompletionThreads));
        WriteSample(writer, ThreadPool.ThreadCount, nameof(ThreadPool.ThreadCount));
        WriteSample(writer, availableWorkerThreads, nameof(availableWorkerThreads));
        WriteSample(writer, availableCompletionThreads, nameof(availableCompletionThreads));
        writer.EndMetric();
    }

    public CollectorConfiguration Configuration => ThreadPoolGaugeConfig;

    public IReadOnlyList<string> MetricNames => new[] { ThreadPoolGaugeConfig.Name };

    private void WriteSample(IMetricsWriter writer, double value, string labelValue)
    {
        writer.WriteSample(
            value,
            string.Empty,
            Labels,
            new[] { Capitalize(labelValue) });
    }

    private static string Capitalize(string labelValue) =>
        char.ToUpperInvariant(labelValue[0]) + labelValue.Substring(1);
}
