using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Motor.Extensions.Diagnostics.Metrics.Abstractions;
using Prometheus.Client;

namespace Motor.Extensions.Diagnostics.Metrics;

public sealed class MetricsFactory<T> : IMetricsFactory<T>
{
    private readonly MetricFactory _metricFactory;
    private readonly HashSet<string> _names = new();

    public MetricsFactory(IMotorMetricsFactory motorMetricsFactory)
    {
        _metricFactory = motorMetricsFactory.Factory;
    }

    public IEnumerable<string> Names => _names;

    private string PrependNamespace(string name)
    {
        var metricsName = $"{typeof(T).Namespace?.Replace(".", "_").ToLower()}_{name}";
        _names.Add(metricsName);
        return metricsName;
    }

    public ICounter CreateCounter(string name, string help, bool includeTimestamp = false)
    {
        var metricsName = PrependNamespace(name);
        return _metricFactory.CreateCounter(metricsName, help, includeTimestamp);
    }

    public IMetricFamily<ICounter, ValueTuple<string>> CreateCounter(
        string name,
        string help,
        string labelName,
        bool includeTimestamp = false
    )
    {
        var metricsName = PrependNamespace(name);
        return _metricFactory.CreateCounter(metricsName, help, labelName, includeTimestamp);
    }

    public IMetricFamily<ICounter, TLabels> CreateCounter<TLabels>(
        string name,
        string help,
        TLabels labelNames,
        bool includeTimestamp = false
    )
        where TLabels : struct, ITuple, IEquatable<TLabels>
    {
        var metricsName = PrependNamespace(name);
        return _metricFactory.CreateCounter(metricsName, help, labelNames, includeTimestamp);
    }

    public IMetricFamily<ICounter> CreateCounter(string name, string help, params string[] labelNames)
    {
        var metricsName = PrependNamespace(name);
        return _metricFactory.CreateCounter(metricsName, help, labelNames);
    }

    public IMetricFamily<ICounter> CreateCounter(
        string name,
        string help,
        bool includeTimestamp = false,
        params string[] labelNames
    )
    {
        var metricsName = PrependNamespace(name);
        return _metricFactory.CreateCounter(metricsName, help, includeTimestamp, labelNames);
    }

    public ICounter<long> CreateCounterInt64(string name, string help, bool includeTimestamp = false)
    {
        var metricsName = PrependNamespace(name);
        return _metricFactory.CreateCounterInt64(metricsName, help, includeTimestamp);
    }

    public IMetricFamily<ICounter<long>, ValueTuple<string>> CreateCounterInt64(
        string name,
        string help,
        string labelName,
        bool includeTimestamp = false
    )
    {
        var metricsName = PrependNamespace(name);
        return _metricFactory.CreateCounterInt64(metricsName, help, labelName, includeTimestamp);
    }

    public IMetricFamily<ICounter<long>, TLabels> CreateCounterInt64<TLabels>(
        string name,
        string help,
        TLabels labelNames,
        bool includeTimestamp = false
    )
        where TLabels : struct, ITuple, IEquatable<TLabels>
    {
        var metricsName = PrependNamespace(name);
        return _metricFactory.CreateCounterInt64(metricsName, help, labelNames, includeTimestamp);
    }

    public IMetricFamily<ICounter<long>> CreateCounterInt64(string name, string help, params string[] labelNames)
    {
        var metricsName = PrependNamespace(name);
        return _metricFactory.CreateCounterInt64(metricsName, help, labelNames);
    }

    public IMetricFamily<ICounter<long>> CreateCounterInt64(
        string name,
        string help,
        bool includeTimestamp = false,
        params string[] labelNames
    )
    {
        var metricsName = PrependNamespace(name);
        return _metricFactory.CreateCounterInt64(metricsName, help, includeTimestamp, labelNames);
    }

    public IGauge CreateGauge(string name, string help, bool includeTimestamp = false)
    {
        var metricsName = PrependNamespace(name);
        return _metricFactory.CreateGauge(metricsName, help, includeTimestamp);
    }

    public IMetricFamily<IGauge, TLabels> CreateGauge<TLabels>(
        string name,
        string help,
        TLabels labelNames,
        bool includeTimestamp = false
    )
        where TLabels : struct, ITuple, IEquatable<TLabels>
    {
        var metricsName = PrependNamespace(name);
        return _metricFactory.CreateGauge(metricsName, help, labelNames, includeTimestamp);
    }

    public IMetricFamily<IGauge> CreateGauge(string name, string help, params string[] labelNames)
    {
        var metricsName = PrependNamespace(name);
        return _metricFactory.CreateGauge(metricsName, help, labelNames);
    }

    public IMetricFamily<IGauge, ValueTuple<string>> CreateGauge(
        string name,
        string help,
        string labelName,
        bool includeTimestamp = false
    )
    {
        var metricsName = PrependNamespace(name);
        return _metricFactory.CreateGauge(metricsName, help, labelName, includeTimestamp);
    }

