using Motor.Extensions.Hosting.Abstractions;

namespace Motor.Extensions.Conversion.SystemJson
{
    public static class SystemJsonHostBuilderExtensions
    {
        public static IPublisherBuilder<TOut> AddSystemJson<TOut>(this IPublisherBuilder<TOut> publisherBuilder) where TOut : notnull
        {
            publisherBuilder.AddSerializer<SystemJsonSerializer<TOut>>();
            return publisherBuilder;
        }

        public static IConsumerBuilder<TIn> AddSystemJson<TIn>(this IConsumerBuilder<TIn> consumerBuilder) where TIn : notnull
        {
            consumerBuilder.AddDeserializer<SystemJsonDeserializer<TIn>>();
            return consumerBuilder;
        }
    }
}
