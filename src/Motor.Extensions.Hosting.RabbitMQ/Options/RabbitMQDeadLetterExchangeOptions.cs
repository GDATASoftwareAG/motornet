using Motor.Extensions.Hosting.RabbitMQ.Validation;

namespace Motor.Extensions.Hosting.RabbitMQ.Options;

public record RabbitMQDeadLetterExchangeOptions : RabbitMQQueueLimitOptions
{
    public string Name { get; set; } = string.Empty;
    public RabbitMQBindingOptions Binding { get; set; } = new();
}
