using CloudNative.CloudEvents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Motor.Extensions.Hosting.Abstractions;

namespace Motor.Extensions.Hosting.Kafka
{
    public static class KafkaHostBuilderExtensions
    {
        public static void AddKafkaWithConfig<T>(this IConsumerBuilder<T> builder, IConfiguration config)
        {
            builder.AddTransient<ICloudEventFormatter, JsonEventFormatter>();
            builder.Configure<KafkaConsumerConfig<T>>(config);
            builder.AddConsumer<KafkaMessageConsumer<T>>();
        }

        public static void AddKafka<T>(this IConsumerBuilder<T> builder, string configSection = "KafkaConsumer")
        {
            builder.AddKafkaWithConfig(builder.Context.Configuration.GetSection(configSection));
        }

        public static void AddRabbitMQWithConfig<T>(this IPublisherBuilder<T> builder, IConfiguration config)
        {
            builder.AddTransient<ICloudEventFormatter, JsonEventFormatter>();
            builder.AddPublisher<KafkaMessagePublisher<T>>();
            builder.Configure<KafkaPublisherConfig<T>>(config);
        }

        public static void AddRabbitMQ<T>(this IPublisherBuilder<T> builder, string configSection = "KafkaPublisher")
        {
            builder.AddRabbitMQWithConfig(builder.Context.Configuration.GetSection(configSection));
        }
    }
}
