namespace Motor.Extensions.Hosting.RabbitMQ.Options;

public record RabbitMQDeadLetterExchangeOptions : RabbitMQQueueLimitOptions
{
    public string Name { get; set; } = string.Empty;
    public bool RepublishInvalidInputToDeadLetterExchange { get; set; }
    public RabbitMQBindingOptions Binding { get; set; } = new();
}
