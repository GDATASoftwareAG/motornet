using System.Collections.Generic;
using System.Linq;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.Core;
using Motor.Extensions.Hosting.CloudEvents;

namespace Motor.Extensions.Hosting.Kafka;

public static class KafkaTopicExtension
{
    public static CloudEventAttribute KafkaTopicAttribute { get; } =
        CloudEventAttribute.CreateExtension("topic", CloudEventAttributeType.String);

    public static IEnumerable<CloudEventAttribute> AllAttributes { get; } =
        new[] { KafkaTopicAttribute }.ToList().AsReadOnly();

    public static MotorCloudEvent<TData> SetKafkaTopic<TData>(this MotorCloudEvent<TData> cloudEvent, string? value) where TData : class
    {
        Validation.CheckNotNull(cloudEvent, nameof(cloudEvent));
        cloudEvent[KafkaTopicAttribute] = value;
        return cloudEvent;
    }

    public static string? GetKafkaTopic<TData>(this MotorCloudEvent<TData> cloudEvent) where TData : class =>
        Validation.CheckNotNull(cloudEvent, nameof(cloudEvent))[KafkaTopicAttribute] as string;
}
