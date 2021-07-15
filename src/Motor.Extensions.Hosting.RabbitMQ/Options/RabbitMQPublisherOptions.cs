using Motor.Extensions.Hosting.RabbitMQ.Validation;

namespace Motor.Extensions.Hosting.RabbitMQ.Options
{
    public record RabbitMQPublisherOptions<T> : RabbitMQBaseOptions
    {
        [RequireValid]
        public RabbitMQBindingOptions PublishingTarget { get; set; } = new();

        public byte? DefaultPriority { get; set; } = null;
        public bool OverwriteExchange { get; set; }
    }
}
