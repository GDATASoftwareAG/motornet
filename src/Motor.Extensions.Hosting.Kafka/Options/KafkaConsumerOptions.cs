using Confluent.Kafka;

namespace Motor.Extensions.Hosting.Kafka.Options
{
    public class KafkaConsumerOptions<T> : ConsumerConfig
    {
        public KafkaConsumerOptions()
        {
            EnableAutoCommit = false;
        }

        public string? Topic { get; set; }
        public int CommitPeriod { get; set; } = 1000;
    }
}
