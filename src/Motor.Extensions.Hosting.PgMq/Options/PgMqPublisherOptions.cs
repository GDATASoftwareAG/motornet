namespace Motor.Extensions.Hosting.PgMq.Options;

// ReSharper disable once UnusedTypeParameter
public class PgMqPublisherOptions<T>
{
    public string ConnectionString { get; set; } = string.Empty;
    public string QueueName { get; set; } = string.Empty;
}
