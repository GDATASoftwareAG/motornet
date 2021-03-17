namespace Motor.Extensions.Hosting.RabbitMQ.Options
{
    public record RabbitMQPublisherOptions<T> : RabbitMQBaseOptions
    {
        public RabbitMQBindingOptions PublishingTarget { get; set; } = new();
        public byte? DefaultPriority { get; set; } = null;
        public bool OverwriteExchange { get; set; }
    }
}
