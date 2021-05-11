using System;
using System.Collections.Generic;
using CloudNative.CloudEvents;

namespace Motor.Extensions.Compression.Abstractions
{
    public class CompressionTypeExtension : ICloudEventExtension
    {
        private const string CompressionTypeAttributeName = "compressionType";
        private IDictionary<string, object> _attributes = new Dictionary<string, object>();

        public CompressionTypeExtension(string compressionType)
        {
            CompressionType = compressionType;
        }

        public string? CompressionType
        {
            get => (string?)_attributes[CompressionTypeAttributeName];
            private init => _attributes[CompressionTypeAttributeName] =
                value ?? throw new ArgumentNullException(nameof(CompressionType));
        }

        public void Attach(CloudEvent cloudEvent)
        {
            var eventAttributes = cloudEvent.GetAttributes();
            if (_attributes == eventAttributes)
            {
                // already done
                return;
            }

            foreach (var (key, value) in _attributes)
            {
                eventAttributes[key] = value;
            }

            _attributes = eventAttributes;
        }

        public bool ValidateAndNormalize(string key, ref object value)
        {
            if (key is CompressionTypeAttributeName)
            {
                return value switch
                {
                    null => true,
                    string _ => true,
                    _ => throw new InvalidOperationException("ErrorCompressionTypeValueIsNotAString")
                };
            }

            return false;
        }

        // Disabled null check because CloudEvent SDK doesn't 
        // implement null-checks
#pragma warning disable CS8603
        public Type GetAttributeType(string name)
        {
            return name is CompressionTypeAttributeName ? typeof(string) : null;
        }
    }
}
