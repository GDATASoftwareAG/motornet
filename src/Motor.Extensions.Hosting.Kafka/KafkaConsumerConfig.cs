using Confluent.Kafka;

namespace Motor.Extensions.Hosting.Kafka
{
    public class KafkaConsumerConfig<T> : ConsumerConfig
    {
        public KafkaConsumerConfig()
        {
            EnableAutoCommit = false;
        }

        public string? Topic { get; set; }
        public int CommitPeriod { get; set; } = 1000;
    }
}
