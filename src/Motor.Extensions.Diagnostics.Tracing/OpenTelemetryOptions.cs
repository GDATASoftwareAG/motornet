using System.Collections.Generic;

namespace Motor.Extensions.Diagnostics.Tracing
{
    public record OpenTelemetryOptions
    {
        public static readonly string DefaultActivitySourceName = "MotorNet.OpenTelemetry";
        public double SamplingProbability { get; set; } = 0.0001;
        public List<string> Sources { get; set; } = new() {DefaultActivitySourceName};
    }
}
