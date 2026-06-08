using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Motor.Extensions.Hosting.Abstractions;

namespace Motor.Extensions.Hosting.Consumer;

public static class TypedConsumerServiceExtensions
{
    extension(IHostApplicationBuilder hostBuilder)
    {
        public IConsumerBuilder<TInput> AddConsumer<TInput>()
            where TInput : class
        {
            var consumerBuilder = new ConsumerBuilder<TInput>(
                hostBuilder.Services,
                hostBuilder.Environment,
                hostBuilder.Configuration
            );
            hostBuilder.Services.AddHostedService<TypedConsumerService<TInput>>();

            return consumerBuilder;
        }
    }

    extension(IHostBuilder hostBuilder)
    {
        public IHostBuilder ConfigureConsumer<TInput>(Action<HostBuilderContext, IConsumerBuilder<TInput>> action)
            where TInput : class
        {
            hostBuilder.ConfigureServices(
                (context, collection) =>
                {
                    var consumerBuilder = new ConsumerBuilder<TInput>(collection, context);
                    collection.AddHostedService<TypedConsumerService<TInput>>();
                    action.Invoke(context, consumerBuilder);
                }
            );
            return hostBuilder;
        }
    }
}
