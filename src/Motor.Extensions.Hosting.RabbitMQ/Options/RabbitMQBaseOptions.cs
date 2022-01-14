using System;
using System.Net.Security;
using System.Security.Authentication;
using Motor.Extensions.Hosting.RabbitMQ.Validation;
using RabbitMQ.Client;

namespace Motor.Extensions.Hosting.RabbitMQ.Options;

public sealed record RabbitMQSslOptions
{
    public bool Enabled { get; init; }
    public SslProtocols Protocols { get; init; } = SslProtocols.Tls12;
    public SslPolicyErrors AcceptablePolicyErrors { get; init; } = SslPolicyErrors.None;
    public RemoteCertificateValidationCallback? CertificateValidationCallback { get; set; }
}

public abstract record RabbitMQBaseOptions
{
    [NotWhitespaceOrEmpty]
    public string Host { get; init; } = string.Empty;

    [NotWhitespaceOrEmpty]
    public string VirtualHost { get; init; } = string.Empty;

    [NotWhitespaceOrEmpty]
    public string User { get; init; } = string.Empty;

    [NotWhitespaceOrEmpty]
    public string Password { get; init; } = string.Empty;

    public int Port { get; init; } = AmqpTcpEndpoint.UseDefaultPort;
    public TimeSpan RequestedHeartbeat { get; set; } = TimeSpan.FromSeconds(60);
    public RabbitMQSslOptions Tls { get; init; } = new();
}
