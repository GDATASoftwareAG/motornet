using System;
using Motor.Extensions.Hosting.RabbitMQ.Validation;

namespace Motor.Extensions.Hosting.RabbitMQ.Options;

public abstract record RabbitMQBaseOptions
{
    [NotWhitespaceOrEmpty]
    public string Host { get; set; } = string.Empty;

    [NotWhitespaceOrEmpty]
    public string VirtualHost { get; set; } = string.Empty;

    [NotWhitespaceOrEmpty]
    public string User { get; set; } = string.Empty;

    [NotWhitespaceOrEmpty]
    public string Password { get; set; } = string.Empty;

    public int Port { get; set; } = 5672;
    public TimeSpan RequestedHeartbeat { get; set; } = TimeSpan.FromSeconds(60);
}
