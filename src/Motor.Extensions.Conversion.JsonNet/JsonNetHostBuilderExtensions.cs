using Motor.Extensions.Hosting.Abstractions;

namespace Motor.Extensions.Conversion.JsonNet
{
    public static class JsonNetHostBuilderExtensions
    {
        public static IPublisherBuilder<TOut> AddJsonNet<TOut>(this IPublisherBuilder<TOut> hostBuilder)
        {
            hostBuilder.AddSerializer<JsonNetSerializer<TOut>>();
            return hostBuilder;
        }

        public static IConsumerBuilder<TIn> AddJsonNet<TIn>(this IConsumerBuilder<TIn> consumerBuilder)
        {
            consumerBuilder.AddDeserializer<JsonNetDeserializer<TIn>>();
            return consumerBuilder;
        }
    }
}
