using Motor.Extensions.Hosting.Abstractions;

namespace Motor.Extensions.Conversion.JsonNet
{
    public static class JsonNetHostBuilderExtensions
    {
        public static IPublisherBuilder<TOut> AddJsonNet<TOut>(this IPublisherBuilder<TOut> hostBuilder) where TOut : notnull
        {
            hostBuilder.AddSerializer<JsonNetSerializer<TOut>>();
            return hostBuilder;
        }

        public static IConsumerBuilder<TIn> AddJsonNet<TIn>(this IConsumerBuilder<TIn> consumerBuilder) where TIn : notnull
        {
            consumerBuilder.AddDeserializer<JsonNetDeserializer<TIn>>();
            return consumerBuilder;
        }
    }
}
