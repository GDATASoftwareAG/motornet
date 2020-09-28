using CloudNative.CloudEvents.Extensions;
using OpenTracing;

namespace Motor.Extensions.Diagnostics.Tracing
{
    public class JaegerTracingExtension: DistributedTracingExtension
    {
        private ISpanContext? _spanContext;
        public ISpanContext? SpanContext
        {
            get { return _spanContext; }
            set
            {
                TraceParent = value?.TraceId;
                TraceState = value?.SpanId;
                _spanContext = value;
            }
        }
        
        private ISpan? _span;
        public ISpan? Span
        {
            get { return _span; }
            set
            {
                SpanContext = value?.Context;
                _span = value;
            }
        }

        public JaegerTracingExtension(ISpanContext? spanContext)
        {
            SpanContext = spanContext;
        }
        public JaegerTracingExtension(ISpan? span)
        {
            Span = span;
        }
    }
}
