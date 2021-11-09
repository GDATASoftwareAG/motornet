using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Motor.Extensions.ContentEncoding.Abstractions;
using Motor.Extensions.Conversion.Abstractions;
using Motor.Extensions.Diagnostics.Metrics;
using Motor.Extensions.Diagnostics.Metrics.Abstractions;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using Prometheus.Client;

namespace Motor.Extensions.Hosting.Publisher;

public class TypedMessagePublisher<TOutput, TPublisher> : ITypedMessagePublisher<TOutput>
    where TPublisher : ITypedMessagePublisher<byte[]>
    where TOutput : class
{
    private readonly TPublisher _bytesMessagePublisher;
    private readonly ISummary? _messageSerialization;
    private readonly ISummary? _messageEncoding;
    private readonly IMessageSerializer<TOutput> _messageSerializer;
    private readonly ContentEncodingOptions _encodingOptions;
    private readonly IMessageEncoder _messageEncoder;

    public TypedMessagePublisher(IMetricsFactory<TypedMessagePublisher<TOutput, TPublisher>>? metrics,
        TPublisher bytesMessagePublisher, IMessageSerializer<TOutput> messageSerializer,
        IOptions<ContentEncodingOptions> encodingOptions, IMessageEncoder messageEncoder)
    {
        _bytesMessagePublisher = bytesMessagePublisher;
        _messageSerializer = messageSerializer;
        _encodingOptions = encodingOptions.Value;
        _messageEncoder = messageEncoder;
        _messageSerialization =
            metrics?.CreateSummary("message_serialization", "Message serialization duration in ms");
        _messageEncoding =
            metrics?.CreateSummary("message_encoding", "Message encoding duration in ms");
    }

    public async Task PublishMessageAsync(MotorCloudEvent<TOutput> motorCloudEvent, CancellationToken token = default)
    {
        byte[] bytes, encodedBytes;
        using (new AutoObserveStopwatch(() => _messageSerialization))
        {
            bytes = _messageSerializer.Serialize(motorCloudEvent.TypedData);
        }

        using (new AutoObserveStopwatch(() => _messageEncoding))
        {
            encodedBytes = await _messageEncoder.EncodeAsync(bytes, token);
        }

        var bytesEvent = motorCloudEvent.CreateNew(encodedBytes, true);
        bytesEvent.SetMotorVersion();

        if (!_encodingOptions.IgnoreEncoding)
        {
            bytesEvent.SetEncoding(_messageEncoder.Encoding);
        }

        await _bytesMessagePublisher.PublishMessageAsync(bytesEvent, token);
    }
}
