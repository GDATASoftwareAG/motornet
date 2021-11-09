using System;
using System.Threading;
using System.Threading.Tasks;
using CloudNative.CloudEvents;
using Microsoft.Extensions.Options;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using Motor.Extensions.Hosting.NATS.Options;
using NATS.Client;

namespace Motor.Extensions.Hosting.NATS;

public class NATSMessagePublisher<TOutput> : IRawMessagePublisher<TOutput>, IDisposable where TOutput : notnull
{
    private readonly CloudEventFormatter _cloudEventFormatter;
    private readonly NATSBaseOptions _options;
    private readonly IConnection _client;
    private readonly PublisherOptions _publisherOptions;

    public NATSMessagePublisher(
        IOptions<NATSBaseOptions> options,
        INATSClientFactory natsClientFactory,
        CloudEventFormatter cloudEventFormatter,
        IOptions<PublisherOptions> publisherOptions)
    {
        _options = options.Value;
        _client = natsClientFactory.From(_options);
        _cloudEventFormatter = cloudEventFormatter;
        _publisherOptions = publisherOptions.Value;
    }

    public Task PublishMessageAsync(MotorCloudEvent<byte[]> motorCloudEvent, CancellationToken token = default)
    {
        switch (_publisherOptions.CloudEventFormat)
        {
            case CloudEventFormat.Protocol:
                _client.Publish(_options.Topic, motorCloudEvent.TypedData);
                break;
            case CloudEventFormat.Json:
                var value = _cloudEventFormatter.EncodeStructuredModeMessage(motorCloudEvent.ConvertToCloudEvent(), out _);
                _client.Publish(_options.Topic, value.ToArray());
                break;
            default:
                throw new UnhandledCloudEventFormatException(_publisherOptions.CloudEventFormat);
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
