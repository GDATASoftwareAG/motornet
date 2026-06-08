using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Motor.Extensions.Hosting.Abstractions;

namespace Motor.Extensions.Hosting.Publisher;

public static class TypedMessagePublisherExtensions
{
    extension<TOutput>(IHostApplicationBuilder hostBuilder)
        where TOutput : class
    {
        public IPublisherBuilder<TOutput> AddPublisher()
        {
            hostBuilder.Services.AddHostedService<TypedPublisherService<TOutput>>();
            return new PublisherBuilder<TOutput>(
                hostBuilder.Services,
                hostBuilder.Environment,
                hostBuilder.Configuration
            );
        }
    }

    extension<TOutput>(IHostBuilder builder)
        where TOutput : class
    {
        public IHostBuilder ConfigurePublisher(Action<HostBuilderContext, IPublisherBuilder<TOutput>> action)
        {
            builder.ConfigureServices(
                (context, services) =>
                {
                    services.AddHostedService<TypedPublisherService<TOutput>>();
                    var consumerBuilder = new PublisherBuilder<TOutput>(services, context);
                    consumerBuilder.Build(action);
                }
            );
            return builder;
        }
    }
}
