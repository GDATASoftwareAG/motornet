using Confluent.Kafka;

namespace Motor.Extensions.Hosting.Kafka.Options;

public class KafkaPublisherOptions<T> : ProducerConfig
{
    public KafkaPublisherOptions()
    {
        // Allow librdkafka to batch messages for better throughput.
        // LingerMs controls how long to wait for additional messages before sending a batch.
        LingerMs ??= 5;
        // Enable Snappy compression for better network throughput with low CPU overhead.
        CompressionType ??= Confluent.Kafka.CompressionType.Snappy;
    }

    public string? Topic { get; set; }
}
