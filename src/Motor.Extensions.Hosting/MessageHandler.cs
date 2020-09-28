using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Diagnostics.Metrics;
using Motor.Extensions.Diagnostics.Metrics.Abstractions;
using Microsoft.Extensions.Logging;

namespace Motor.Extensions.Hosting
{
    public class MessageHandler<TInput, TOutput> : MultiResultMessageHandler<TInput, TOutput>
        where TInput : class 
        where TOutput : class
    {
        public MessageHandler(ILogger<MessageHandler<TInput, TOutput>> logger,
            IMetricsFactory<MessageHandler<TInput, TOutput>> metrics, IMessageConverter<TInput, TOutput> converter,
            ITypedMessagePublisher<TOutput> publisher) :
            base(logger, metrics, new SingleToMultiConverterAdapter<TInput, TOutput>(converter), publisher)
        {
        }
    }
}
