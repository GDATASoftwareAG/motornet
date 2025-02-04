using System.Collections.Generic;
using Motor.Extensions.Hosting.RabbitMQ.Validation;

namespace Motor.Extensions.Hosting.RabbitMQ.Options;

public record RabbitMQQueueOptions : RabbitMQQueueLimitOptions
{
    [NotWhitespaceOrEmpty]
    public string Name { get; set; } = string.Empty;

    [RequireValid]
    public RabbitMQBindingOptions[] Bindings { get; set; } = new RabbitMQBindingOptions[0];

    public bool Durable { get; set; } = true;
    public bool AutoDelete { get; set; }
    public QueueMode Mode { get; set; } = QueueMode.Default;
    public IDictionary<string, object?> Arguments { get; set; } = new Dictionary<string, object?>();
    public RabbitMQDeadLetterExchangeOptions? DeadLetterExchange { get; set; }
}
