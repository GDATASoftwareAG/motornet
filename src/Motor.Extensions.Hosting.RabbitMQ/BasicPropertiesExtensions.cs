using System.Collections.Generic;
using CloudNative.CloudEvents;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.RabbitMQ.Config;
using RabbitMQ.Client;

namespace Motor.Extensions.Hosting.RabbitMQ
{
    public static class BasicPropertiesExtensions
    {
        public static void Update<T>(this IBasicProperties self, MotorCloudEvent<byte[]> cloudEvent, 
            RabbitMQPublisherConfig<T> config, ICloudEventFormatter cloudEventFormatter)
        {
            
            var messagePriority = cloudEvent.Extension<RabbitMQPriorityExtension>()?.Priority ??
                                  config.DefaultPriority;
            if (messagePriority.HasValue)
                self.Priority = messagePriority.Value;
            var dictionary = new Dictionary<string, object>();

            foreach (var attr in cloudEvent.GetAttributes())
            {
                if (string.Equals(attr.Key, CloudEventAttributes.DataAttributeName(cloudEvent.SpecVersion))
                    || string.Equals(attr.Key,
                        CloudEventAttributes.DataContentTypeAttributeName(cloudEvent.SpecVersion))
                    || string.Equals(attr.Key, RabbitMQPriorityExtension.PriorityAttributeName)
                    || string.Equals(attr.Key, RabbitMQBindingConfigExtension.ExchangeAttributeName)
                    || string.Equals(attr.Key, RabbitMQBindingConfigExtension.RoutingKeyAttributeName))
                    continue;
                dictionary.Add($"{RabbitMQPriorityExtension.CloudEventPrefix}-{attr.Key}",
                    cloudEventFormatter.EncodeAttribute(cloudEvent.SpecVersion, attr.Key, attr.Value,
                        cloudEvent.GetExtensions().Values));
            }

            self.Headers = dictionary;
        }
    }
}
