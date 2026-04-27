using CloudNative.CloudEvents;
using CloudNative.CloudEvents.SystemTextJson;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.Kafka.Options;

namespace Motor.Extensions.Hosting.Kafka;

public static class KafkaHostBuilderExtensions
{
    public static void AddKafkaWithConfig<T>(this IConsumerBuilder<T> builder, IConfiguration config)
        where T : notnull
    {
        builder.AddTransient<CloudEventFormatter, JsonEventFormatter>();
        builder.Configure<KafkaConsumerOptions<T>>(config);
        builder.AddConsumer<KafkaMessageConsumer<T>>();
    }

    public static void AddKafka<T>(this IConsumerBuilder<T> builder, string configSection = "KafkaConsumer")
        where T : notnull
    {
        builder.AddKafkaWithConfig(builder.Context.Configuration.GetSection(configSection));
    }

    /// <summary>
    /// Registers a Kafka publisher as the dead letter queue for consumed messages of type <typeparamref name="T"/>.
    /// Failed messages (<see cref="Motor.Extensions.Hosting.Abstractions.ProcessedMessageStatus.Failure"/>,
    /// <see cref="Motor.Extensions.Hosting.Abstractions.ProcessedMessageStatus.InvalidInput"/>, and
    /// <see cref="Motor.Extensions.Hosting.Abstractions.ProcessedMessageStatus.TemporaryFailure"/> after retries)
    /// will be forwarded to the configured Kafka topic instead of being dropped or stopping the application.
    /// </summary>
    public static void AddKafkaDeadLetterQueueWithConfig<T>(this IConsumerBuilder<T> builder, IConfiguration config)
        where T : notnull
    {
        builder.Configure<KafkaPublisherOptions<T>>(config);
        builder.TryAddSingleton<IOptions<PublisherOptions>>(_ => Microsoft.Extensions.Options.Options.Create(new PublisherOptions()));
        builder.AddSingleton<IRawMessagePublisher<T>, KafkaMessagePublisher<T>>();
    }

    /// <summary>
    /// Registers a Kafka publisher as the dead letter queue for consumed messages of type <typeparamref name="T"/>,
    /// reading publisher configuration from the <paramref name="configSection"/> key.
    /// </summary>
    public static void AddKafkaDeadLetterQueue<T>(
        this IConsumerBuilder<T> builder,
        string configSection = "KafkaDeadLetterQueue"
    )
        where T : notnull
    {
        builder.AddKafkaDeadLetterQueueWithConfig<T>(builder.Context.Configuration.GetSection(configSection));
    }

    public static void AddKafkaWithConfig<T>(this IPublisherBuilder<T> builder, IConfiguration config)
        where T : notnull
    {
        builder.AddTransient<CloudEventFormatter, JsonEventFormatter>();
        builder.AddPublisher<KafkaMessagePublisher<T>>();
        builder.Configure<KafkaPublisherOptions<T>>(config);
    }

    public static void AddKafka<T>(this IPublisherBuilder<T> builder, string configSection = "KafkaPublisher")
        where T : notnull
    {
        builder.AddKafkaWithConfig(builder.Context.Configuration.GetSection(configSection));
    }
}
