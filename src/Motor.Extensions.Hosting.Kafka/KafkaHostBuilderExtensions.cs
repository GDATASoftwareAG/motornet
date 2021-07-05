using CloudNative.CloudEvents;
using CloudNative.CloudEvents.SystemTextJson;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.Kafka.Options;

namespace Motor.Extensions.Hosting.Kafka
{
    public static class KafkaHostBuilderExtensions
    {
        public static void AddKafkaWithConfig<T>(this IConsumerBuilder<T> builder, IConfiguration config) where T : notnull
        {
            builder.AddTransient<CloudEventFormatter, JsonEventFormatter>();
            builder.Configure<KafkaConsumerOptions<T>>(config);
            builder.AddConsumer<KafkaMessageConsumer<T>>();
        }

        public static void AddKafka<T>(this IConsumerBuilder<T> builder, string configSection = "KafkaConsumer") where T : notnull
        {
            builder.AddKafkaWithConfig(builder.Context.Configuration.GetSection(configSection));
        }

        public static void AddKafkaWithConfig<T>(this IPublisherBuilder<T> builder, IConfiguration config) where T : notnull
        {
            builder.AddTransient<CloudEventFormatter, JsonEventFormatter>();
            builder.AddPublisher<KafkaMessagePublisher<T>>();
            builder.Configure<KafkaPublisherOptions<T>>(config);
        }

        public static void AddKafka<T>(this IPublisherBuilder<T> builder, string configSection = "KafkaPublisher") where T : notnull
        {
            builder.AddKafkaWithConfig(builder.Context.Configuration.GetSection(configSection));
        }
    }
}
