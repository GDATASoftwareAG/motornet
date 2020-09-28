using Motor.Extensions.Hosting.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Motor.Extensions.Hosting.Kafka
{
    public static class KafkaHostBuilderExtensions
    {
        public static void AddKafkaWithConfig<T>(this IConsumerBuilder<T> builder, IConfiguration config)
        {
            builder.Configure<KafkaConsumerConfig<T>>(config);
            builder.AddConsumer<KafkaConsumer<T>>();
        }
        
        public static void AddKafka<T>(this IConsumerBuilder<T> builder, string configSection = "KafkaConsumer")
        {
            builder.AddKafkaWithConfig(builder.Context.Configuration.GetSection(configSection));
        }
    }
}
