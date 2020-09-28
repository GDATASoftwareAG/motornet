namespace Motor.Extensions.Hosting.RabbitMQ.Config
{
    public class RabbitMQPublisherConfig<T> : RabbitMQConfig
    {
        public RabbitMQBindingConfig PublishingTarget { get; set; } = new RabbitMQBindingConfig();
        public byte? DefaultPriority { get; set; } = null;
    }
}
