using System.Collections.Generic;
using System.Diagnostics;

namespace Motor.Extensions.Diagnostics.Tracing
{
    public record OpenTelemetryOptions
    {
        public static readonly string DefaultActivitySourceName = "MotorNet.OpenTelemetry";
        internal static readonly ActivitySource ActivitySource;

        static OpenTelemetryOptions()
        {
            ActivitySource = new ActivitySource(DefaultActivitySourceName);
        }

        public double SamplingProbability { get; set; } = 0.0001;
        public List<string> Sources { get; set; } = new() { DefaultActivitySourceName };
    }
}
