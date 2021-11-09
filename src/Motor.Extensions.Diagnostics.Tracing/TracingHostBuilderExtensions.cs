using System;
using Motor.Extensions.Diagnostics.Telemetry;
using Motor.Extensions.Utilities.Abstractions;

namespace Motor.Extensions.Diagnostics.Tracing;

[Obsolete("Replaced by TelemetryHostBuilderExtensions")]
public static class TracingHostBuilderExtensions
{
    public static readonly string JaegerExporter = TelemetryHostBuilderExtensions.JaegerExporter;
    public static readonly string OpenTelemetry = TelemetryHostBuilderExtensions.OpenTelemetry;

    public static readonly string AttributeMotorNetEnvironment =
        TelemetryHostBuilderExtensions.AttributeMotorNetEnvironment;

    public static readonly string AttributeMotorNetLibraryVersion =
        TelemetryHostBuilderExtensions.AttributeMotorNetLibraryVersion;

    [Obsolete("Replaced by ConfigureOpenTelemetry")]
    public static IMotorHostBuilder ConfigureJaegerTracing(this IMotorHostBuilder hostBuilder)
    {
        return hostBuilder.ConfigureOpenTelemetry();
    }
}
