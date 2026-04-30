namespace Motor.Extensions.Hosting.PgMq.Options;

// ReSharper disable once UnusedTypeParameter
public record PgMqPublisherOptions<T> : PgOptions
{
    public string QueueName { get; init; } = string.Empty;
}
