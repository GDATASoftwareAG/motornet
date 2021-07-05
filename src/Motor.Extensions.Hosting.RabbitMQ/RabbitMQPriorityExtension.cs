using System.Collections.Generic;
using System.Linq;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.Core;
using Motor.Extensions.Hosting.CloudEvents;

namespace Motor.Extensions.Hosting.RabbitMQ
{
    public static class RabbitMQPriorityExtension
    {
        public static CloudEventAttribute RabbitMQPriorityAttribute { get; } =
            CloudEventAttribute.CreateExtension("priority", CloudEventAttributeType.Integer);

        public static IEnumerable<CloudEventAttribute> AllAttributes { get; } =
            new[] { RabbitMQPriorityAttribute }.ToList().AsReadOnly();

        public static MotorCloudEvent<TData> SetRabbitMQPriority<TData>(this MotorCloudEvent<TData> cloudEvent, byte? value) where TData : class
        {
            Validation.CheckNotNull(cloudEvent, nameof(cloudEvent));
            cloudEvent[RabbitMQPriorityAttribute] = (int?)value;
            return cloudEvent;
        }

        public static byte? GetRabbitMQPriority<TData>(this MotorCloudEvent<TData> cloudEvent) where TData : class
        {
            var priority = (int?)Validation.CheckNotNull(cloudEvent, nameof(cloudEvent))[RabbitMQPriorityAttribute];
            return priority is null or < 0 or > 255 ? null : (byte)priority;
        }
    }
}
