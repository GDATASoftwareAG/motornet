using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using OpenTelemetry.Trace;

namespace Motor.Extensions.Diagnostics.Telemetry;

public class TelemetryDelegatingMessageHandler<TInput> : DelegatingMessageHandler<TInput>
    where TInput : class
{
    private readonly ILogger<TelemetryDelegatingMessageHandler<TInput>> _logger;

    public TelemetryDelegatingMessageHandler(ILogger<TelemetryDelegatingMessageHandler<TInput>> logger)
    {
        _logger = logger;
    }

    public override async Task<ProcessedMessageStatus> HandleMessageAsync(
        MotorCloudEvent<TInput> dataCloudEvent,
        CancellationToken token = default
    )
    {
        var parentContext = dataCloudEvent.GetActivityContext();
        using var activity = OpenTelemetryOptions.ActivitySource.StartActivity(
            nameof(HandleMessageAsync),
            ActivityKind.Server,
            parentContext
        );
        if (activity is null)
        {
            return await base.HandleMessageAsync(dataCloudEvent, token);
        }

        using (activity.Start())
        using (_logger.BeginScope("TraceId: {traceid}, SpanId: {spanid}", activity.TraceId, activity.SpanId))
        {
            var processedMessageStatus = ProcessedMessageStatus.CriticalFailure;
            try
            {
                dataCloudEvent.SetActivity(activity);
                processedMessageStatus = await base.HandleMessageAsync(dataCloudEvent, token);
            }
            finally
            {
                activity.SetTag(nameof(ProcessedMessageStatus), processedMessageStatus.ToString());
                switch (processedMessageStatus)
                {
                    case ProcessedMessageStatus.Success:
                        activity.SetStatus(Status.Ok);
                        break;
                    case ProcessedMessageStatus.TemporaryFailure:
                        activity.SetStatus(
                            Status.Error.WithDescription(nameof(ProcessedMessageStatus.TemporaryFailure))
                        );
                        break;
                    case ProcessedMessageStatus.InvalidInput:
                        activity.SetStatus(Status.Error.WithDescription(nameof(ProcessedMessageStatus.InvalidInput)));
                        break;
                    case ProcessedMessageStatus.CriticalFailure:
                        activity.SetStatus(
                            Status.Error.WithDescription(nameof(ProcessedMessageStatus.CriticalFailure))
                        );
                        break;
                    case ProcessedMessageStatus.Failure:
                        activity.SetStatus(Status.Error.WithDescription(nameof(ProcessedMessageStatus.Failure)));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return processedMessageStatus;
        }
    }
}
