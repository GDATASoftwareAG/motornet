using Confluent.Kafka;

namespace Motor.Extensions.Hosting.Kafka
{
    public class KafkaPublisherConfig<T> : ProducerConfig
    {
        public string? Topic { get; set; }
    }
}
