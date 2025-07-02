using Confluent.Kafka;

namespace Motor.Extensions.Hosting.Kafka.Options;

public class KafkaConsumerOptions<T> : ConsumerConfig
{
    public KafkaConsumerOptions()
    {
        EnableAutoCommit = false;
        EnableAutoOffsetStore = false;
    }

    public string? Topic { get; set; }
    public int CommitPeriod { get; set; } = 1000;
    public int MaxConcurrentMessages { get; set; } = 1000;
    public int RetriesOnTemporaryFailure { get; set; } = 10;
}
