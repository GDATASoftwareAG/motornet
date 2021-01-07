namespace Motor.Extensions.Hosting.RabbitMQ.Options
{
    public record RabbitMQConsumerOptions<T> : RabbitMQBaseOptions
    {
        public RabbitMQQueueOptions Queue { get; set; } = new();
        public ushort PrefetchCount { get; set; } = 10;
    }
}
