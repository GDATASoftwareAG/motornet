using System.Diagnostics;
using Motor.Extensions.Diagnostics.Telemetry;

namespace Motor.Extensions.Hosting.Publisher;

public static class TypedMessagePublisherUtils
{
    internal static readonly ActivitySource ActivitySource;

    static TypedMessagePublisherUtils()
    {
        ActivitySource = new ActivitySource(OpenTelemetryOptions.DefaultActivitySourceName);
    }
}
