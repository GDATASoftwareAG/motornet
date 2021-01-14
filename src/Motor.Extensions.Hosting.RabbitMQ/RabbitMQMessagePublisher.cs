using System;
using System.Threading;
using System.Threading.Tasks;
using CloudNative.CloudEvents;
using Microsoft.Extensions.Options;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.RabbitMQ.Options;

namespace Motor.Extensions.Hosting.RabbitMQ
{
    public class RabbitMQMessagePublisher<T> : RabbitMQConnectionHandler, ITypedMessagePublisher<byte[]>
    {
        private readonly ICloudEventFormatter _cloudEventFormatter;
        private readonly RabbitMQPublisherOptions<T> _options;
        private readonly IRabbitMQConnectionFactory _connectionFactory;
        private bool _connected;

        public RabbitMQMessagePublisher(IRabbitMQConnectionFactory connectionFactory,
            IOptions<RabbitMQPublisherOptions<T>> config, ICloudEventFormatter cloudEventFormatter)
        {
            _connectionFactory = connectionFactory;
            _cloudEventFormatter = cloudEventFormatter;
            _options = config.Value;
        }

        public async Task PublishMessageAsync(MotorCloudEvent<byte[]> cloudEvent, CancellationToken token = default)
        {
            if (!_connected) await StartAsync().ConfigureAwait(false);
            if (Channel is null) throw new InvalidOperationException("Channel is not created.");
            var properties = Channel.CreateBasicProperties();
            properties.DeliveryMode = 2;
            properties.Update(cloudEvent, _options, _cloudEventFormatter);

            var publishingTarget = cloudEvent.Extension<RabbitMQBindingExtension>()?.BindingOptions ??
                                   _options.PublishingTarget;

            Channel.BasicPublish(publishingTarget.Exchange, publishingTarget.RoutingKey, true, properties,
                cloudEvent.TypedData);
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
            ConnectionFactory = _connectionFactory.From(_options);
        }
    }
}
