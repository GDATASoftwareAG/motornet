using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CloudNative.CloudEvents;
using Motor.Extensions.Hosting.CloudEvents;
using Motor.Extensions.Hosting.RabbitMQ.Options;
using RabbitMQ.Client;

namespace Motor.Extensions.Hosting.RabbitMQ
{
    public static class BasicPropertiesExtensions
    {
        public static string CloudEventPrefix => "cloudEvents:";

        private static readonly List<CloudEventAttribute> IgnoredAttributes = new();

        static BasicPropertiesExtensions()
        {
            IgnoredAttributes.AddRange(RabbitMQBindingExtension.AllAttributes);
            IgnoredAttributes.AddRange(RabbitMQPriorityExtension.AllAttributes);
        }

        public static void Update<T>(this IBasicProperties self, MotorCloudEvent<byte[]> cloudEvent,
            RabbitMQPublisherOptions<T> options)
        {
            var messagePriority = cloudEvent.GetRabbitMQPriority() ?? options.DefaultPriority;
            if (messagePriority.HasValue)
                self.Priority = messagePriority.Value;

            var headers = new Dictionary<string, object>();

            var attributesToConsider = cloudEvent.GetPopulatedAttributes()
                .Where(t => !IgnoredAttributes.Contains(t.Key));

            foreach (var (key, value) in attributesToConsider)
            {
                headers.Add($"{CloudEventPrefix}{key.Name}", Encoding.UTF8.GetBytes(key.Format(value)));
            }

            self.Headers = headers;
        }

        public static MotorCloudEvent<byte[]> ExtractCloudEvent(this IBasicProperties self,
            IApplicationNameService applicationNameService, ReadOnlyMemory<byte> body)
        {
            var attributes = new Dictionary<string, object>();
            IDictionary<string, object> headers = new Dictionary<string, object>();
            if (self.IsHeadersPresent() && self.Headers is not null)
            {
                headers = self.Headers;
            }

            var cloudEventHeaders = headers
                .Where(t => t.Key.StartsWith(CloudEventPrefix))
                .Select(t => (Key: t.Key[CloudEventPrefix.Length..], t.Value))
                .Where(t => !t.Key.Equals(CloudEventsSpecVersion.SpecVersionAttribute.Name, StringComparison.InvariantCultureIgnoreCase));

            foreach (var (key, value) in cloudEventHeaders)
            {
                attributes.Add(key, value);
            }

            var cloudEvent = new MotorCloudEvent<byte[]>(applicationNameService, body.ToArray(), new Uri("rabbitmq://notset"));

            if (attributes.Count == 0)
            {
                return cloudEvent;
            }

            foreach (var (key, value) in attributes)
            {
                if (value is byte[] byteValue)
                {
                    cloudEvent.SetAttributeFromString(key.ToLowerInvariant(),
                        Encoding.UTF8.GetString(byteValue));
                }
            }

            return cloudEvent;
        }
    }
}
