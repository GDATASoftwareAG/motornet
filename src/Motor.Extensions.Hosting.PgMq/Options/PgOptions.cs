using System.ComponentModel.DataAnnotations;

namespace Motor.Extensions.Hosting.PgMq.Options;

/// <summary>
/// PostgreSQL connection options shared by PgMq producer and consumer.
/// </summary>
public abstract record PgOptions
{
    /// <summary>PostgreSQL server host name or IP address.</summary>
    public required string Host { get; init; }

    /// <summary>PostgreSQL server port. Defaults to 5432.</summary>
    public int Port { get; init; } = 5432;

    /// <summary>PostgreSQL database name.</summary>
    public required string Database { get; init; }

    /// <summary>PostgreSQL user name.</summary>
    public required string Username { get; init; }

    /// <summary>PostgreSQL password.</summary>
    public required string Password { get; init; }

    /// <summary>The pgmq queue name.</summary>
    public required string QueueName { get; init; }

    /// <summary>
    /// Builds a connection string from the structured properties.
    /// </summary>
    public string ToConnectionString() =>
        $"Host={Host};Port={Port};Database={Database};Username={Username};Password={Password}";
}
