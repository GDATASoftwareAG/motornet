using System;
using System.Threading;
using System.Threading.Tasks;
using CloudNative.CloudEvents;
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
public sealed class PgMqMessageConsumer<TData> : IMessageConsumer<TData>
    where TData : notnull
{
    private readonly PgMqConsumerOptions<TData> _options;
    private readonly CloudEventFormatter _cloudEventFormatter;
    private readonly ILogger<PgMqMessageConsumer<TData>> _logger;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly IApplicationNameService _applicationNameService;
    private readonly INpgmqClient _npgmqClient;
    private IPgMqMessageReader? _messageReader;
    public PgMqMessageConsumer(
        PgMqConsumerOptions<TData> options,
        CloudEventFormatter cloudEventFormatter,
        ILogger<PgMqMessageConsumer<TData>> logger,
        IHostApplicationLifetime applicationLifetime,
        IApplicationNameService applicationNameService,
        INpgmqClient npgmqClient
    )
    {
        _options = options;
        _cloudEventFormatter = cloudEventFormatter;
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
        _messageReader = new AutoDetectMessageReader(_cloudEventFormatter, _applicationNameService);
        await _npgmqClient.InitAsync(token);
        await _npgmqClient.CreateQueueAsync(_options.QueueName, token);
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
        EnsureInitialized();
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
    public Task StopAsync(CancellationToken token = default)
    {
        return Task.CompletedTask;
    }
    private void EnsureInitialized()
    {
        if (_messageReader is null)
            throw new InvalidOperationException("Consumer not started. Call StartAsync before ExecuteAsync.");
        if (ConsumeCallbackAsync is null)
            throw new InvalidOperationException("ConsumeCallbackAsync is not configured.");
    }

    private async Task ProcessNextMessageAsync(CancellationToken token)
    {
        var message = await _messageReader!.ReadAsync(
            _npgmqClient,
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
        var status = await InvokeCallbackAsync(message.CloudEvent, token);
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
    private Task<ProcessedMessageStatus> InvokeCallbackAsync(
        MotorCloudEvent<byte[]> cloudEvent,
        CancellationToken token
    )
    {
        return ConsumeCallbackAsync!(cloudEvent, token);
    }
}
