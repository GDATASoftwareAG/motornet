namespace Motor.Extensions.Hosting.PgMq.Options;

// ReSharper disable once UnusedTypeParameter
public record PgMqConsumerOptions : PgOptions
{
    /// <summary>
    /// The visibility timeout is the amount of time a message is invisible to other consumers
    /// after it has been read by a consumer. If the message is NOT deleted or archived within
    /// the visibility timeout, it will become visible again and can be read by another consumer.
    /// </summary>
    public int VisibilityTimeoutInSeconds { get; init; } = 300;

    /// <summary>
    /// Maximum duration of the long-polling operation.
    /// 5 is the default, same as in the upstream library.
    /// </summary>
    public int PollTimeoutSeconds { get; init; } = 5;

    /// <summary>
    /// Delay between internal postgres checks of the queue.
    /// 250 is the default, same as in the upstream library.
    /// </summary>
    public int PollIntervalMilliseconds { get; init; } = 250;
}
