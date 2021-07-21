using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Motor.Extensions.ContentEncoding.Abstractions;
using Motor.Extensions.Conversion.Abstractions;

namespace Motor.Extensions.Hosting.Abstractions
{
    public interface IPublisherBuilder<T> : IServiceCollection where T : notnull
    {
        HostBuilderContext Context { get; }

        void AddPublisher<TPublisher>()
            where TPublisher : ITypedMessagePublisher<byte[]>;

        void AddPublisher<TPublisher>(Func<IServiceProvider, TPublisher> implementationFactory)
            where TPublisher : class, ITypedMessagePublisher<byte[]>;

        void AddSerializer<TSerializer>()
            where TSerializer : IMessageSerializer<T>;

        void AddEncoder<TEncoder>()
            where TEncoder : IMessageEncoder;
    }
}
