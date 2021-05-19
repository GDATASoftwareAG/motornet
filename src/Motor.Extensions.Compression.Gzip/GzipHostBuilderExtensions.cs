using Motor.Extensions.Hosting.Abstractions;

namespace Motor.Extensions.Compression.Gzip
{
    public static class GzipHostBuilderExtensions
    {
        public static IPublisherBuilder<TOut> AddGzipCompression<TOut>(this IPublisherBuilder<TOut> hostBuilder)
            where TOut : notnull
        {
            hostBuilder.AddCompressor<GzipMessageCompressor>();
            return hostBuilder;
        }

        public static IConsumerBuilder<TOut> AddGzipDecompression<TOut>(this IConsumerBuilder<TOut> hostBuilder)
            where TOut : notnull
        {
            hostBuilder.AddDecompressor<GzipMessageDecompressor>();
            return hostBuilder;
        }
    }
}
