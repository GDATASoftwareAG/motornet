using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Motor.Extensions.Conversion.Abstractions;

namespace Motor.Extensions.Hosting.Abstractions
{
    public interface IPublisherBuilder<T> : IServiceCollection
    {
        HostBuilderContext Context { get; }

        public void AddPublisher<TPublisher>()
            where TPublisher : ITypedMessagePublisher<byte[]>;

        public void AddSerializer<TSerializer>()
            where TSerializer : IMessageSerializer<T>;
    }
}
