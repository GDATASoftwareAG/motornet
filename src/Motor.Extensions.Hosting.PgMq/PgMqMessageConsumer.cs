using System;
using System.Text;
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
    private NpgmqClient? _npgmqClient;

    public PgMqMessageConsumer(
        PgMqConsumerOptions<TData> options,
        CloudEventFormatter cloudEventFormatter,
        ILogger<PgMqMessageConsumer<TData>> logger,
        IHostApplicationLifetime applicationLifetime,
        IApplicationNameService applicationNameService
    )
    {
        _options = options;
        _cloudEventFormatter = cloudEventFormatter;
        _logger = logger;
        _applicationLifetime = applicationLifetime;
        _applicationNameService = applicationNameService;
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
        _npgmqClient = new NpgmqClient(_options.ConnectionString);
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
        if (_npgmqClient is null)
        {
            _logger.LogError(
                LogEvents.ConsumerNotStarted,
                "Consumer not started. Call StartAsync before ExecuteAsync."
            );
            return;
        }

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

    private async Task ProcessNextMessageAsync(CancellationToken token)
    {
        long msgId;
        MotorCloudEvent<byte[]> motorCloudEvent;

        switch (_options.CloudEventFormat)
        {
            case CloudEventFormat.Protocol:
            {
                var message = await _npgmqClient!.ReadAsync<byte[]>(
                    _options.QueueName,
                    _options.VisibilityTimeoutInSeconds,
                    token
                );
                if (message is null)
                {
                    await Task.Delay(_options.PollingIntervalInMilliseconds, token);
                    return;
                }
                msgId = message.MsgId;
                motorCloudEvent = BuildCloudEventFromHeaders(message.Message ?? Array.Empty<byte>(), message.Headers);
                break;
            }
            case CloudEventFormat.Json:
            {
                var message = await _npgmqClient!.ReadAsync<string>(
                    _options.QueueName,
                    _options.VisibilityTimeoutInSeconds,
                    token
                );
                if (message is null)
                {
                    await Task.Delay(_options.PollingIntervalInMilliseconds, token);
                    return;
                }
                msgId = message.MsgId;
                var jsonBytes = Encoding.UTF8.GetBytes(message.Message ?? string.Empty);
                var cloudEvent = _cloudEventFormatter.DecodeStructuredModeMessage(jsonBytes, null, null);
                motorCloudEvent = cloudEvent.ToMotorCloudEvent(_applicationNameService);
                break;
            }
            default:
                throw new UnhandledCloudEventFormatException(_options.CloudEventFormat);
        }

        var status = await InvokeCallbackAsync(motorCloudEvent, token);

        switch (status)
        {
            case ProcessedMessageStatus.Success:
                _logger.LogDebug(
                    LogEvents.MessageSuccessfullyProcessed,
                    "Message {MsgId} successfully processed, deleting from queue",
                    msgId
                );
                await _npgmqClient!.DeleteAsync(_options.QueueName, msgId, token);
                break;
            case ProcessedMessageStatus.TemporaryFailure:
                _logger.LogWarning(
                    LogEvents.TemporaryFailureOnConsume,
                    "Message {MsgId} had a temporary failure, will be requeued after visibility timeout",
                    msgId
                );
                break;
            case ProcessedMessageStatus.Failure:
                _logger.LogError(LogEvents.FailureOnConsume, "Message {MsgId} failed permanently, discarding", msgId);
                await _npgmqClient!.DeleteAsync(_options.QueueName, msgId, token);
                break;
            case ProcessedMessageStatus.InvalidInput:
                _logger.LogError(LogEvents.InvalidInputOnConsume, "Message {MsgId} has invalid input, discarding", msgId);
                await _npgmqClient!.DeleteAsync(_options.QueueName, msgId, token);
                break;
            case ProcessedMessageStatus.CriticalFailure:
                _logger.LogCritical(
                    LogEvents.CriticalFailureOnConsume,
                    "Message {MsgId} processing failed with critical failure, stopping application",
                    msgId
                );
                _applicationLifetime.StopApplication();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status.ToString());
        }
    }

    private async Task<ProcessedMessageStatus> InvokeCallbackAsync(
        MotorCloudEvent<byte[]> cloudEvent,
        CancellationToken token
    )
    {
        return await (ConsumeCallbackAsync ?? throw new InvalidOperationException("ConsumeCallbackAsync is not configured."))(cloudEvent, token);
    }

    private MotorCloudEvent<byte[]> BuildCloudEventFromHeaders(
        byte[] body,
        System.Collections.Generic.IReadOnlyDictionary<string, object>? headers
    )
    {
        var cloudEvent = new MotorCloudEvent<byte[]>(_applicationNameService, body, new Uri("pgmq://notset"));

        if (headers is not null)
        {
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
        }

        return cloudEvent;
    }
}
