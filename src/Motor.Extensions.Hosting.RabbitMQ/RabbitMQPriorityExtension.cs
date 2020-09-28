using System;
using System.Collections.Generic;
using CloudNative.CloudEvents;

namespace Motor.Extensions.Hosting.RabbitMQ
{
    public class RabbitMQPriorityExtension : ICloudEventExtension
    {
        public static string CloudEventPrefix = "x-cloud-event";
        public const string PriorityAttributeName = "priority";
        IDictionary<string, object> attributes = new Dictionary<string, object>();

        public RabbitMQPriorityExtension(byte priority)
        {
            Priority = priority;
        }


        public byte? Priority
        {
            get => (byte?)attributes[PriorityAttributeName];
            set
            {
                if (value != null) attributes[PriorityAttributeName] = value;
            }
        }

        public void Attach(CloudEvent cloudEvent)
        {
            var eventAttributes = cloudEvent.GetAttributes();
            if (attributes == eventAttributes)
            {
                // already done
                return;
            }

            foreach (var attr in attributes)
            {
                eventAttributes[attr.Key] = attr.Value;
            }

            attributes = eventAttributes;
        }

        public bool ValidateAndNormalize(string key, ref object value)
        {
            switch (key)
            {
                case PriorityAttributeName:
                    switch (value)
                    {
                        case null:
                            return true;
                        case string s:
                        {
                            if (!byte.TryParse(s, out var i))
                                throw new InvalidOperationException("ErrorPriorityValueIsaNotAnInteger");
                            value = (byte?)i;
                            return true;
                        }
                        case byte b:
                            value = b;
                            return true;
                        default:
                            throw new InvalidOperationException("ErrorPriorityValueIsaNotAnInteger");
                    }
            }

            return false;
        }

        public Type GetAttributeType(string name)
        {
            return name switch
            {
                PriorityAttributeName => typeof(byte?),
                _ => null
            };
        }
    }
}
