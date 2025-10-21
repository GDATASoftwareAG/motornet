using System.Collections.Generic;
using System.Linq;
using CloudNative.CloudEvents;
using Motor.Extensions.Hosting.CloudEvents;
using CloudEventValidation = CloudNative.CloudEvents.Core.Validation;

namespace Motor.Extensions.Hosting.RabbitMQ;

public static class RabbitMQPriorityExtension
{
    public static CloudEventAttribute RabbitMQPriorityAttribute { get; } =
        CloudEventAttribute.CreateExtension("priority", CloudEventAttributeType.Integer);

    public static IEnumerable<CloudEventAttribute> AllAttributes { get; } =
        new[] { RabbitMQPriorityAttribute }.ToList().AsReadOnly();

    public static MotorCloudEvent<TData> SetRabbitMQPriority<TData>(this MotorCloudEvent<TData> cloudEvent, byte? value)
        where TData : class
    {
        CloudEventValidation.CheckNotNull(cloudEvent, nameof(cloudEvent));
        cloudEvent[RabbitMQPriorityAttribute] = (int?)value;
        return cloudEvent;
    }

    public static byte? GetRabbitMQPriority<TData>(this MotorCloudEvent<TData> cloudEvent)
        where TData : class
    {
        return CloudEventValidation.CheckNotNull(cloudEvent, nameof(cloudEvent))[RabbitMQPriorityAttribute] switch
        {
            int and (< 0 or > 255) => null,
            int priority => (byte)priority,
            _ => null,
        };
    }
}
