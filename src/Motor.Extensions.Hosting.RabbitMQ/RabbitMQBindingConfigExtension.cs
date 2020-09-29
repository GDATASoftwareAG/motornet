using System;
using System.Collections.Generic;
using CloudNative.CloudEvents;
using Motor.Extensions.Hosting.RabbitMQ.Config;

namespace Motor.Extensions.Hosting.RabbitMQ
{
    public class RabbitMQBindingConfigExtension : ICloudEventExtension
    {
        public const string RoutingKeyAttributeName = "binding-routing-key";
        public const string ExchangeAttributeName = "binding-exchange";
        private IDictionary<string, object> _attributes = new Dictionary<string, object>();


        public RabbitMQBindingConfigExtension(string exchange, string routingKey)
        {
            Exchange = exchange;
            RoutingKey = routingKey;
        }


        public string? Exchange
        {
            get => (string?) _attributes[ExchangeAttributeName];
            set
            {
                if (value != null) _attributes[ExchangeAttributeName] = value;
            }
        }

        public string? RoutingKey
        {
            get => (string?) _attributes[RoutingKeyAttributeName];
            set
            {
                if (value != null) _attributes[RoutingKeyAttributeName] = value;
            }
        }

        public RabbitMQBindingConfig? BindingConfig => Exchange != null && RoutingKey != null
            ? new RabbitMQBindingConfig
            {
                Exchange = Exchange,
                RoutingKey = RoutingKey
            }
            : null;

        public void Attach(CloudEvent cloudEvent)
        {
            var eventAttributes = cloudEvent.GetAttributes();
            if (_attributes == eventAttributes)
                // already done
                return;

            foreach (var attr in _attributes) eventAttributes[attr.Key] = attr.Value;

            _attributes = eventAttributes;
        }

        public bool ValidateAndNormalize(string key, ref dynamic value)
        {
            switch (key)
            {
                case RoutingKeyAttributeName:
                    switch (value)
                    {
                        case null:
                            return true;
                        case string _:
                            return true;
                        default:
                            throw new InvalidOperationException("ErrorRoutingKeyValueIsNotAString");
                    }
                case ExchangeAttributeName:
                    switch (value)
                    {
                        case null:
                            return true;
                        case string _:
                            return true;
                        default:
                            throw new InvalidOperationException("ErrorExchangeValueIsNotAString");
                    }
            }

            return false;
        }

        // Disabled null check because CloudEvent SDK doesn't 
        // implement null-checks
        #pragma warning disable CS8603
        public Type GetAttributeType(string name)
        {
            return name switch
            {
                RoutingKeyAttributeName => typeof(string),
                ExchangeAttributeName => typeof(string),
                _ => null
            };
        }
    }
}
