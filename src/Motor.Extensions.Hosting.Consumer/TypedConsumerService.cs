using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Motor.Extensions.Compression.Abstractions;
using Motor.Extensions.Conversion.Abstractions;
using Motor.Extensions.Diagnostics.Metrics;
using Motor.Extensions.Diagnostics.Metrics.Abstractions;
using Motor.Extensions.Hosting.Abstractions;
using Prometheus.Client;

namespace Motor.Extensions.Hosting.Consumer
{
    public class TypedConsumerService<TInput> : BackgroundService
        where TInput : class
    {
        private readonly IMessageConsumer<TInput> _consumer;
        private readonly IMessageDeserializer<TInput> _deserializer;
        private readonly Dictionary<string, IMessageDecompressor> _decompressorByCompressionType;
        private readonly ILogger<TypedConsumerService<TInput>> _logger;
        private readonly IBackgroundTaskQueue<MotorCloudEvent<TInput>> _queue;
        private readonly ISummary? _messageDeserialization;
        private readonly ISummary? _messageDecompression;

        public TypedConsumerService(
            ILogger<TypedConsumerService<TInput>> logger,
            IMetricsFactory<TypedConsumerService<TInput>>? metrics,
            IBackgroundTaskQueue<MotorCloudEvent<TInput>> queue,
            IMessageDeserializer<TInput> deserializer,
            IEnumerable<IMessageDecompressor> decompressors,
            IMessageConsumer<TInput> consumer)
        {
            _logger = logger;
            _queue = queue;
            _deserializer = deserializer;
            _consumer = consumer;
            _consumer.ConsumeCallbackAsync = SingleMessageConsumeAsync;

            _messageDeserialization =
                metrics?.CreateSummary("message_deserialization", "Message deserialization duration in ms");
            _messageDecompression =
                metrics?.CreateSummary("message_decompression", "Message decompression duration in ms");

            _decompressorByCompressionType = new Dictionary<string, IMessageDecompressor>();
            foreach (var decompressor in decompressors)
            {
                _decompressorByCompressionType[decompressor.CompressionType] = decompressor;
            }
        }

        public override async Task StartAsync(CancellationToken token)
        {
            await _consumer.StartAsync(token);
            await base.StartAsync(token);
        }

        public override async Task StopAsync(CancellationToken token)
        {
            await base.StopAsync(token);
            await _consumer.StopAsync(token);
        }

        private async Task<ProcessedMessageStatus> SingleMessageConsumeAsync(MotorCloudEvent<byte[]> dataCloudEvent,
            CancellationToken token)
        {
            try
            {
                byte[] decompressed;
                using (new AutoObserveStopwatch(() => _messageDecompression))
                {
                    decompressed = await DecompressMessageAsync(
                        dataCloudEvent.Extension<CompressionTypeExtension>()?.CompressionType ??
                        NoOpMessageCompressor.NoOpCompressionType, dataCloudEvent.TypedData, token);
                }

                TInput deserialized;
                using (new AutoObserveStopwatch(() => _messageDeserialization))
                {
                    deserialized = _deserializer.Deserialize(decompressed);
                }

                return await _queue
                    .QueueBackgroundWorkItem(dataCloudEvent.CreateNew(deserialized, true))
                    .ConfigureAwait(true);
            }
            catch (ArgumentException e)
            {
                _logger.LogError(LogEvents.InvalidInput, e, "Invalid Input");
                return ProcessedMessageStatus.InvalidInput;
            }
            catch (Exception e)
            {
                _logger.LogError(LogEvents.UnexpectedErrorOnMessageProcessing, e, "Invalid Input");
                return ProcessedMessageStatus.CriticalFailure;
            }
        }

        private async Task<byte[]> DecompressMessageAsync(string compressionType, byte[] compressed,
            CancellationToken cancellationToken)
        {
            if (!_decompressorByCompressionType.TryGetValue(compressionType, out var decompressor))
            {
                throw new ArgumentException($"Unsupported compressionType {compressionType}");
            }

            return await decompressor.DecompressAsync(compressed, cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken token)
        {
            _consumer.ConsumeCallbackAsync = SingleMessageConsumeAsync;
            return _consumer.ExecuteAsync(token);
        }
    }
}
