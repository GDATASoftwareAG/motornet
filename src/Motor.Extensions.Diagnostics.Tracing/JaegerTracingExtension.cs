using CloudNative.CloudEvents.Extensions;
using OpenTracing;

namespace Motor.Extensions.Diagnostics.Tracing
{
    public class JaegerTracingExtension : DistributedTracingExtension
    {
        private ISpan? _span;
        private ISpanContext? _spanContext;

        public JaegerTracingExtension(ISpanContext? spanContext)
        {
            SpanContext = spanContext;
        }

        public JaegerTracingExtension(ISpan? span)
        {
            Span = span;
        }

        public ISpanContext? SpanContext
        {
            get => _spanContext;
            set
            {
                TraceParent = value?.TraceId;
                TraceState = value?.SpanId;
                _spanContext = value;
            }
        }

        public ISpan? Span
        {
            get => _span;
            set
            {
                SpanContext = value?.Context;
                _span = value;
            }
        }
    }
}
