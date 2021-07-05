using System.Threading;
using System.Threading.Tasks;
using Motor.Extensions.Compression.Abstractions;
using Motor.Extensions.Conversion.Abstractions;
using Motor.Extensions.Diagnostics.Metrics;
using Motor.Extensions.Diagnostics.Metrics.Abstractions;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using Prometheus.Client;

namespace Motor.Extensions.Hosting.Publisher
{
    public class TypedMessagePublisher<TOutput, TPublisher> : ITypedMessagePublisher<TOutput>
        where TPublisher : ITypedMessagePublisher<byte[]>
        where TOutput : class
    {
        private readonly TPublisher _bytesMessagePublisher;
        private readonly ISummary? _messageSerialization;
        private readonly ISummary? _messageCompression;
        private readonly IMessageSerializer<TOutput> _messageSerializer;
        private readonly IMessageCompressor _messageCompressor;

        public TypedMessagePublisher(IMetricsFactory<TypedMessagePublisher<TOutput, TPublisher>>? metrics,
            TPublisher bytesMessagePublisher, IMessageSerializer<TOutput> messageSerializer,
            IMessageCompressor messageCompressor)
        {
            _bytesMessagePublisher = bytesMessagePublisher;
            _messageSerializer = messageSerializer;
            _messageCompressor = messageCompressor;
            _messageSerialization =
                metrics?.CreateSummary("message_serialization", "Message serialization duration in ms");
            _messageCompression =
                metrics?.CreateSummary("message_compression", "Message compression duration in ms");
        }

        public async Task PublishMessageAsync(MotorCloudEvent<TOutput> motorCloudEvent, CancellationToken token = default)
        {
            byte[] bytes, compressedBytes;
            using (new AutoObserveStopwatch(() => _messageSerialization))
            {
                bytes = _messageSerializer.Serialize(motorCloudEvent.TypedData);
            }

            using (new AutoObserveStopwatch(() => _messageCompression))
            {
                compressedBytes = await _messageCompressor.CompressAsync(bytes, token);
            }

            var bytesEvent = motorCloudEvent.CreateNew(compressedBytes, true);
            bytesEvent.SetCompressionType(_messageCompressor.CompressionType);
            await _bytesMessagePublisher.PublishMessageAsync(bytesEvent, token);
        }
    }
}
