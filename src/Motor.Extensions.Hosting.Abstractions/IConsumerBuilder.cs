using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Motor.Extensions.Compression.Abstractions;
using Motor.Extensions.Conversion.Abstractions;

namespace Motor.Extensions.Hosting.Abstractions
{
    public interface IConsumerBuilder<T> : IServiceCollection where T : notnull
    {
        HostBuilderContext Context { get; }

        public void AddConsumer<TConsumer>()
            where TConsumer : IMessageConsumer<T>;

        void AddDecompressor<TDecompressor>() where TDecompressor : IMessageDecompressor;

        public void AddDeserializer<TDeserializer>()
            where TDeserializer : IMessageDeserializer<T>;
    }
}
