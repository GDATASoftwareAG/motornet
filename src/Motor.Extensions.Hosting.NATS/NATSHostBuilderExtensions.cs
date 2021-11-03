using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.NATS.Options;

namespace Motor.Extensions.Hosting.NATS
{
    public static class NATSHostBuilderExtensions
    {
        public static void AddNATSWithConfig<T>(this IConsumerBuilder<T> builder,
            IConfiguration clientConfiguration)
            where T : notnull
        {
            builder.Configure<NATSConsumerOptions>(clientConfiguration);
            builder.AddConsumer<NATSConsumer<T>>();
            builder.AddTransient<INATSClientFactory, NATSClientFactory>();
        }

        public static void AddNATS<T>(this IConsumerBuilder<T> builder, string clientConfigSection = "NATSConsumer")
            where T : notnull
        {
            builder.AddNATSWithConfig(builder.Context.Configuration.GetSection(clientConfigSection));
        }

        public static void AddNATSWithConfig<T>(this IPublisherBuilder<T> builder,
            IConfiguration clientConfiguration)
            where T : notnull
        {
            builder.Configure<NATSBaseOptions>(clientConfiguration);
            builder.AddPublisher<NATSMessagePublisher>();
            builder.AddTransient<INATSClientFactory, NATSClientFactory>();
        }

        public static void AddNATS<T>(this IPublisherBuilder<T> builder, string clientConfigSection = "NATSPublisher")
            where T : notnull
        {
            builder.AddNATSWithConfig(builder.Context.Configuration.GetSection(clientConfigSection));
        }
    }
}
