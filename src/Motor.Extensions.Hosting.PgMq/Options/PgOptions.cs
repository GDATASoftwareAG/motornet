using System.ComponentModel.DataAnnotations;

namespace Motor.Extensions.Hosting.PgMq.Options;

/// <summary>
/// PostgreSQL connection options shared by PgMq producer and consumer.
/// </summary>
public abstract record PgOptions
{
    /// <summary>PostgreSQL server host name or IP address.</summary>
    [Required]
    public string Host { get; init; } = string.Empty;

    /// <summary>PostgreSQL server port. Defaults to 5432.</summary>
    public int Port { get; init; } = 5432;

    /// <summary>PostgreSQL database name.</summary>
    [Required]
    public string Database { get; init; } = string.Empty;

    /// <summary>PostgreSQL user name.</summary>
    [Required]
    public string Username { get; init; } = string.Empty;

    /// <summary>PostgreSQL password.</summary>
    [Required]
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// Builds a connection string from the structured properties.
    /// </summary>
    public string ToConnectionString() =>
        $"Host={Host};Port={Port};Database={Database};Username={Username};Password={Password}";
}
