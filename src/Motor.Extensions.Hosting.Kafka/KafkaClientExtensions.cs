using System;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.Kafka;
using Confluent.Kafka;
using Motor.Extensions.Hosting.Abstractions;

namespace Motor.Extensions.Hosting.Kafka
{
    internal static class KafkaClientExtensions
    {
        public static MotorCloudEvent<byte[]> ToMotorCloudEvent<T>(this ConsumeResult<string, byte[]> message,
            IApplicationNameService applicationNameService, ICloudEventFormatter cloudEventFormatter)
        {
            if (!message.Message.IsCloudEvent())
                return new MotorCloudEvent<byte[]>(applicationNameService, message.Message.Value, typeof(T).Name,
                    new Uri("kafka://notset"));
            var cloudEvent = message.Message.ToCloudEvent(cloudEventFormatter);
            var motorCloudEvent = new MotorCloudEvent<byte[]>(applicationNameService, (byte[]) cloudEvent.Data,
                cloudEvent.Type, cloudEvent.Source, cloudEvent.Id, cloudEvent.Time);
            var newAttributes = motorCloudEvent.GetAttributes();
            foreach (var (key, value) in cloudEvent.GetAttributes())
            {
                if (!newAttributes.ContainsKey(key))
                {
                    newAttributes.Add(key, value);
                }
            }

            return motorCloudEvent;
        }
    }
}
