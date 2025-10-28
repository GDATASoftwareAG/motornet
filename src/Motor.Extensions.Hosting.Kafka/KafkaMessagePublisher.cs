using System;
using System.Threading;
using System.Threading.Tasks;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.Extensions;
using CloudNative.CloudEvents.Kafka;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using Motor.Extensions.Hosting.Kafka.Options;

namespace Motor.Extensions.Hosting.Kafka;

public class KafkaMessagePublisher<TOutput> : IRawMessagePublisher<TOutput>, IDisposable
    where TOutput : notnull
{
    private readonly CloudEventFormatter _cloudEventFormatter;
    private readonly IProducer<string?, byte[]> _producer;
    private readonly KafkaPublisherOptions<TOutput> _options;
    private readonly PublisherOptions _publisherOptions;

    public KafkaMessagePublisher(
        IOptions<KafkaPublisherOptions<TOutput>> options,
        CloudEventFormatter cloudEventFormatter,
        IOptions<PublisherOptions> publisherOptions
    )
    {
        _cloudEventFormatter = cloudEventFormatter;
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _publisherOptions = publisherOptions.Value ?? throw new ArgumentNullException(nameof(publisherOptions));
        _producer = new ProducerBuilder<string?, byte[]>(_options).Build();
    }

    public async Task PublishMessageAsync(MotorCloudEvent<byte[]> motorCloudEvent, CancellationToken token = default)
    {
        var topic = motorCloudEvent.GetKafkaTopic() ?? _options.Topic;
        var message = CloudEventToKafkaMessage(motorCloudEvent);
        await _producer.ProduceAsync(topic, message, token);
    }

    public Message<string?, byte[]> CloudEventToKafkaMessage(MotorCloudEvent<byte[]> motorCloudEvent)
    {
        var cloudEvent = motorCloudEvent.ConvertToCloudEvent();
        switch (_publisherOptions.CloudEventFormat)
        {
            case CloudEventFormat.Protocol:
                return cloudEvent.ToKafkaMessage(ContentMode.Binary, _cloudEventFormatter);
            case CloudEventFormat.Json:
                var value = _cloudEventFormatter.EncodeStructuredModeMessage(cloudEvent, out _);
                var key = cloudEvent[Partitioning.PartitionKeyAttribute] as string;
                return new Message<string?, byte[]> { Value = value.ToArray(), Key = key };
            default:
                throw new UnhandledCloudEventFormatException(_publisherOptions.CloudEventFormat);
        }
    }

    public void Dispose()
    {
        _producer.Dispose();
    }
}
