namespace Motor.Extensions.Hosting.Abstractions;

public enum CloudEventFormat
{
    /// <summary>
    /// If supported by publisher or consumer, the protocol format uses headers from Kafka or RabbitMQ to store CloudEvent metadata.
    /// </summary>
    Protocol,
    /// <summary>
    /// If supported by publisher or consumer, CloudEvent published in the enveloped variant, see https://github.com/cloudevents/spec/blob/v1.0.1/json-format.md#3-envelope.
    /// </summary>
    Json,
}
public record PublisherOptions
{
    public CloudEventFormat CloudEventFormat { get; init; } = CloudEventFormat.Protocol;
}
