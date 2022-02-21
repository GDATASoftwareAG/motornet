using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Motor.Extensions.ContentEncoding.Abstractions;
using Motor.Extensions.Conversion.Abstractions;
using Motor.Extensions.Diagnostics.Metrics;
using Motor.Extensions.Diagnostics.Metrics.Abstractions;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using Prometheus.Client;

namespace Motor.Extensions.Hosting.Consumer;

public class TypedConsumerService<TInput> : BackgroundService
    where TInput : class
{
    private readonly IMessageConsumer<TInput> _consumer;
    private readonly IMessageDeserializer<TInput> _deserializer;
    private readonly Dictionary<string, IMessageDecoder> _decoderByEncoding;
    private readonly ILogger<TypedConsumerService<TInput>> _logger;
    private readonly IBackgroundTaskQueue<MotorCloudEvent<TInput>> _queue;
    private readonly ISummary? _messageDeserialization;
    private readonly ContentEncodingOptions _encodingOptions;
    private readonly ISummary? _messageDecoding;

    public TypedConsumerService(
        ILogger<TypedConsumerService<TInput>> logger,
        IMetricsFactory<TypedConsumerService<TInput>>? metrics,
        IBackgroundTaskQueue<MotorCloudEvent<TInput>> queue,
        IMessageDeserializer<TInput> deserializer,
        IOptions<ContentEncodingOptions> encodingOptions,
        IEnumerable<IMessageDecoder> decoders,
        IMessageConsumer<TInput> consumer)
    {
        _logger = logger;
        _queue = queue;
        _deserializer = deserializer;
        _consumer = consumer;
        _consumer.ConsumeCallbackAsync = SingleMessageConsumeAsync;
        _encodingOptions = encodingOptions.Value;

        _messageDeserialization =
            metrics?.CreateSummary("message_deserialization", "Message deserialization duration in ms");
        _messageDecoding =
            metrics?.CreateSummary("message_decoding", "Message decoding duration in ms");

        _decoderByEncoding = new Dictionary<string, IMessageDecoder>();
        foreach (var decoder in decoders)
        {
            _decoderByEncoding[decoder.Encoding] = decoder;
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
            byte[] decoded;
            using (new AutoObserveStopwatch(() => _messageDecoding))
            {
                decoded = await DecodeMessageAsync(
                    dataCloudEvent.GetEncoding(), dataCloudEvent.TypedData, token);
            }

            TInput deserialized;
            using (new AutoObserveStopwatch(() => _messageDeserialization))
            {
                deserialized = _deserializer.Deserialize(decoded);
            }

            return await _queue
                .QueueBackgroundWorkItem(dataCloudEvent.CreateNew(deserialized, true))
                .ConfigureAwait(true);
        }
        catch (ArgumentException e)
        {
            if (e.InnerException is OperationCanceledException)
            {
                _logger.LogWarning(LogEvents.ServiceWasStopped, e.InnerException, "Service was already stopped");
                return ProcessedMessageStatus.TemporaryFailure;
            }
            _logger.LogError(LogEvents.InvalidInput, e, "Invalid Input");
            return ProcessedMessageStatus.InvalidInput;
        }
        catch (Exception e)
        {
            _logger.LogError(LogEvents.UnexpectedErrorOnMessageProcessing, e, "Invalid Input");
            return ProcessedMessageStatus.CriticalFailure;
        }
    }

    private async Task<byte[]> DecodeMessageAsync(string encoding, byte[] encodedMsg,
        CancellationToken cancellationToken)
    {
        if (!_decoderByEncoding.TryGetValue(
            _encodingOptions.IgnoreEncoding ? NoOpMessageEncoder.NoEncoding : encoding, out var decoder))
        {
            throw new ArgumentException($"Unsupported encoding {encoding}");
        }

        return await decoder.DecodeAsync(encodedMsg, cancellationToken);
    }

    protected override Task ExecuteAsync(CancellationToken token)
    {
        _consumer.ConsumeCallbackAsync = SingleMessageConsumeAsync;
        return _consumer.ExecuteAsync(token);
    }
}
