using System;
using System.Collections.Generic;
using System.Linq;
using CloudNative.CloudEvents;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.RabbitMQ.Options;
using RabbitMQ.Client;

namespace Motor.Extensions.Hosting.RabbitMQ
{
    public static class BasicPropertiesExtensions
    {
        public static readonly string CloudEventPrefix = "cloudEvents:";

        public static void Update<T>(this IBasicProperties self, MotorCloudEvent<byte[]> cloudEvent,
            RabbitMQPublisherOptions<T> options, ICloudEventFormatter cloudEventFormatter)
        {
            var messagePriority = cloudEvent.Extension<RabbitMQPriorityExtension>()?.Priority ??
                                  options.DefaultPriority;
            if (messagePriority.HasValue)
                self.Priority = messagePriority.Value;
            var dictionary = new Dictionary<string, object>();

            foreach (var attr in cloudEvent.GetAttributes())
            {
                if (string.Equals(attr.Key, CloudEventAttributes.DataAttributeName(cloudEvent.SpecVersion))
                    || string.Equals(attr.Key,
                        CloudEventAttributes.DataContentTypeAttributeName(cloudEvent.SpecVersion))
                    || string.Equals(attr.Key, RabbitMQPriorityExtension.PriorityAttributeName)
                    || string.Equals(attr.Key, RabbitMQBindingExtension.ExchangeAttributeName)
                    || string.Equals(attr.Key, RabbitMQBindingExtension.RoutingKeyAttributeName))
                    continue;
                dictionary.Add($"{CloudEventPrefix}{attr.Key}",
                    cloudEventFormatter.EncodeAttribute(cloudEvent.SpecVersion, attr.Key, attr.Value,
                        cloudEvent.GetExtensions().Values));
            }

            self.Headers = dictionary;
        }

        public static MotorCloudEvent<byte[]> ExtractCloudEvent<T>(this IBasicProperties self,
            IApplicationNameService applicationNameService, ICloudEventFormatter cloudEventFormatter,
            ReadOnlyMemory<byte> body,
            IReadOnlyCollection<ICloudEventExtension> extensions)
        {
            var specVersion = CloudEventsSpecVersion.V1_0;
            var attributes = new Dictionary<string, object>();
            IDictionary<string, object> headers = new Dictionary<string, object>();
            if (self.IsHeadersPresent() && self.Headers != null)
            {
                headers = self.Headers;
            }

            foreach (var header in headers
                .Where(t => t.Key.StartsWith(CloudEventPrefix))
                .Select(t =>
                    new KeyValuePair<string, object>(
                        t.Key.Substring(CloudEventPrefix.Length),
                        t.Value)))
            {
                if (string.Equals(header.Key, CloudEventAttributes.DataContentTypeAttributeName(specVersion),
                        StringComparison.InvariantCultureIgnoreCase)
                    || string.Equals(header.Key, CloudEventAttributes.SpecVersionAttributeName(specVersion),
                        StringComparison.InvariantCultureIgnoreCase))
                    continue;

                attributes.Add(header.Key, header.Value);
            }

            if (attributes.Count == 0)
                return new MotorCloudEvent<byte[]>(applicationNameService, body.ToArray(), typeof(T).Name,
                    new Uri("rabbitmq://notset"), extensions: extensions.ToArray());

            var cloudEvent = new MotorCloudEvent<byte[]>(applicationNameService, body.ToArray(), extensions);

            foreach (var attribute in attributes)
                cloudEvent.GetAttributes().Add(attribute.Key, cloudEventFormatter.DecodeAttribute(
                    cloudEvent.SpecVersion, attribute.Key, (byte[]) attribute.Value, extensions));

            return cloudEvent;
        }
    }
}
