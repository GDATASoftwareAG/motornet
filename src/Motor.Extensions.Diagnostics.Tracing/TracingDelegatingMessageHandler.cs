using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Motor.Extensions.Hosting.Abstractions;
using OpenTracing;
using OpenTracing.Tag;

namespace Motor.Extensions.Diagnostics.Tracing
{
    public class TracingDelegatingMessageHandler<TInput> : DelegatingMessageHandler<TInput>
        where TInput : class
    {
        private readonly ILogger<TracingDelegatingMessageHandler<TInput>> _logger;
        private readonly ITracer _tracer;

        public TracingDelegatingMessageHandler(ILogger<TracingDelegatingMessageHandler<TInput>> logger, ITracer tracer)
        {
            _logger = logger;
            _tracer = tracer;
        }

        public override async Task<ProcessedMessageStatus> HandleMessageAsync(MotorCloudEvent<TInput> dataCloudEvent,
            CancellationToken token = default)
        {
            var spanBuilder = _tracer.BuildSpan(nameof(HandleMessageAsync));
            var extension = dataCloudEvent.GetExtensionOrCreate(() => new JaegerTracingExtension((ISpanContext?) null));
            if (extension.SpanContext != null)
                spanBuilder = spanBuilder.AddReference(References.FollowsFrom, extension.SpanContext);

            using var scope = spanBuilder.StartActive(true);
            using (_logger.BeginScope("TraceId: {traceid}, SpanId: {spanid}",
                scope.Span.Context.TraceId, scope.Span.Context.SpanId, scope.Span.Context.TraceId))
            {
                var processedMessageStatus = ProcessedMessageStatus.CriticalFailure;
                try
                {
                    extension.Span = scope.Span;
                    processedMessageStatus = await base.HandleMessageAsync(dataCloudEvent, token);
                }
                finally
                {
                    scope.Span
                        .SetTag(nameof(ProcessedMessageStatus), processedMessageStatus.ToString())
                        .SetTag(Tags.Error.Key, processedMessageStatus != ProcessedMessageStatus.Success);
                }

                return processedMessageStatus;
            }
        }
    }
}
