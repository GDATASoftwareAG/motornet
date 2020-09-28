namespace Motor.Extensions.Hosting.RabbitMQ.Config

{
    public class RabbitMQConsumerConfig<T> : RabbitMQConfig
    {
        public RabbitMQQueueConfig Queue { get; set; } = new RabbitMQQueueConfig();
        public ushort PrefetchCount { get; set; } = 10;
    }
}
