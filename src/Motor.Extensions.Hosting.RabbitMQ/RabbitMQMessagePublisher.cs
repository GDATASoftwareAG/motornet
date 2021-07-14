using System;
using System.Threading;
using System.Threading.Tasks;
using CloudNative.CloudEvents;
using Microsoft.Extensions.Options;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.RabbitMQ.Options;
using RabbitMQ.Client;

namespace Motor.Extensions.Hosting.RabbitMQ
{
    public class RabbitMQMessagePublisher<T> : ITypedMessagePublisher<byte[]>
    {
        private readonly ICloudEventFormatter _cloudEventFormatter;
        private readonly RabbitMQPublisherOptions<T> _options;
        private readonly IRabbitMQConnectionFactory<T> _connectionFactory;
        private IModel? _channel;
        private bool _connected;

        public RabbitMQMessagePublisher(
            IRabbitMQConnectionFactory<T> connectionFactory,
            IOptions<RabbitMQPublisherOptions<T>> config,
            ICloudEventFormatter cloudEventFormatter
        )
        {
            _connectionFactory = connectionFactory;
            _cloudEventFormatter = cloudEventFormatter;
            _options = config.Value;
        }

        public async Task PublishMessageAsync(MotorCloudEvent<byte[]> cloudEvent, CancellationToken token = default)
        {
            if (!_connected)
            {
                await StartAsync().ConfigureAwait(false);
            }

            if (_channel is null)
            {
                throw new InvalidOperationException("Channel is not created.");
            }

            var properties = _channel.CreateBasicProperties();
            properties.DeliveryMode = 2;
            properties.Update(cloudEvent, _options, _cloudEventFormatter);

            var publishingTarget = cloudEvent.Extension<RabbitMQBindingExtension>()?.BindingOptions ?? _options.PublishingTarget;

            if (_options.OverwriteExchange)
            {
                publishingTarget.Exchange = _options.PublishingTarget.Exchange;
            }

            _channel.BasicPublish(
                publishingTarget.Exchange,
                publishingTarget.RoutingKey,
                true,
                properties,
                cloudEvent.TypedData
            );
        }

        private Task StartAsync()
        {
            _channel = _connectionFactory.CurrentChannel;
            _connected = true;
            return Task.CompletedTask;
        }
    }
}
