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
    public int MaxConcurrentMessages { get; set; } = 1000;
    public int RetriesOnTemporaryFailure { get; set; } = 10;
    public TimeSpan RetryBasePeriod { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// When set, failed messages are forwarded to a dead letter queue via the registered
    /// <see cref="Motor.Extensions.Hosting.Abstractions.IRawMessagePublisher{TOutput}"/> instead of being silently dropped or stopping the application.
    /// </summary>
    public KafkaDeadLetterQueueOptions? DeadLetterQueue { get; set; }
}
