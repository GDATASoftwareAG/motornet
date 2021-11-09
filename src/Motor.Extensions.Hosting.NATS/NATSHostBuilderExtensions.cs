using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.NATS.Options;

namespace Motor.Extensions.Hosting.NATS;

public static class NATSHostBuilderExtensions
{
    public static void AddNATSWithConfig<TInput>(this IConsumerBuilder<TInput> builder,
        IConfiguration clientConfiguration)
        where TInput : notnull
    {
        builder.Configure<NATSConsumerOptions>(clientConfiguration);
        builder.AddConsumer<NATSMessageConsumer<TInput>>();
        builder.AddTransient<INATSClientFactory, NATSClientFactory>();
    }

    public static void AddNATS<TInput>(this IConsumerBuilder<TInput> builder, string clientConfigSection = "NATSConsumer")
        where TInput : notnull
    {
        builder.AddNATSWithConfig(builder.Context.Configuration.GetSection(clientConfigSection));
    }

    public static void AddNATSWithConfig<TOutput>(this IPublisherBuilder<TOutput> builder,
        IConfiguration clientConfiguration)
        where TOutput : notnull
    {
        builder.Configure<NATSBaseOptions>(clientConfiguration);
        builder.AddPublisher<NATSMessagePublisher<TOutput>>();
        builder.AddTransient<INATSClientFactory, NATSClientFactory>();
    }

    public static void AddNATS<TOutput>(this IPublisherBuilder<TOutput> builder, string clientConfigSection = "NATSPublisher")
        where TOutput : notnull
    {
        builder.AddNATSWithConfig(builder.Context.Configuration.GetSection(clientConfigSection));
    }
}
