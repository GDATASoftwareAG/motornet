using Motor.Extensions.Hosting.Abstractions;

namespace Motor.Extensions.ContentEncoding.Gzip;

public static class GzipHostBuilderExtensions
{
    public static IPublisherBuilder<TOut> AddGzipCompression<TOut>(this IPublisherBuilder<TOut> hostBuilder)
        where TOut : notnull
    {
        hostBuilder.AddEncoder<GzipMessageEncoder>();
        return hostBuilder;
    }

    public static IConsumerBuilder<TOut> AddGzipDecompression<TOut>(this IConsumerBuilder<TOut> hostBuilder)
        where TOut : notnull
    {
        hostBuilder.AddDecoder<GzipMessageDecoder>();
        return hostBuilder;
    }
}