    public IMetricFamily<IGauge> CreateGauge(
        string name,
        string help,
        bool includeTimestamp = false,
        params string[] labelNames
    )
    {
        var metricsName = PrependNamespace(name);
        return _metricFactory.CreateGauge(metricsName, help, includeTimestamp, labelNames);
    }

    public IGauge<long> CreateGaugeInt64(string name, string help, bool includeTimestamp = false)
    {
        var metricsName = PrependNamespace(name);
        return _metricFactory.CreateGaugeInt64(metricsName, help, includeTimestamp);
    }

    public IMetricFamily<IGauge<long>, ValueTuple<string>> CreateGaugeInt64(
        string name,
        string help,
        string labelName,
        bool includeTimestamp = false
    )
    {
        var metricsName = PrependNamespace(name);
        return _metricFactory.CreateGaugeInt64(metricsName, help, labelName, includeTimestamp);
    }

    public IMetricFamily<IGauge<long>, TLabels> CreateGaugeInt64<TLabels>(
        string name,
        string help,
        TLabels labelNames,
        bool includeTimestamp = false
    )
        where TLabels : struct, ITuple, IEquatable<TLabels>
    {
        var metricsName = PrependNamespace(name);
        return _metricFactory.CreateGaugeInt64(metricsName, help, labelNames, includeTimestamp);
    }

    public IMetricFamily<IGauge<long>> CreateGaugeInt64(string name, string help, params string[] labelNames)
    {
        var metricsName = PrependNamespace(name);
        return _metricFactory.CreateGaugeInt64(metricsName, help, labelNames);
    }

    public IMetricFamily<IGauge<long>> CreateGaugeInt64(
        string name,
        string help,
        bool includeTimestamp = false,
        params string[] labelNames
    )
    {
        var metricsName = PrependNamespace(name);
        return _metricFactory.CreateGaugeInt64(metricsName, help, includeTimestamp, labelNames);
    }

    public IHistogram CreateHistogram(string name, string help, bool includeTimestamp = false, double[]? buckets = null)
    {
        var metricsName = PrependNamespace(name);
        return _metricFactory.CreateHistogram(metricsName, help, includeTimestamp, buckets);
    }

    public IMetricFamily<IHistogram, ValueTuple<string>> CreateHistogram(
        string name,
        string help,
        string labelName,
        bool includeTimestamp = false,
        double[]? buckets = null
    )
    {
        var metricsName = PrependNamespace(name);
        return _metricFactory.CreateHistogram(metricsName, help, labelName, includeTimestamp, buckets);
    }

    public IMetricFamily<IHistogram, TLabels> CreateHistogram<TLabels>(
        string name,
        string help,
        TLabels labelNames,
        bool includeTimestamp = false,
        double[]? buckets = null
    )
        where TLabels : struct, ITuple, IEquatable<TLabels>
    {
        var metricsName = PrependNamespace(name);
        return _metricFactory.CreateHistogram(metricsName, help, labelNames, includeTimestamp, buckets);
    }

    public IMetricFamily<IHistogram> CreateHistogram(string name, string help, params string[] labelNames)
    {
        var metricsName = PrependNamespace(name);
        return _metricFactory.CreateHistogram(metricsName, help, labelNames);
    }

    public IMetricFamily<IHistogram> CreateHistogram(
        string name,
        string help,
        double[]? buckets = null,
        params string[] labelNames
    )
    {
        var metricsName = PrependNamespace(name);
        return _metricFactory.CreateHistogram(metricsName, help, buckets, labelNames);
    }

    public IMetricFamily<IHistogram> CreateHistogram(
        string name,
        string help,
        bool includeTimestamp = false,
        params string[] labelNames
    )
    {
        var metricsName = PrependNamespace(name);
        return _metricFactory.CreateHistogram(metricsName, help, includeTimestamp, labelNames);
    }

    public IMetricFamily<IHistogram> CreateHistogram(
        string name,
        string help,
        bool includeTimestamp = false,
        double[]? buckets = null,
        params string[] labelNames
    )
    {
        var metricsName = PrependNamespace(name);
        return _metricFactory.CreateHistogram(metricsName, help, includeTimestamp, buckets, labelNames);
    }

    public IUntyped CreateUntyped(string name, string help, bool includeTimestamp = false)
    {
        var metricsName = PrependNamespace(name);
        return _metricFactory.CreateUntyped(metricsName, help, includeTimestamp);
    }

    public IMetricFamily<IUntyped, ValueTuple<string>> CreateUntyped(
        string name,
        string help,
        string labelName,
        bool includeTimestamp = false
    )
    {
        var metricsName = PrependNamespace(name);
        return _metricFactory.CreateUntyped(metricsName, help, labelName, includeTimestamp);
    }

