using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.Kafka;
using Confluent.Kafka;
using Motor.Extensions.Hosting.CloudEvents;

namespace Motor.Extensions.Hosting.Kafka;

internal static class KafkaClientExtensions
{
    public static MotorCloudEvent<byte[]> ToMotorCloudEvent(
        this Message<string?, byte[]> message,
        IApplicationNameService applicationNameService,
        CloudEventFormatter cloudEventFormatter
    )
    {
        if (!message.IsCloudEvent())
        {
            var nonCloudEvent = new MotorCloudEvent<byte[]>(
                applicationNameService,
                message.Value,
                new Uri("kafka://notset")
            );
            AddHeadersToCloudEvent(message, nonCloudEvent);
            return nonCloudEvent;
        }

        var cloudEvent = message.ToCloudEvent(cloudEventFormatter);
        if (cloudEvent.Data is null)
        {
            throw new ArgumentException("Data property of CloudEvent is null");
        }
        if (cloudEvent.Source is null)
        {
            throw new ArgumentException("Source property of CloudEvent is null");
        }

        if (cloudEvent.Data is JsonElement element)
        {
            cloudEvent.Data = Encoding.UTF8.GetBytes(element.GetRawText());
        }
        var motorCloudEvent = new MotorCloudEvent<byte[]>(
            applicationNameService,
            (byte[])cloudEvent.Data,
            cloudEvent.Type,
            cloudEvent.Source,
            cloudEvent.Id,
            cloudEvent.Time,
            cloudEvent.DataContentType
        );
        foreach (var (key, value) in cloudEvent.GetPopulatedAttributes())
        {
            if (motorCloudEvent.GetAttribute(key.Name) is null)
            {
                motorCloudEvent[key] = value;
            }
        }

        AddHeadersToCloudEvent(message, motorCloudEvent);

        return motorCloudEvent;
    }

    private static void AddHeadersToCloudEvent(
        Message<string?, byte[]> message,
        MotorCloudEvent<byte[]> motorCloudEvent
    )
    {
        if (message.Headers is null)
        {
            return;
        }

        foreach (var header in message.Headers)
        {
            var headerValue = header.GetValueBytes();
            if (headerValue is not null)
            {
                try
                {
                    motorCloudEvent.SetAttributeFromString(
                        header.Key.ToLowerInvariant(),
                        Encoding.UTF8.GetString(headerValue)
                    );
                }
                catch (ArgumentException)
                {
                    // Ignore headers that cannot be parsed as valid CloudEvent attributes.
                }
            }
        }
    }
}
