using Motor.Extensions.Hosting.Abstractions;

namespace Motor.Extensions.Conversion.JsonNet;

public static class JsonNetHostBuilderExtensions
{
    extension<TOut>(IPublisherBuilder<TOut> builder)
        where TOut : notnull
    {
        public IPublisherBuilder<TOut> AddJsonNet()
        {
            builder.AddSerializer<JsonNetSerializer<TOut>>();
            return builder;
        }
    }

    extension<TIn>(IConsumerBuilder<TIn> builder)
        where TIn : notnull
    {
        public IConsumerBuilder<TIn> AddJsonNet()
        {
            builder.AddDeserializer<JsonNetDeserializer<TIn>>();
            return builder;
        }
    }
}
