using CloudNative.CloudEvents;
using CloudNative.CloudEvents.SystemTextJson;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.Kafka.Options;

namespace Motor.Extensions.Hosting.Kafka;

public static class KafkaHostBuilderExtensions
{
    extension<T>(IConsumerBuilder<T> builder)
        where T : notnull
    {
        public IConsumerBuilder<T> AddKafkaWithConfig(IConfiguration config)
        {
            builder.AddTransient<CloudEventFormatter, JsonEventFormatter>();
            builder.Configure<KafkaConsumerOptions<T>>(config);
            builder.AddConsumer<KafkaMessageConsumer<T>>();

            return builder;
        }

        public IConsumerBuilder<T> AddKafka(string configSection = "KafkaConsumer") =>
            builder.AddKafkaWithConfig(builder.Configuration.GetSection(configSection));
    }

    extension<T>(IPublisherBuilder<T> builder)
        where T : notnull
    {
        public IPublisherBuilder<T> AddKafkaWithConfig(IConfiguration config)
        {
            builder.AddTransient<CloudEventFormatter, JsonEventFormatter>();
            builder.AddPublisher<KafkaMessagePublisher<T>>();
            builder.Configure<KafkaPublisherOptions<T>>(config);

            return builder;
        }

        public IPublisherBuilder<T> AddKafka(string configSection = "KafkaPublisher") =>
            builder.AddKafkaWithConfig(builder.Configuration.GetSection(configSection));
    }
}
