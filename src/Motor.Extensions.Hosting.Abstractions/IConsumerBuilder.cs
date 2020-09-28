using Motor.Extensions.Conversion.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Motor.Extensions.Hosting.Abstractions
{
    public interface IConsumerBuilder<T> : IServiceCollection
    {
        HostBuilderContext Context { get; }
        public void AddConsumer<TConsumer>()
            where TConsumer : IMessageConsumer<T>;

        public void AddDeserializer<TDeserializer>()
            where TDeserializer : IMessageDeserializer<T>;
    }
}
