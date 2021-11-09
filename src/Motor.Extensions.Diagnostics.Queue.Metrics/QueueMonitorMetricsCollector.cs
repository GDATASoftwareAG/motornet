using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Motor.Extensions.Diagnostics.Queue.Abstractions;
using Prometheus.Client;
using Prometheus.Client.Collectors;
using Prometheus.Client.MetricsWriter;

namespace Motor.Extensions.Diagnostics.Queue.Metrics;

internal class QueueMonitorMetricsCollector : ICollector
{
    private static readonly string[] Labels = { "queue" };

    internal static readonly CollectorConfiguration QueueMonitorCollectorConfig =
        new("motor_extensions_diagnostics_queue_metrics");

    private static readonly MetricConfiguration MessagesReadyGaugeConfig = new(
        "motor_extensions_diagnostics_queue_metrics_messages_ready",
        "Expose information about how many messages are ready to be processed",
        Labels,
        false
    );

    private static readonly MetricConfiguration ActiveConsumersGaugeConfig = new(
        "motor_extensions_diagnostics_queue_metrics_active_consumers",
        "Expose information about how many active consumers are listening on the queue",
        Labels,
        false
    );

    private readonly IEnumerable<IQueueMonitor> _queueMonitors;

    public QueueMonitorMetricsCollector(IEnumerable<IQueueMonitor> queueMonitors)
    {
        _queueMonitors = queueMonitors;
    }

    public void Collect(IMetricsWriter writer)
    {
        var tasks = _queueMonitors
            .Select(m => m.GetCurrentState());
        var states = Task.WhenAll(tasks).Result;
        WriteMessagesReadyMetric(writer, states);
        WriteActiveConsumersMetric(writer, states);
    }

    public CollectorConfiguration Configuration => QueueMonitorCollectorConfig;

    public IReadOnlyList<string> MetricNames => new[]
    {
            MessagesReadyGaugeConfig.Name,
            ActiveConsumersGaugeConfig.Name
        };

    private static void WriteMessagesReadyMetric(IMetricsWriter writer, IEnumerable<QueueState> states)
    {
        writer.WriteMetricHeader(MessagesReadyGaugeConfig.Name, MetricType.Gauge, MessagesReadyGaugeConfig.Help);

        foreach (var (queueName, readyMessages, _) in states)
        {
            writer.WriteSample(readyMessages, string.Empty, Labels, new[] { queueName });
        }

        writer.EndMetric();
    }

    private static void WriteActiveConsumersMetric(IMetricsWriter writer, IEnumerable<QueueState> states)
    {
        writer.WriteMetricHeader(ActiveConsumersGaugeConfig.Name, MetricType.Gauge, ActiveConsumersGaugeConfig.Help);

        foreach (var (queueName, _, consumerCount) in states)
        {
            writer.WriteSample(consumerCount, string.Empty, Labels, new[] { queueName });
        }

        writer.EndMetric();
    }
}
