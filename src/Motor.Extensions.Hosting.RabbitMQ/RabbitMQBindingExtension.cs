using System.Collections.Generic;
using System.Linq;
using CloudNative.CloudEvents;
using Motor.Extensions.Hosting.CloudEvents;
using CloudEventValidation = CloudNative.CloudEvents.Core.Validation;

namespace Motor.Extensions.Hosting.RabbitMQ;

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
        CloudEventValidation.CheckNotNull(cloudEvent, nameof(cloudEvent));
        cloudEvent[RabbitMQExchangeAttribute] = exchange;
        cloudEvent[RabbitMQRoutingKeyAttribute] = routingKey;
        return cloudEvent;
    }

    public static string? GetRabbitMQExchange<TData>(this MotorCloudEvent<TData> cloudEvent) where TData : class
    {
        return CloudEventValidation.CheckNotNull(cloudEvent, nameof(cloudEvent))[RabbitMQExchangeAttribute] as
            string;
    }

    public static string? GetRabbitMQRoutingKey<TData>(this MotorCloudEvent<TData> cloudEvent) where TData : class
    {
        return CloudEventValidation.CheckNotNull(cloudEvent, nameof(cloudEvent))[RabbitMQRoutingKeyAttribute] as
            string;
    }
}
