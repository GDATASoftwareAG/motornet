using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CloudNative.CloudEvents.Extensions;
using Microsoft.Extensions.Logging;
using Motor.Extensions.Hosting.Abstractions;
using OpenTelemetry.Trace;

namespace Motor.Extensions.Diagnostics.Tracing
{
    public class TracingDelegatingMessageHandler<TInput> : DelegatingMessageHandler<TInput>
        where TInput : class
    {
        private readonly ILogger<TracingDelegatingMessageHandler<TInput>> _logger;

        private static readonly ActivitySource ActivitySource =
            new(OpenTelemetryOptions.DefaultActivitySourceName);

        public TracingDelegatingMessageHandler(ILogger<TracingDelegatingMessageHandler<TInput>> logger)
        {
            Activity.DefaultIdFormat = ActivityIdFormat.W3C;
            Activity.ForceDefaultIdFormat = true;
            _logger = logger;
        }

        public override async Task<ProcessedMessageStatus> HandleMessageAsync(MotorCloudEvent<TInput> dataCloudEvent,
            CancellationToken token = default)
        {
            ActivityContext parentContext = default;
            var extension = dataCloudEvent.GetExtensionOrCreate(() => new DistributedTracingExtension());
            if (extension.TraceParent is not null)
            {
                parentContext = extension.GetActivityContext();
            }
            using var activity = ActivitySource.StartActivity(nameof(HandleMessageAsync), ActivityKind.Server, parentContext);
            if (activity is null) return await base.HandleMessageAsync(dataCloudEvent, token);

            using (activity.Start())
            using (_logger.BeginScope("TraceId: {traceid}, SpanId: {spanid}",
                activity.TraceId, activity.SpanId))
            {
                var processedMessageStatus = ProcessedMessageStatus.CriticalFailure;
                try
                {
                    extension.SetActivity(activity);
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
                            activity.SetStatus(Status.Error.WithDescription(nameof(ProcessedMessageStatus.TemporaryFailure)));
                            break;
                        case ProcessedMessageStatus.InvalidInput:
                            activity.SetStatus(Status.Error.WithDescription(nameof(ProcessedMessageStatus.InvalidInput)));
                            break;
                        case ProcessedMessageStatus.CriticalFailure:
                            activity.SetStatus(Status.Error.WithDescription(nameof(ProcessedMessageStatus.CriticalFailure)));
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                return processedMessageStatus;
            }
        }
    }
}
