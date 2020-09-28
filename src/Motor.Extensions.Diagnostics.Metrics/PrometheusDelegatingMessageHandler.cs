using System.Threading;
using System.Threading.Tasks;
using Motor.Extensions.Diagnostics.Metrics.Abstractions;
using Motor.Extensions.Hosting.Abstractions;
using Prometheus.Client.Abstractions;

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
                metricsFactory.CreateCounter("message_processing_total", "Message processing status total", "status");
        }

        public override async Task<ProcessedMessageStatus> HandleMessageAsync(MotorCloudEvent<TInput> dataCloudEvent,
            CancellationToken token = default)
        {
            var processedMessageStatus = ProcessedMessageStatus.CriticalFailure;
            try
            {
                processedMessageStatus = await base.HandleMessageAsync(dataCloudEvent, token);
            }
            finally
            {
                _messageProcessingTotal.WithLabels(processedMessageStatus.ToString()).Inc();
            }

            return processedMessageStatus;
        }
    }
}
