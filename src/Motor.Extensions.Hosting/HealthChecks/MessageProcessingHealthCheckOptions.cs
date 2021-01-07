using System;

namespace Motor.Extensions.Hosting.HealthChecks
{
    public record MessageProcessingHealthCheckOptions
    {
        public TimeSpan MaxTimeSinceLastProcessedMessage { get; set; } = TimeSpan.FromMinutes(5);
    }
}
