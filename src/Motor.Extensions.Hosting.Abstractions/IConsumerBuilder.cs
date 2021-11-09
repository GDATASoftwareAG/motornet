using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Motor.Extensions.ContentEncoding.Abstractions;
using Motor.Extensions.Conversion.Abstractions;

namespace Motor.Extensions.Hosting.Abstractions;

public interface IConsumerBuilder<T> : IServiceCollection where T : notnull
{
    HostBuilderContext Context { get; }

    public void AddConsumer<TConsumer>()
        where TConsumer : IMessageConsumer<T>;

    void AddDecoder<TDecoder>() where TDecoder : IMessageDecoder;

    void AddConsumer<TConsumer>(Func<IServiceProvider, TConsumer> implementationFactory)
        where TConsumer : IMessageConsumer<T>;

    public void AddDeserializer<TDeserializer>()
        where TDeserializer : IMessageDeserializer<T>;
}
