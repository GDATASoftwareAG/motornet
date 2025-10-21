using System;

namespace Motor.Extensions.Hosting.Abstractions;

public class UnhandledCloudEventFormatException : ArgumentException
{
    public UnhandledCloudEventFormatException(CloudEventFormat cloudEventFormat)
        : base($"Unhandled CloudEventFormat {cloudEventFormat}") { }
}
