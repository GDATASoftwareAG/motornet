using System.Collections.Generic;
using System.Linq;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.Core;
using Motor.Extensions.Hosting.CloudEvents;

namespace Motor.Extensions.Hosting.RabbitMQ
{
    public static class RabbitMQBindingExtension
    {
        public static CloudEventAttribute RabbitMQExchangeAttribute { get; } =
            CloudEventAttribute.CreateExtension("bindingexchange", CloudEventAttributeType.String);

        public static CloudEventAttribute RabbitMQRoutingKeyAttribute { get; } =
            CloudEventAttribute.CreateExtension("bindingroutingkey", CloudEventAttributeType.String);

        public static IEnumerable<CloudEventAttribute> AllAttributes { get; } =
            new[] { RabbitMQExchangeAttribute, RabbitMQRoutingKeyAttribute }.ToList().AsReadOnly();

        public static MotorCloudEvent<TData> SetRabbitMQBinding<TData>(this MotorCloudEvent<TData> cloudEvent,
            string? exchange, string? routingKey) where TData : class
        {
            Validation.CheckNotNull(cloudEvent, nameof(cloudEvent));
            cloudEvent[RabbitMQExchangeAttribute] = exchange;
            cloudEvent[RabbitMQRoutingKeyAttribute] = routingKey;
            return cloudEvent;
        }

        public static string? GetRabbitMQExchange<TData>(this MotorCloudEvent<TData> cloudEvent) where TData : class
        {
            return (string?)Validation.CheckNotNull(cloudEvent, nameof(cloudEvent))[RabbitMQExchangeAttribute];
        }

        public static string? GetRabbitMQRoutingKey<TData>(this MotorCloudEvent<TData> cloudEvent) where TData : class
        {
            return (string?)Validation.CheckNotNull(cloudEvent, nameof(cloudEvent))[RabbitMQRoutingKeyAttribute];
        }
    }
}
