using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using Motor.Extensions.Hosting.PgMq.Options;
using Npgmq;

namespace Motor.Extensions.Hosting.PgMq;

/// <summary>
/// Message consumer for pgmq (https://github.com/pgmq/pgmq).
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Uses Npgmq (https://github.com/brianpursley/Npgmq).</item>
/// </list>
/// </remarks>
/// <typeparam name="TData">The input message type.</typeparam>
public sealed class PgMqMessageConsumer<TData> : IMessageConsumer<TData>, IAsyncDisposable
    where TData : notnull
{
    private readonly PgMqConsumerOptions<TData> _options;
    private readonly ILogger<PgMqMessageConsumer<TData>> _logger;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly IApplicationNameService _applicationNameService;
    private readonly INpgmqClient _npgmqClient;
    private bool _started;

    public PgMqMessageConsumer(
        PgMqConsumerOptions<TData> options,
        ILogger<PgMqMessageConsumer<TData>> logger,
        IHostApplicationLifetime applicationLifetime,
        IApplicationNameService applicationNameService,
        INpgmqClient npgmqClient
    )
    {
        _options = options;
        _logger = logger;
        _applicationLifetime = applicationLifetime;
        _applicationNameService = applicationNameService;
        _npgmqClient = npgmqClient;
    }

    public Func<
        MotorCloudEvent<byte[]>,
        CancellationToken,
        Task<ProcessedMessageStatus>
    >? ConsumeCallbackAsync { get; set; }

    /// <summary>
    /// Starts the <see cref="PgMqMessageConsumer{TData}"/>.
    /// </summary>
    /// <param name="token">A cancellation token.</param>
    /// <returns>A <see cref="Task"/> that completes when the consumer is ready.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item>Creates the <see cref="NpgmqClient"/>.</item>
    /// <item>Ensures the pgmq extension has been created in Postgres.</item>
    /// <item>Creates the queue if it does not exist.</item>
    /// </list>
    /// </remarks>
    public async Task StartAsync(CancellationToken token = default)
    {
        await _npgmqClient.InitAsync(token);
        await _npgmqClient.CreateQueueAsync(_options.QueueName, token);
        _started = true;
    }

    /// <summary>
    /// Executes the <see cref="PgMqMessageConsumer{TData}"/>.
    /// </summary>
    /// <param name="token">A cancellation token.</param>
    /// <remarks>
    /// <list type="bullet">
    /// <item>Consumes messages from the queue sequentially.</item>
    /// <item>Uses polling with a delay when no messages are available.</item>
    /// <item>On <see cref="ProcessedMessageStatus.Success"/>, the message is acknowledged (deleted).</item>
    /// <item>On <see cref="ProcessedMessageStatus.TemporaryFailure"/>, the message is left in the queue to be redelivered after the visibility timeout.</item>
    /// <item>On <see cref="ProcessedMessageStatus.Failure"/> or <see cref="ProcessedMessageStatus.InvalidInput"/>, the message is deleted (not redelivered).</item>
    /// <item>On <see cref="ProcessedMessageStatus.CriticalFailure"/> or unhandled exception, the application is stopped.</item>
    /// </list>
    /// </remarks>
    public async Task ExecuteAsync(CancellationToken token = default)
    {
        if (!EnsureInitialized())
            return;
        while (!token.IsCancellationRequested)
        {
            try
            {
                await ProcessNextMessageAsync(token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                break;
            }
            catch (Exception e)
            {
                _logger.LogCritical(
                    LogEvents.MessageHandlingUnexpectedException,
                    e,
                    "Unexpected exception in message handling"
                );
                _applicationLifetime.StopApplication();
                break;
            }
        }
    }

    public Task StopAsync(CancellationToken token = default) => Task.CompletedTask;

    public ValueTask DisposeAsync() => _npgmqClient is IAsyncDisposable d ? d.DisposeAsync() : ValueTask.CompletedTask;

    private bool EnsureInitialized()
    {
        if (!_started)
        {
            _logger.LogError(
                LogEvents.ConsumerNotStarted,
                "Consumer not started. Call StartAsync before ExecuteAsync."
            );
            return false;
        }
        if (ConsumeCallbackAsync is null)
        {
            _logger.LogError(LogEvents.ConsumerNotStarted, "ConsumeCallbackAsync is not configured.");
            return false;
        }
        return true;
    }

    private async Task ProcessNextMessageAsync(CancellationToken token)
    {
        // Poll as byte[] to match the protocol format: the producer sends raw payload bytes,
        // which Npgmq stores as Base64 in the JSONB column and deserialises back to byte[] on read.
        var message = await _npgmqClient.PollAsync<byte[]>(
            _options.QueueName,
            _options.VisibilityTimeoutInSeconds,
            _options.PollTimeoutSeconds,
            _options.PollIntervalMilliseconds,
            token
        );

        if (message is null)
        {
            return;
        }

        // If the message has no pgmq headers with CloudEvent attributes, it is a JSON envelope
        // (structured content mode), which is not supported by this consumer.
        if (message.Headers is not { Count: > 0 })
            throw new NotImplementedException(
                "Structured-mode JSON CloudEvents are not supported by the PgMq consumer. Use protocol (binary content mode) format."
            );

        var cloudEvent = DecodeProtocolFormat(message.Message ?? Array.Empty<byte>(), message.Headers);

        var status = await InvokeCallbackAsync(cloudEvent, token);
        switch (status)
        {
            case ProcessedMessageStatus.Success:
                _logger.LogDebug(
                    LogEvents.MessageSuccessfullyProcessed,
                    "Message {MsgId} successfully processed, deleting from queue",
                    message.MsgId
                );
                await _npgmqClient.DeleteAsync(_options.QueueName, message.MsgId, token);
                break;
            case ProcessedMessageStatus.TemporaryFailure:
                _logger.LogWarning(
                    LogEvents.TemporaryFailureOnConsume,
                    "Message {MsgId} had a temporary failure, will be requeued after visibility timeout",
                    message.MsgId
                );
                break;
            case ProcessedMessageStatus.Failure:
                _logger.LogError(
                    LogEvents.FailureOnConsume,
                    "Message {MsgId} failed permanently, discarding",
                    message.MsgId
                );
                await _npgmqClient.DeleteAsync(_options.QueueName, message.MsgId, token);
                break;
            case ProcessedMessageStatus.InvalidInput:
                _logger.LogError(
                    LogEvents.InvalidInputOnConsume,
                    "Message {MsgId} has invalid input, discarding",
                    message.MsgId
                );
                await _npgmqClient.DeleteAsync(_options.QueueName, message.MsgId, token);
                break;
            case ProcessedMessageStatus.CriticalFailure:
                _logger.LogCritical(
                    LogEvents.CriticalFailureOnConsume,
                    "Message {MsgId} in queue {QueueName} processing failed with critical failure, stopping application",
                    message.MsgId,
                    _options.QueueName
                );
                // Make the message immediately visible again so it can be reprocessed after a restart.
                await _npgmqClient.SetVtAsync(_options.QueueName, message.MsgId, 0, token);
                _applicationLifetime.StopApplication();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status.ToString());
        }
    }

    /// <summary>
    /// Protocol (binary content mode): CloudEvent attributes are stored in pgmq headers,
    /// the body is the raw payload as <c>byte[]</c> (Npgmq deserialises from the Base64-encoded JSONB value).
    /// </summary>
    private MotorCloudEvent<byte[]> DecodeProtocolFormat(byte[] body, IReadOnlyDictionary<string, object> headers)
    {
        var cloudEvent = new MotorCloudEvent<byte[]>(_applicationNameService, body, new Uri("pgmq://notset"));
        foreach (var (key, value) in headers)
        {
            try
            {
                cloudEvent.SetAttributeFromString(key, value.ToString() ?? string.Empty);
            }
            catch (ArgumentException)
            {
                // Ignore headers that cannot be parsed as valid CloudEvent attributes.
            }
        }
        return cloudEvent;
    }

    private Task<ProcessedMessageStatus> InvokeCallbackAsync(
        MotorCloudEvent<byte[]> cloudEvent,
        CancellationToken token
    )
    {
        return ConsumeCallbackAsync!(cloudEvent, token);
    }
}
