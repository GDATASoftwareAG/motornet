namespace Motor.Extensions.Hosting.RabbitMQ.Options;

public abstract record RabbitMQQueueLimitOptions
{
    public int? MaxLength { get; set; } = 1_000_000;
    public long? MaxLengthBytes { get; set; } = 200 * 1024 * 1024;
    public int? MaxPriority { get; set; } = 255;
    public int? MessageTtl { get; set; } = 86_400_000;
}
