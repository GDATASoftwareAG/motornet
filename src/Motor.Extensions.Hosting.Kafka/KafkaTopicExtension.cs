using System;
using System.Collections.Generic;
using CloudNative.CloudEvents;

namespace Motor.Extensions.Hosting.Kafka
{
    public class KafkaTopicExtension : ICloudEventExtension
    {
        public const string TopicAttributeName = "topic";
        private IDictionary<string, object> _attributes = new Dictionary<string, object>();

        public KafkaTopicExtension(string topic)
        {
            Topic = topic;
        }

        public string? Topic
        {
            get => (string?)_attributes[TopicAttributeName];
            set
            {
                if (value is not null) _attributes[TopicAttributeName] = value;
            }
        }

        public void Attach(CloudEvent cloudEvent)
        {
            var eventAttributes = cloudEvent.GetAttributes();
            if (_attributes == eventAttributes)
                // already done
                return;

            foreach (var attr in _attributes) eventAttributes[attr.Key] = attr.Value;

            _attributes = eventAttributes;
        }

        public bool ValidateAndNormalize(string key, ref object value)
        {
            if (key is TopicAttributeName)
            {
                return value switch
                {
                    null => true,
                    string _ => true,
                    _ => throw new InvalidOperationException("ErrorTopicValueIsNotAString")
                };
            }

            return false;
        }

        // Disabled null check because CloudEvent SDK doesn't 
        // implement null-checks
#pragma warning disable CS8603
        public Type GetAttributeType(string name)
        {
            return name is TopicAttributeName ? typeof(string) : null;
        }
    }
}
