using CloudNative.CloudEvents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Motor.Extensions.Diagnostics.Queue.Abstractions;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.RabbitMQ.Options;

namespace Motor.Extensions.Hosting.RabbitMQ
{
    public static class RabbitMQMessageHostBuilderExtensions
    {
        public static void AddRabbitMQWithConfig<T>(this IConsumerBuilder<T> builder, IConfiguration config) where T : notnull
        {
            builder.AddTransient<ICloudEventFormatter, JsonEventFormatter>();
            builder.Configure<RabbitMQConsumerOptions<T>>(config);
            builder.AddSingleton(sp => RabbitMQConnectionFactory<T>.From(sp.GetRequiredService<IOptions<RabbitMQConsumerOptions<T>>>().Value));
            builder.AddConsumer<RabbitMQMessageConsumer<T>>();
            builder.AddSingleton<IQueueMonitor, RabbitMQQueueMonitor<T>>();
        }

        public static void AddRabbitMQ<T>(this IConsumerBuilder<T> builder, string configSection = "RabbitMQConsumer") where T : notnull
        {
            builder.AddRabbitMQWithConfig(builder.Context.Configuration.GetSection(configSection));
        }

        public static void AddRabbitMQWithConfig<T>(this IPublisherBuilder<T> builder, IConfiguration config) where T : notnull
        {
            builder.AddTransient<ICloudEventFormatter, JsonEventFormatter>();
            builder.Configure<RabbitMQPublisherOptions<T>>(config);
            builder.AddSingleton(sp => RabbitMQConnectionFactory<T>.From(sp.GetRequiredService<IOptions<RabbitMQConsumerOptions<T>>>().Value));
            builder.AddPublisher<RabbitMQMessagePublisher<T>>();
        }

        public static void AddRabbitMQ<T>(this IPublisherBuilder<T> builder, string configSection = "RabbitMQPublisher") where T : notnull
        {
            builder.AddRabbitMQWithConfig(builder.Context.Configuration.GetSection(configSection));
        }
    }
}
