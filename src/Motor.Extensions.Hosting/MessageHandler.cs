using Microsoft.Extensions.Logging;
using Motor.Extensions.Diagnostics.Metrics.Abstractions;
using Motor.Extensions.Hosting.Abstractions;

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
