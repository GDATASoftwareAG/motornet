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

        internal static readonly MetricConfiguration QueueMonitorGaugeConfig = new(
            "motor_extensions_diagnostics_queue_metrics_messages_ready",
            "Expose information about how many messages are ready to be processed",
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
            writer.WriteMetricHeader(QueueMonitorGaugeConfig.Name, MetricType.Gauge, QueueMonitorGaugeConfig.Help);
            foreach (var (queueName, readyMessages) in states)
            {
                writer.WriteSample(readyMessages, string.Empty, Labels, new[] { queueName });
            }

            writer.EndMetric();
        }

        public CollectorConfiguration Configuration => QueueMonitorGaugeConfig;

        public IReadOnlyList<string> MetricNames => new[] { QueueMonitorGaugeConfig.Name };
    }
}
