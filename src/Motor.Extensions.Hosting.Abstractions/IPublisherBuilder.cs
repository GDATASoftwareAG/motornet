using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Motor.Extensions.ContentEncoding.Abstractions;
using Motor.Extensions.Conversion.Abstractions;

namespace Motor.Extensions.Hosting.Abstractions;

public interface IPublisherBuilder<TOutput> : IServiceCollection where TOutput : notnull
{
    HostBuilderContext Context { get; }

    void AddPublisher<TPublisher>()
        where TPublisher : IRawMessagePublisher<TOutput>;

    void AddPublisher<TPublisher>(Func<IServiceProvider, TPublisher> implementationFactory)
        where TPublisher : class, IRawMessagePublisher<TOutput>;

    void AddSerializer<TSerializer>()
        where TSerializer : IMessageSerializer<TOutput>;

    void AddEncoder<TEncoder>()
        where TEncoder : IMessageEncoder;
}
