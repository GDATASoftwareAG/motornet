namespace Motor.Extensions.Hosting.PgMq.Options;

// ReSharper disable once UnusedTypeParameter
public record PgMqConsumerOptions<T> : PgOptions
{
    public string QueueName { get; init; } = string.Empty;
    
    public int VisibilityTimeoutInSeconds { get; init; } = 300;

    public int PollTimeoutSeconds { get; init; } = 5;

    public int PollIntervalMilliseconds { get; init; } = 5000;
}
