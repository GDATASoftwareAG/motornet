namespace Motor.Extensions.Hosting.Kafka.Options;

public class KafkaDeadLetterQueueOptions
{
    /// <summary>
    /// When <see langword="true"/>, application shuts down when there is a failure during publishing to the dead letter queue. This is the default behavior to prevent message loss.
    /// </summary>
    public bool ShutdownAppOnPublishFailure  { get; set; } = true;
}
