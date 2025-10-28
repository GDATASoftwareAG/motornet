using System;
using Microsoft.Extensions.Hosting;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Utilities.Abstractions;

namespace Motor.Extensions.Hosting.Publisher;

public static class TypedMessagePublisherExtensions
{
    public static IMotorHostBuilder ConfigurePublisher<TOutput>(
        this IMotorHostBuilder hostBuilder,
        Action<HostBuilderContext, IPublisherBuilder<TOutput>> action
    )
        where TOutput : class
    {
        hostBuilder.ConfigureServices(
            (context, collection) =>
            {
                var consumerBuilder = new PublisherBuilder<TOutput>(collection, context);
                consumerBuilder.Build(action);
            }
        );
        return hostBuilder;
    }
}
