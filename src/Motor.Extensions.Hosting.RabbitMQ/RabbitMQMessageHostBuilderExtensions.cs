using CloudNative.CloudEvents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.RabbitMQ.Options;

namespace Motor.Extensions.Hosting.RabbitMQ
{
    public static class RabbitMQMessageHostBuilderExtensions
    {
        public static void AddRabbitMQWithConfig<T>(this IConsumerBuilder<T> builder, IConfiguration config) where T : notnull
        {
            builder.AddTransient<ICloudEventFormatter, JsonEventFormatter>();
            builder.AddTransient<IRabbitMQConnectionFactory, RabbitMQConnectionFactory>();
            builder.Configure<RabbitMQConsumerOptions<T>>(config);
            builder.AddConsumer<RabbitMQMessageConsumer<T>>();
        }

        public static void AddRabbitMQ<T>(this IConsumerBuilder<T> builder, string configSection = "RabbitMQConsumer") where T : notnull
        {
            builder.AddRabbitMQWithConfig(builder.Context.Configuration.GetSection(configSection));
        }

        public static void AddRabbitMQWithConfig<T>(this IPublisherBuilder<T> builder, IConfiguration config) where T : notnull
        {
            builder.AddTransient<ICloudEventFormatter, JsonEventFormatter>();
            builder.AddTransient<IRabbitMQConnectionFactory, RabbitMQConnectionFactory>();
            builder.AddPublisher<RabbitMQMessagePublisher<T>>();
            builder.Configure<RabbitMQPublisherOptions<T>>(config);
        }

        public static void AddRabbitMQ<T>(this IPublisherBuilder<T> builder, string configSection = "RabbitMQPublisher") where T : notnull
        {
            builder.AddRabbitMQWithConfig(builder.Context.Configuration.GetSection(configSection));
        }
    }
}
