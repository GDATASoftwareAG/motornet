using Google.Protobuf;
using Motor.Extensions.Hosting.Abstractions;

namespace Motor.Extensions.Conversion.Protobuf
{
    public static class ProtobufHostBuilderExtensions
    {
        public static IPublisherBuilder<TOut> AddProtobuf<TOut>(this IPublisherBuilder<TOut> publisherBuilder)
            where TOut : IMessage
        {
            publisherBuilder.AddSerializer<ProtobufSerializer<TOut>>();
            return publisherBuilder;
        }

        public static IConsumerBuilder<TIn> AddProtobuf<TIn>(this IConsumerBuilder<TIn> consumerBuilder)
            where TIn : IMessage<TIn>, new()
        {
            consumerBuilder.AddDeserializer<ProtobufDeserializer<TIn>>();
            return consumerBuilder;
        }
    }
}
