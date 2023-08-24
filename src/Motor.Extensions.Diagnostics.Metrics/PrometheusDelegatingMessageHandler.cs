using System.Threading;
using System.Threading.Tasks;
using Motor.Extensions.Diagnostics.Metrics.Abstractions;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using Prometheus.Client;

namespace Motor.Extensions.Diagnostics.Metrics;

public class PrometheusDelegatingMessageHandler<TInput> : DelegatingMessageHandler<TInput>
    where TInput : class
{
    private readonly IMetricFamily<ISummary> _messageProcessing;

    public PrometheusDelegatingMessageHandler(IMetricsFactory<PrometheusDelegatingMessageHandler<TInput>> metricsFactory)
    {
        _messageProcessing =
            metricsFactory.CreateSummary("message_processing", "Message processing duration in ms", false, "status");
    }

    public override async Task<ProcessedMessageStatus> HandleMessageAsync(MotorCloudEvent<TInput> dataCloudEvent,
        CancellationToken token = default)
    {
        var processedMessageStatus = ProcessedMessageStatus.CriticalFailure;
        using (new AutoObserveStopwatch(() => _messageProcessing.WithLabels(processedMessageStatus.ToString())))
        {
            processedMessageStatus = await base.HandleMessageAsync(dataCloudEvent, token);
        }

        return processedMessageStatus;
    }
}
