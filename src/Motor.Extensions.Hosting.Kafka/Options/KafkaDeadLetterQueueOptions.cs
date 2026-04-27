namespace Motor.Extensions.Hosting.Kafka.Options;

public class KafkaDeadLetterQueueOptions
{
    /// <summary>
    /// When <see langword="true"/>, messages that result in <see cref="Motor.Extensions.Hosting.Abstractions.ProcessedMessageStatus.Failure"/>
    /// are published to the dead letter queue and the offset is committed. The consumer continues processing.
    /// </summary>
    public bool PublishFailure { get; set; } = true;

    /// <summary>
    /// When <see langword="true"/>, messages that result in <see cref="Motor.Extensions.Hosting.Abstractions.ProcessedMessageStatus.InvalidInput"/>
    /// are published to the dead letter queue and the offset is committed. The consumer continues processing.
    /// </summary>
    public bool PublishInvalidInput { get; set; } = true;

    /// <summary>
    /// When <see langword="true"/>, messages that result in <see cref="Motor.Extensions.Hosting.Abstractions.ProcessedMessageStatus.TemporaryFailure"/>
    /// after all retries are exhausted are published to the dead letter queue and the offset is committed.
    /// The consumer continues processing instead of stopping the application.
    /// </summary>
    public bool PublishTemporaryFailureAfterRetries { get; set; } = true;
}
