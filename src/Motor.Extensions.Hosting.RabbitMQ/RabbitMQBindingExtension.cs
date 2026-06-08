using System.Collections.Generic;
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

    public static IReadOnlyList<CloudEventAttribute> AllAttributes { get; } =
    [RabbitMQExchangeAttribute, RabbitMQRoutingKeyAttribute];

    extension<TData>(MotorCloudEvent<TData> cloudEvent)
        where TData : class
    {
        public MotorCloudEvent<TData> SetRabbitMQBinding(string? exchange, string? routingKey)
        {
            CloudEventValidation.CheckNotNull(cloudEvent, nameof(cloudEvent));
            cloudEvent[RabbitMQExchangeAttribute] = exchange;
            cloudEvent[RabbitMQRoutingKeyAttribute] = routingKey;
            return cloudEvent;
        }

        public string? GetRabbitMQExchange() =>
            CloudEventValidation.CheckNotNull(cloudEvent, nameof(cloudEvent))[RabbitMQExchangeAttribute] as string;

        public string? GetRabbitMQRoutingKey() =>
            CloudEventValidation.CheckNotNull(cloudEvent, nameof(cloudEvent))[RabbitMQRoutingKeyAttribute] as string;
    }
}
