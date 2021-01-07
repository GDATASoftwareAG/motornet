namespace Motor.Extensions.Diagnostics.Tracing
{
    public record OpenTelemetryOptions
    {
        public double SamplingProbability { get; set; } = 0.0001;
    }
}
