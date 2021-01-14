using Confluent.Kafka;

namespace Motor.Extensions.Hosting.Kafka.Options
{
    public class KafkaPublisherOptions<T> : ProducerConfig
    {
        public string? Topic { get; set; }
    }
}
