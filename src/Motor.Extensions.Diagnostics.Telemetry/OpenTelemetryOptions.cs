using System.Collections.Generic;
using System.Diagnostics;

namespace Motor.Extensions.Diagnostics.Telemetry;

public record OpenTelemetryOptions
{
    public static readonly string DefaultActivitySourceName = "MotorNet.OpenTelemetry";
    internal static readonly ActivitySource ActivitySource;

    static OpenTelemetryOptions()
    {
        ActivitySource = new ActivitySource(DefaultActivitySourceName);
    }

    public double SamplingProbability { get; init; } = 0.0001;
    public List<string> Sources { get; init; } = new();

    /// <summary>
    ///
    /// If Filter returns false, health and metrics requests are collected.
    /// If Filter returns true, the request is filtered out.
    /// </summary>
    public bool FilterOutTelemetryRequests { get; init; } = true;
}
