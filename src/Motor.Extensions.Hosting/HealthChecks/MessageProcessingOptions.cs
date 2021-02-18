using System;

namespace Motor.Extensions.Hosting.HealthChecks
{
    public record MessageProcessingOptions
    {
        public TimeSpan MaxTimeSinceLastProcessedMessage { get; set; } = TimeSpan.FromMinutes(5);
    }
}
