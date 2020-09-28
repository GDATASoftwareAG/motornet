using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.Extensions;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Diagnostics.Tracing;
using Microsoft.Extensions.Options;
using Motor.Extensions.Hosting.RabbitMQ.Config;
using OpenTracing;
using OpenTracing.Propagation;

namespace Motor.Extensions.Hosting.RabbitMQ
{
    public class RabbitMQMessagePublisher<T> : RabbitMQConnectionHandler, ITypedMessagePublisher<byte[]>
    {
        private bool _connected;
        private readonly IRabbitMQConnectionFactory _connectionFactory;
        private readonly ICloudEventFormatter _cloudEventFormatter;
        private readonly ITracer _tracer;
        private readonly RabbitMQPublisherConfig<T> _config;
        
        public RabbitMQMessagePublisher(IRabbitMQConnectionFactory connectionFactory,
            IOptions<RabbitMQPublisherConfig<T>> config, ICloudEventFormatter cloudEventFormatter, ITracer tracer)
        {
            _connectionFactory = connectionFactory;
            _cloudEventFormatter = cloudEventFormatter;
            _config = config.Value;
            _tracer = tracer;
        }
        
        private Task StartAsync()
        {
            SetConnectionFactory();
            EstablishConnection();
            EstablishChannel();
            _connected = true;
            return Task.CompletedTask;
        }
        
        private void SetConnectionFactory()
        {
            ConnectionFactory = _connectionFactory.From(_config);
        }
        
        public async Task PublishMessageAsync(MotorCloudEvent<byte[]> cloudEvent, CancellationToken token = default)
        {
            if (!_connected)
            {
                await StartAsync();
            }
            if (Channel == null)
            {
                throw new InvalidOperationException("Channel is not created.");
            }
            var properties = Channel.CreateBasicProperties();
            properties.DeliveryMode = 2;
            var messagePriority = cloudEvent.Extension<RabbitMQPriorityExtension>()?.Priority ??
                                        _config.DefaultPriority;
            if (messagePriority.HasValue)
                properties.Priority = messagePriority.Value;
            var dictionary = new Dictionary<string, object>();
            var spanContext = cloudEvent.Extension<JaegerTracingExtension>()?.SpanContext;
            if (spanContext != null)
            {
                _tracer.Inject(spanContext, BuiltinFormats.TextMap, new RabbitMQHeadersMap(dictionary));
            }

            foreach (var attr in cloudEvent.GetAttributes())
            {
                if (string.Equals(attr.Key, CloudEventAttributes.DataAttributeName(cloudEvent.SpecVersion))                    
                    || string.Equals(attr.Key, CloudEventAttributes.DataContentTypeAttributeName(cloudEvent.SpecVersion))
                    || string.Equals(attr.Key, RabbitMQPriorityExtension.PriorityAttributeName)
                    || string.Equals(attr.Key, RabbitMQBindingConfigExtension.ExchangeAttributeName)
                    || string.Equals(attr.Key, RabbitMQBindingConfigExtension.RoutingKeyAttributeName)
                    || string.Equals(attr.Key, DistributedTracingExtension.TraceParentAttributeName)
                    || string.Equals(attr.Key, DistributedTracingExtension.TraceStateAttributeName))
                {
                    continue;
                }
                dictionary.Add($"{RabbitMQPriorityExtension.CloudEventPrefix}-{attr.Key}", _cloudEventFormatter.EncodeAttribute(cloudEvent.SpecVersion, attr.Key, attr.Value, cloudEvent.GetExtensions().Values));
            }
            
            properties.Headers = dictionary;

            var publishingTarget = cloudEvent.Extension<RabbitMQBindingConfigExtension>()?.BindingConfig ??
                                   _config.PublishingTarget;

            Channel.BasicPublish(publishingTarget.Exchange, publishingTarget.RoutingKey, true, properties, cloudEvent.TypedData);
        }
    }
}
