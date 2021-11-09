using System;

using System.Threading;
using System.Threading.Tasks;
using CloudNative.CloudEvents;
using Microsoft.Extensions.Options;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using Motor.Extensions.Hosting.RabbitMQ.Options;
using RabbitMQ.Client;

namespace Motor.Extensions.Hosting.RabbitMQ;

public class RabbitMQMessagePublisher<TOutput> : IRawMessagePublisher<TOutput> where TOutput : notnull
{
    private readonly RabbitMQPublisherOptions<TOutput> _options;
    private IModel? _channel;
    private bool _connected;

    internal IRabbitMQConnectionFactory<TOutput> ConnectionFactory { get; }

    public RabbitMQMessagePublisher(
        IRabbitMQConnectionFactory<TOutput> connectionFactory,
        IOptions<RabbitMQPublisherOptions<TOutput>> config,
        CloudEventFormatter cloudEventFormatter
    )
    {
        ConnectionFactory = connectionFactory;
        _options = config.Value;
    }

    public async Task PublishMessageAsync(MotorCloudEvent<byte[]> motorCloudEvent, CancellationToken token = default)
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
        properties.Update(motorCloudEvent, _options);

        var exchange = motorCloudEvent.GetRabbitMQExchange() ?? _options.PublishingTarget.Exchange;
        var routingKey = motorCloudEvent.GetRabbitMQRoutingKey() ?? _options.PublishingTarget.RoutingKey;

        if (_options.OverwriteExchange)
        {
            exchange = _options.PublishingTarget.Exchange;
        }

        _channel.BasicPublish(exchange, routingKey, true, properties, motorCloudEvent.TypedData);
    }

    private Task StartAsync()
    {
        _channel = ConnectionFactory.CurrentChannel;
        _connected = true;
        return Task.CompletedTask;
    }
}
