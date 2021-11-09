using System;

namespace Motor.Extensions.Hosting.HealthChecks;

public record TooManyTemporaryFailuresOptions
{
    /// <summary>
    /// Gets or sets the maximum number of temporary failures since the last message was handled for the health check
    /// to succeed. (Default: 1000)
    /// </summary>
    public uint MaxTemporaryFailuresSinceLastHandledMessage { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the timespan since when the last message needs to be successfully handled for the health check
    /// to succeed. (Default: 30s)
    /// </summary>
    public TimeSpan MaxTimeSinceLastHandledMessage { get; set; } = TimeSpan.FromSeconds(30);
}
