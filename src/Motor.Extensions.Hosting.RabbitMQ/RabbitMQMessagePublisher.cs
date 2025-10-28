using System;
using System.Threading;
using System.Threading.Tasks;
using CloudNative.CloudEvents;
using Microsoft.Extensions.Options;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using Motor.Extensions.Hosting.RabbitMQ.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Motor.Extensions.Hosting.RabbitMQ;

public class RabbitMQMessagePublisher<TOutput> : IRawMessagePublisher<TOutput>
    where TOutput : notnull
{
    private readonly RabbitMQPublisherOptions<TOutput> _options;
    private readonly PublisherOptions _publisherOptions;
    private readonly CloudEventFormatter _cloudEventFormatter;
    private IChannel? _channel;
    private bool _connected;

    internal IRabbitMQConnectionFactory<TOutput> ConnectionFactory { get; }

    public RabbitMQMessagePublisher(
        IRabbitMQConnectionFactory<TOutput> connectionFactory,
        IOptions<RabbitMQPublisherOptions<TOutput>> config,
        IOptions<PublisherOptions> publisherOptions,
        CloudEventFormatter cloudEventFormatter
    )
    {
        ConnectionFactory = connectionFactory;
        _options = config.Value;
        _publisherOptions = publisherOptions.Value;
        _cloudEventFormatter = cloudEventFormatter;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _channel = await ConnectionFactory.CurrentChannelAsync();
        _connected = true;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        ConnectionFactory.Dispose();
        return Task.CompletedTask;
    }

    public async Task PublishMessageAsync(MotorCloudEvent<byte[]> motorCloudEvent, CancellationToken token = default)
    {
        try
        {
            if (!_connected || _channel is null)
            {
                throw new InvalidOperationException("Channel is not created.");
            }

            var properties = new BasicProperties { DeliveryMode = DeliveryModes.Persistent };
            properties.SetPriority(motorCloudEvent, _options);

            var exchange = motorCloudEvent.GetRabbitMQExchange() ?? _options.PublishingTarget.Exchange;
            var routingKey = motorCloudEvent.GetRabbitMQRoutingKey() ?? _options.PublishingTarget.RoutingKey;

            if (_options.OverwriteExchange)
            {
                exchange = _options.PublishingTarget.Exchange;
            }

            switch (_publisherOptions.CloudEventFormat)
            {
                case CloudEventFormat.Protocol:
                    properties.WriteCloudEventIntoHeader(motorCloudEvent);
                    await _channel.BasicPublishAsync(
                        exchange,
                        routingKey,
                        true,
                        properties,
                        motorCloudEvent.TypedData,
                        token
                    );
                    break;
                case CloudEventFormat.Json:
                    var data = _cloudEventFormatter.EncodeStructuredModeMessage(
                        motorCloudEvent.ConvertToCloudEvent(),
                        out _
                    );
                    await _channel.BasicPublishAsync(exchange, routingKey, true, properties, data, token);
                    break;
                default:
                    throw new UnhandledCloudEventFormatException(_publisherOptions.CloudEventFormat);
            }
        }
        catch (AlreadyClosedException e)
        {
            throw new TemporaryFailureException("Couldn't publish message", e, FailureLevel.Warning);
        }
        catch (BrokerUnreachableException e)
        {
            throw new TemporaryFailureException("Couldn't publish message", e, FailureLevel.Warning);
        }
    }
}
