using System.Threading;
using System.Threading.Tasks;
using Motor.Extensions.Diagnostics.Metrics.Abstractions;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using Prometheus.Client;

namespace Motor.Extensions.Diagnostics.Metrics
{
    public class PrometheusDelegatingMessageHandler<TInput> : DelegatingMessageHandler<TInput>
        where TInput : class
    {
        private readonly IMetricFamily<ICounter> _messageProcessingTotal;

        public PrometheusDelegatingMessageHandler(
            IMetricsFactory<PrometheusDelegatingMessageHandler<TInput>> metricsFactory)
        {
            _messageProcessingTotal =
                metricsFactory.CreateCounter("message_processing_total", "Message processing status total", false, "status");
        }

        public override async Task<ProcessedMessageStatus> HandleMessageAsync(MotorCloudEvent<TInput> dataCloudEvent,
            CancellationToken token = default)
        {
            var processedMessageStatus = ProcessedMessageStatus.CriticalFailure;
            using (new AutoIncCounter(() => _messageProcessingTotal.WithLabels(processedMessageStatus.ToString())))
            {
                processedMessageStatus = await base.HandleMessageAsync(dataCloudEvent, token);
            }

            return processedMessageStatus;
        }
    }
}
