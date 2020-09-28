using System;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Utilities.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Motor.Extensions.Hosting.Consumer
{
    public static class TypedConsumerServiceExtensions
    {
        public static IMotorHostBuilder ConfigureConsumer<TInput>(this IMotorHostBuilder hostBuilder,
            Action<HostBuilderContext, IConsumerBuilder<TInput>> action)
            where TInput : class
        {
            hostBuilder.ConfigureServices((context, collection) =>
            {
                var consumerBuilder = new ConsumerBuilder<TInput>(collection, context);
                collection.AddHostedService<TypedConsumerService<TInput>>();
                action.Invoke(context, consumerBuilder);
            });
            return hostBuilder;
        }
    }
}
