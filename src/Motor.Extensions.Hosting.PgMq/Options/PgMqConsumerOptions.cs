using Motor.Extensions.Hosting.Abstractions;

namespace Motor.Extensions.Hosting.PgMq.Options;

// ReSharper disable once UnusedTypeParameter
public class PgMqConsumerOptions<T>
{
    public string ConnectionString { get; set; } = string.Empty;
    public string QueueName { get; set; } = string.Empty;
    public CloudEventFormat CloudEventFormat { get; set; } = CloudEventFormat.Protocol;
    /// <summary>
    /// Visibility timeout in seconds for each message read. Defaults to 30.
    /// </summary>
    public int VisibilityTimeoutInSeconds { get; set; } = 30;
    /// <summary>
    /// Delay in milliseconds between polls when no messages are available. Defaults to 250.
    /// </summary>
    public int PollingIntervalInMilliseconds { get; set; } = 250;
}
