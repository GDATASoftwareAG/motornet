namespace Motor.Extensions.Hosting.Abstractions;

public enum CloudEventFormat
{
    Protocol,
    Json,
}
public record PublisherOptions
{
    public CloudEventFormat CloudEventFormat { get; init; } = CloudEventFormat.Protocol;
}
