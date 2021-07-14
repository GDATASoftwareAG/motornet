using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Motor.Extensions.Diagnostics.Queue.Abstractions;
using Prometheus.Client;
using Prometheus.Client.Collectors;
using Prometheus.Client.MetricsWriter;

namespace Motor.Extensions.Diagnostics.Queue.Metrics
{
    internal class QueueMonitorGauge : ICollector
    {
        private static readonly string[] Labels = { "queue" };

        internal static readonly CollectorConfiguration QueueMonitorCollectorConfig =
            new("motor_extensions_diagnostics_queue_metrics");

        private static readonly MetricConfiguration QueueMonitorGaugeConfig = new(
            "motor_extensions_diagnostics_queue_metrics_messages_ready",
            "Expose information about how many messages are ready to be processed",
            Labels,
            false
        );

        private static readonly MetricConfiguration ConsumersMonitorGaugeConfig = new(
            "motor_extensions_diagnostics_queue_metrics_active_consumers",
            "Expose information about how many active consumers are listening on the queue",
            Labels,
            false
        );

        private readonly IEnumerable<IQueueMonitor> _queueMonitors;

        public QueueMonitorGauge(IEnumerable<IQueueMonitor> queueMonitors)
        {
            _queueMonitors = queueMonitors;
        }

        public void Collect(IMetricsWriter writer)
        {
            var tasks = _queueMonitors
                .Select(m => m.GetCurrentState());
            var states = Task.WhenAll(tasks).Result;
            WriteQueuesMetric(writer, states);
            WriteConsumersMetric(writer, states);
        }

        public CollectorConfiguration Configuration => QueueMonitorCollectorConfig;

        public IReadOnlyList<string> MetricNames => new[]
        {
            QueueMonitorGaugeConfig.Name,
            ConsumersMonitorGaugeConfig.Name
        };

        private static void WriteQueuesMetric(IMetricsWriter writer, IEnumerable<QueueState> states)
        {
            writer.WriteMetricHeader(QueueMonitorGaugeConfig.Name, MetricType.Gauge, QueueMonitorGaugeConfig.Help);
            foreach (var (queueName, readyMessages, _) in states)
            {
                writer.WriteSample(readyMessages, string.Empty, Labels, new[] { queueName });
            }

            writer.EndMetric();
        }

        private static void WriteConsumersMetric(IMetricsWriter writer, IEnumerable<QueueState> states)
        {
            writer.WriteMetricHeader(ConsumersMonitorGaugeConfig.Name, MetricType.Gauge, ConsumersMonitorGaugeConfig.Help);
            foreach (var (queueName, _, consumerCount) in states)
            {
                writer.WriteSample(consumerCount, string.Empty, Labels, new[] { queueName });
            }

            writer.EndMetric();
        }
    }
}