    public IMetricFamily<IUntyped, TLabels> CreateUntyped<TLabels>(
        string name,
        string help,
        TLabels labelNames,
        bool includeTimestamp = false
    )
        where TLabels : struct, ITuple, IEquatable<TLabels>
    {
        var metricsName = PrependNamespace(name);
        return _metricFactory.CreateUntyped(metricsName, help, labelNames, includeTimestamp);
    }

    public IMetricFamily<IUntyped> CreateUntyped(string name, string help, params string[] labelNames)
    {
        var metricsName = PrependNamespace(name);
        return _metricFactory.CreateUntyped(metricsName, help, labelNames);
    }

    public IMetricFamily<IUntyped> CreateUntyped(
        string name,
        string help,
        bool includeTimestamp = false,
        params string[] labelNames
    )
    {
        var metricsName = PrependNamespace(name);
        return _metricFactory.CreateUntyped(metricsName, help, includeTimestamp, labelNames);
    }

    public IMetricFamily<ISummary> CreateSummary(string name, string help, params string[] labelNames)
    {
        var metricsName = PrependNamespace(name);
        return _metricFactory.CreateSummary(metricsName, help, labelNames);
    }

    public IMetricFamily<ISummary> CreateSummary(
        string name,
        string help,
        bool includeTimestamp = false,
        params string[] labelNames
    )
    {
        var metricsName = PrependNamespace(name);
        return _metricFactory.CreateSummary(metricsName, help, includeTimestamp, labelNames);
    }

    public IMetricFamily<ISummary> CreateSummary(
        string name,
        string help,
        string[] labelNames,
        IReadOnlyList<QuantileEpsilonPair> objectives,
        TimeSpan maxAge,
        int? ageBuckets,
        int? bufCap
    )
    {
        var metricsName = PrependNamespace(name);
        return _metricFactory.CreateSummary(metricsName, help, labelNames, objectives, maxAge, ageBuckets, bufCap);
    }

    public ISummary CreateSummary(
        string name,
        string help,
        bool includeTimestamp = false,
        IReadOnlyList<QuantileEpsilonPair>? objectives = null,
        TimeSpan? maxAge = null,
        int? ageBuckets = null,
        int? bufCap = null
    )
    {
        var metricsName = PrependNamespace(name);
        return _metricFactory.CreateSummary(
            metricsName,
            help,
            includeTimestamp,
            objectives,
            maxAge,
            ageBuckets,
            bufCap
        );
    }

    public IMetricFamily<ISummary, ValueTuple<string>> CreateSummary(
        string name,
        string help,
        string labelName,
        bool includeTimestamp = false,
        IReadOnlyList<QuantileEpsilonPair>? objectives = null,
        TimeSpan? maxAge = null,
        int? ageBuckets = null,
        int? bufCap = null
    )
    {
        var metricsName = PrependNamespace(name);
        return _metricFactory.CreateSummary(
            metricsName,
            help,
            labelName,
            includeTimestamp,
            objectives,
            maxAge,
            ageBuckets,
            bufCap
        );
    }

    public IMetricFamily<ISummary, TLabels> CreateSummary<TLabels>(
        string name,
        string help,
        TLabels labelNames,
        bool includeTimestamp = false,
        IReadOnlyList<QuantileEpsilonPair>? objectives = null,
        TimeSpan? maxAge = null,
        int? ageBuckets = null,
        int? bufCap = null
    )
        where TLabels : struct, ITuple, IEquatable<TLabels>
    {
        var metricsName = PrependNamespace(name);
        return _metricFactory.CreateSummary(
            metricsName,
            help,
            labelNames,
            includeTimestamp,
            objectives,
            maxAge,
            ageBuckets,
            bufCap
        );
    }

    public IMetricFamily<ISummary> CreateSummary(
        string name,
        string help,
        string[] labelNames,
        bool includeTimestamp,
        IReadOnlyList<QuantileEpsilonPair>? objectives = null,
        TimeSpan? maxAge = null,
        int? ageBuckets = null,
        int? bufCap = null
    )
    {
        var metricsName = PrependNamespace(name);
        return _metricFactory.CreateSummary(
            metricsName,
            help,
            labelNames,
            includeTimestamp,
            objectives,
            maxAge,
            ageBuckets,
            bufCap
        );
    }

    public void Release(string name)
    {
        var metricsName = PrependNamespace(name);
        _metricFactory.Release(metricsName);
    }

    public void Release<TMetric>(IMetricFamily<TMetric> metricFamily)
        where TMetric : IMetric
    {
        _metricFactory.Release(metricFamily);
    }

    public void Release<TMetric, TLabels>(IMetricFamily<TMetric, TLabels> metricFamily)
        where TMetric : IMetric
        where TLabels : struct, ITuple, IEquatable<TLabels>
    {
        _metricFactory.Release(metricFamily);
    }
}
