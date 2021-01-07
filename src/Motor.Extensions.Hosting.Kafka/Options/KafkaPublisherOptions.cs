using Confluent.Kafka;

namespace Motor.Extensions.Hosting.Kafka
{
    public class KafkaPublisherOptions<T> : ProducerConfig
    {
        public string? Topic { get; set; }
    }
}
