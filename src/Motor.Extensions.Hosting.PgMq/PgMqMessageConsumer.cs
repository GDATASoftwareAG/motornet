using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CloudNative.CloudEvents;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        IOptions<PgMqConsumerOptions<TData>> options,
        CloudEventFormatter cloudEventFormatter,
        ILogger<PgMqMessageConsumer<TData>> logger,
        IHostApplicationLifetime applicationLifetime,
        IApplicationNameService applicationNameService
    )
    {
        _options = options.Value;
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
    /// <item>On any other status or unhandled exception, the application is stopped.</item>
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
        switch (_options.CloudEventFormat)
        {
            case CloudEventFormat.Protocol:
                await ProcessProtocolMessageAsync(token);
                break;
            case CloudEventFormat.Json:
                await ProcessJsonMessageAsync(token);
                break;
            default:
                throw new UnhandledCloudEventFormatException(_options.CloudEventFormat);
        }
    }

    private async Task ProcessProtocolMessageAsync(CancellationToken token)
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

        var cloudEvent = BuildCloudEventFromHeaders(message.Message ?? Array.Empty<byte>(), message.Headers);
        var status = await InvokeCallbackAsync(cloudEvent, token);

        if (status == ProcessedMessageStatus.Success)
        {
            await _npgmqClient.DeleteAsync(_options.QueueName, message.MsgId, token);
        }
        else
        {
            _logger.LogCritical(
                LogEvents.CriticalFailureOnConsume,
                "Message processing failed with status {Status}, stopping application",
                status
            );
            _applicationLifetime.StopApplication();
        }
    }

    private async Task ProcessJsonMessageAsync(CancellationToken token)
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

        var jsonBytes = Encoding.UTF8.GetBytes(message.Message ?? string.Empty);
        var cloudEvent = _cloudEventFormatter.DecodeStructuredModeMessage(jsonBytes, null, null);
        var motorCloudEvent = ToMotorCloudEvent(cloudEvent);

        var status = await InvokeCallbackAsync(motorCloudEvent, token);

        if (status == ProcessedMessageStatus.Success)
        {
            await _npgmqClient.DeleteAsync(_options.QueueName, message.MsgId, token);
        }
        else
        {
            _logger.LogCritical(
                LogEvents.CriticalFailureOnConsume,
                "Message processing failed with status {Status}, stopping application",
                status
            );
            _applicationLifetime.StopApplication();
        }
    }

    private async Task<ProcessedMessageStatus> InvokeCallbackAsync(
        MotorCloudEvent<byte[]> cloudEvent,
        CancellationToken token
    )
    {
        if (ConsumeCallbackAsync is null)
        {
            throw new InvalidOperationException("ConsumeCallbackAsync is not configured.");
        }

        return await ConsumeCallbackAsync(cloudEvent, token);
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

    private MotorCloudEvent<byte[]> ToMotorCloudEvent(CloudEvent cloudEvent)
    {
        if (cloudEvent.Data is null)
        {
            throw new ArgumentException("Data property of CloudEvent is null");
        }

        if (cloudEvent.Source is null)
        {
            throw new ArgumentException("Source property of CloudEvent is null");
        }

        var data = cloudEvent.Data switch
        {
            byte[] bytes => bytes,
            _ => Encoding.UTF8.GetBytes(cloudEvent.Data.ToString() ?? string.Empty),
        };

        var motorCloudEvent = new MotorCloudEvent<byte[]>(
            _applicationNameService,
            data,
            cloudEvent.Type,
            cloudEvent.Source,
            cloudEvent.Id,
            cloudEvent.Time,
            cloudEvent.DataContentType
        );

        foreach (var (key, value) in cloudEvent.GetPopulatedAttributes())
        {
            if (motorCloudEvent.GetAttribute(key.Name) is null)
            {
                motorCloudEvent[key] = value;
            }
        }

        return motorCloudEvent;
    }
}
