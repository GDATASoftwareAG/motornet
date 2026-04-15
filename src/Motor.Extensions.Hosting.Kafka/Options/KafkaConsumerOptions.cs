using System;
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
    public int MaxConcurrentMessagesPerPartition { get; set; } = 1;
    public int RetriesOnTemporaryFailure { get; set; } = 10;
    public TimeSpan RetryBasePeriod { get; set; } = TimeSpan.FromSeconds(1);
}
