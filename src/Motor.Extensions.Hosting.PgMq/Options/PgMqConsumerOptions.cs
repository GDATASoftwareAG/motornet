using System;

namespace Motor.Extensions.Hosting.PgMq.Options;

public class PgMqConsumerOptions<T>
{
    public string ConnectionString { get; set; } = string.Empty;
    public string QueueName { get; set; } = string.Empty;
}
