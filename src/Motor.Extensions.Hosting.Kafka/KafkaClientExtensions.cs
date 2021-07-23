using System;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.Kafka;
using Confluent.Kafka;
using Motor.Extensions.Hosting.CloudEvents;

namespace Motor.Extensions.Hosting.Kafka
{
    internal static class KafkaClientExtensions
    {
        public static MotorCloudEvent<byte[]> ToMotorCloudEvent(this Message<string?, byte[]> message,
            IApplicationNameService applicationNameService, CloudEventFormatter cloudEventFormatter)
        {
            if (!message.IsCloudEvent())
            {
                return new MotorCloudEvent<byte[]>(applicationNameService, message.Value, new Uri("kafka://notset"));
            }

            var cloudEvent = message.ToCloudEvent(cloudEventFormatter);
            if (cloudEvent.Data is null)
            {
                throw new ArgumentException("Data property of CloudEvent is null");
            }
            if (cloudEvent.Source is null)
            {
                throw new ArgumentException("Source property of CloudEvent is null");
            }
            var motorCloudEvent = new MotorCloudEvent<byte[]>(applicationNameService, (byte[])cloudEvent.Data,
                cloudEvent.Type, cloudEvent.Source, cloudEvent.Id, cloudEvent.Time, cloudEvent.DataContentType);
            foreach (var (key, value) in cloudEvent.GetPopulatedAttributes())
            {
                if (motorCloudEvent.GetAttribute(key.Name) is null)
                {
                    motorCloudEvent[key] = value;
                }
            }

            return motorCloudEvent;
        }
    }
}
