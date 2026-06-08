using System.Collections.Generic;
using CloudNative.CloudEvents;
using Motor.Extensions.Hosting.CloudEvents;
using CloudEventValidation = CloudNative.CloudEvents.Core.Validation;

namespace Motor.Extensions.Hosting.RabbitMQ;

public static class RabbitMQPriorityExtension
{
    public static CloudEventAttribute RabbitMQPriorityAttribute { get; } =
        CloudEventAttribute.CreateExtension("priority", CloudEventAttributeType.Integer);

    public static IReadOnlyList<CloudEventAttribute> AllAttributes { get; } = [RabbitMQPriorityAttribute];

    extension<TData>(MotorCloudEvent<TData> cloudEvent)
        where TData : class
    {
        public MotorCloudEvent<TData> SetRabbitMQPriority(byte? value)
        {
            CloudEventValidation.CheckNotNull(cloudEvent, nameof(cloudEvent));
            cloudEvent[RabbitMQPriorityAttribute] = (int?)value;
            return cloudEvent;
        }

        public byte? GetRabbitMQPriority() =>
            CloudEventValidation.CheckNotNull(cloudEvent, nameof(cloudEvent))[RabbitMQPriorityAttribute] switch
            {
                int and (< 0 or > 255) => null,
                int priority => (byte)priority,
                _ => null,
            };
    }
}
