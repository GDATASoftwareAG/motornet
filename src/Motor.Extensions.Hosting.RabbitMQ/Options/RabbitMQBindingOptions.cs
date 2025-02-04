using System.Collections.Generic;
using Motor.Extensions.Hosting.RabbitMQ.Validation;

namespace Motor.Extensions.Hosting.RabbitMQ.Options;

public record RabbitMQBindingOptions
{
    [NotWhitespaceOrEmpty]
    public string RoutingKey { get; set; } = string.Empty;

    [NotWhitespaceOrEmpty]
    public string Exchange { get; set; } = string.Empty;

    public IDictionary<string, object?> Arguments { get; set; } = new Dictionary<string, object?>();
}
