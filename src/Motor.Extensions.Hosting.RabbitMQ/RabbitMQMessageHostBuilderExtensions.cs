using CloudNative.CloudEvents;
using Motor.Extensions.Hosting.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Motor.Extensions.Hosting.RabbitMQ.Config;

namespace Motor.Extensions.Hosting.RabbitMQ
{
    public static class RabbitMQMessageHostBuilderExtensions
    {
        public static void AddRabbitMQWithConfig<T>(this IConsumerBuilder<T> builder, IConfiguration config)
        {
            builder.AddTransient<ICloudEventFormatter, JsonEventFormatter>();
            builder.AddTransient<IRabbitMQConnectionFactory, RabbitMQConnectionFactory>();
            builder.Configure<RabbitMQConsumerConfig<T>>(config);
            builder.AddConsumer<RabbitMQMessageConsumer<T>>();
        }
        
        public static void AddRabbitMQ<T>(this IConsumerBuilder<T> builder, string configSection = "RabbitMQConsumer")
        {
            builder.AddRabbitMQWithConfig(builder.Context.Configuration.GetSection(configSection));
        }

        public static void AddRabbitMQWithConfig<T>(this IPublisherBuilder<T> builder, IConfiguration config)
        {
            builder.AddTransient<ICloudEventFormatter, JsonEventFormatter>();
            builder.AddTransient<IRabbitMQConnectionFactory, RabbitMQConnectionFactory>();
            builder.AddPublisher<RabbitMQMessagePublisher<T>>();
            builder.Configure<RabbitMQPublisherConfig<T>>(config);
        }

        public static void AddRabbitMQ<T>(this IPublisherBuilder<T> builder, string configSection = "RabbitMQPublisher")
        {
            builder.AddRabbitMQWithConfig(builder.Context.Configuration.GetSection(configSection));
        }
    }
}
