using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using CloudNative.CloudEvents;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Motor.Extensions.Diagnostics.Metrics.Abstractions;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using Motor.Extensions.Hosting.Kafka.Options;
using Polly;
using Prometheus.Client;

namespace Motor.Extensions.Hosting.Kafka;

public record ConsumeResultAndProcessedMessageStatus(ConsumeResult<string?, byte[]> ConsumeResult,
    ProcessedMessageStatus ProcessedMessageStatus);

public sealed class KafkaMessageConsumer<TData> : IMessageConsumer<TData>, IDisposable where TData : notnull
{
    private readonly IApplicationNameService _applicationNameService;
    private readonly CloudEventFormatter _cloudEventFormatter;
    private readonly KafkaConsumerOptions<TData> _options;
    private readonly IMetricFamily<IGauge>? _consumerLagGauge;
    private readonly IMetricFamily<ISummary>? _consumerLagSummary;
    private readonly ILogger<KafkaMessageConsumer<TData>> _logger;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private IConsumer<string?, byte[]>? _consumer;

    public KafkaMessageConsumer(ILogger<KafkaMessageConsumer<TData>> logger,
        IOptions<KafkaConsumerOptions<TData>> config,
        IHostApplicationLifetime applicationLifetime,
        IMetricsFactory<KafkaMessageConsumer<TData>>? metricsFactory,
        IApplicationNameService applicationNameService,
        CloudEventFormatter cloudEventFormatter)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _applicationLifetime = applicationLifetime;
        _applicationNameService = applicationNameService ?? throw new ArgumentNullException(nameof(applicationNameService));
        _cloudEventFormatter = cloudEventFormatter;
        _options = config.Value ?? throw new ArgumentNullException(nameof(config));
        _consumerLagSummary = metricsFactory?.CreateSummary("consumer_lag_distribution",
            "Contains a summary of current consumer lag of each partition", new[] { "topic", "partition" });
        _consumerLagGauge = metricsFactory?.CreateGauge("consumer_lag",
            "Contains current number consumer lag of each partition", false, "topic", "partition");

        _processedMessages = Channel.CreateBounded<Task<ConsumeResultAndProcessedMessageStatus>>(_options.MaxConcurrentMessages);
        _timer = new Timer(HandleCommitTimer);
    }

    public Func<MotorCloudEvent<byte[]>, CancellationToken, Task<ProcessedMessageStatus>>? ConsumeCallbackAsync
    {
        get;
        set;
    }

    public Task StartAsync(CancellationToken token = default)
    {
        if (ConsumeCallbackAsync is null)
        {
            throw new InvalidOperationException("ConsumeCallback is null");
        }

        var consumerBuilder = new ConsumerBuilder<string?, byte[]>(_options)
            .SetLogHandler((_, logMessage) => WriteLog(logMessage))
            .SetStatisticsHandler((_, json) => WriteStatistics(json));

        _consumer = consumerBuilder.Build();
        _consumer.Subscribe(_options.Topic);
        return Task.CompletedTask;
    }

    public async Task ExecuteAsync(CancellationToken token = default)
    {
        await Task.Run(async () =>
        {
            var committer = ExecuteCommitLoopAsync(token);

            try
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        if (!await _processedMessages.Writer.WaitToWriteAsync(token))
                        {
                            break;
                        }

                        var msg = _consumer?.Consume(token);
                        if (msg is { IsPartitionEOF: false })
                        {
                            await _processedMessages.Writer.WriteAsync(SingleMessageHandlingAsync(msg, token), token);
                        }
                        else
                        {
                            _logger.LogDebug(LogEvents.NoMessageReceived, "No messages received");
                        }
                    }
                    catch (Exception e) when (e is not OperationCanceledException or ChannelClosedException)
                    {
                        _logger.LogError(LogEvents.MessageReceivedFailure, e, "Failed to receive message.");
                    }
                }

                await committer;
            }
            catch (Exception e) when (e is OperationCanceledException or ChannelClosedException)
            {
                // Execution was cancelled
            }

            Commit();
            _logger.LogInformation(LogEvents.TerminatingKafkaListener, "Terminating Kafka listener...");
        }, token).ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken token = default)
    {
        _consumer?.Close();
        return Task.CompletedTask;
    }

    private void WriteLog(LogMessage logMessage)
    {
        switch (logMessage.Level)
        {
            case SyslogLevel.Emergency:
            case SyslogLevel.Alert:
            case SyslogLevel.Critical:
                _logger.LogCritical($"{logMessage.Message} -(Facility: {{facility}}, Name: {{name}})",
                    logMessage.Facility, logMessage.Name);
                break;
            case SyslogLevel.Error:
                _logger.LogError($"{logMessage.Message} -(Facility: {{facility}}, Name: {{name}})",
                    logMessage.Facility, logMessage.Name);
                break;
            case SyslogLevel.Warning:
                _logger.LogWarning($"{logMessage.Message} -(Facility: {{facility}}, Name: {{name}})",
                    logMessage.Facility, logMessage.Name);
                break;
            case SyslogLevel.Notice:
            case SyslogLevel.Info:
                _logger.LogInformation($"{logMessage.Message} -(Facility: {{facility}}, Name: {{name}})",
                    logMessage.Facility, logMessage.Name);
                break;
            case SyslogLevel.Debug:
                _logger.LogDebug($"{logMessage.Message} -(Facility: {{facility}}, Name: {{name}})",
                    logMessage.Facility, logMessage.Name);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(logMessage.Level));
        }
    }

    private void WriteStatistics(string json)
    {
        var partitionConsumerLags = JsonSerializer
            .Deserialize<KafkaStatistics>(json)?
            .Topics?
            .Select(t => t.Value)
            .SelectMany(t => t.Partitions ?? new Dictionary<string, KafkaStatisticsPartition>())
            .Select(t => (Parition: t.Key.ToString(), t.Value.ConsumerLag));
        if (partitionConsumerLags is null)
        {
            return;
        }

        foreach (var (partition, consumerLag) in partitionConsumerLags)
        {
            var lag = consumerLag;
            if (lag == -1)
            {
                lag = 0;
            }

            _consumerLagSummary?.WithLabels(_options.Topic, partition)?.Observe(lag);
            _consumerLagGauge?.WithLabels(_options.Topic, partition)?.Set(lag);
        }
    }

    private async Task<ConsumeResultAndProcessedMessageStatus> SingleMessageHandlingAsync(ConsumeResult<string?, byte[]> msg, CancellationToken token)
    {
        try
        {
            _logger.LogDebug(LogEvents.ReceivedMessage,
                "Received message from topic '{Topic}:{Partition}' with offset: '{Offset}[{TopicPartitionOffset}]'",
                msg.Topic, msg.Partition, msg.Offset, msg.TopicPartitionOffset);
            var cloudEvent = KafkaMessageToCloudEvent(msg.Message);

            var retryPolicy = Policy
                .HandleResult<ProcessedMessageStatus>(status => status == ProcessedMessageStatus.TemporaryFailure)
                .WaitAndRetryAsync(_options.RetriesOnTemporaryFailure,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
            var status = await retryPolicy.ExecuteAsync(
                (cancellationToken) => ConsumeCallbackAsync!.Invoke(cloudEvent, cancellationToken), token);
            return new ConsumeResultAndProcessedMessageStatus(msg, status);
        }
        catch (Exception e)
        {
            _logger.LogCritical(LogEvents.MessageHandlingUnexpectedException, e,
                "Unexpected exception in message handling");
            _applicationLifetime.StopApplication();
        }

        return new ConsumeResultAndProcessedMessageStatus(msg, ProcessedMessageStatus.CriticalFailure);
    }

    #region Commit

    private readonly Channel<Task<ConsumeResultAndProcessedMessageStatus>> _processedMessages;
    private readonly Timer _timer;
    private readonly object _commitLock = new();
    private ConsumeResultAndProcessedMessageStatus? _lastConsumeResultAndProcessedMessageStatus;

    private async Task ExecuteCommitLoopAsync(CancellationToken cancellationToken)
    {
        RestartCommitTimer();

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = await PeekAndAwaitProcessedMessages(cancellationToken);

                if (IsIrrecoverableFailure(result.ProcessedMessageStatus))
                {
                    _applicationLifetime.StopApplication();
                    break;
                }

                // Remove message from channel, when Task is successfully completed
                await _processedMessages.Reader.ReadAsync(cancellationToken);

                lock (_commitLock)
                {
                    _lastConsumeResultAndProcessedMessageStatus = result;
                }

                if ((result.ConsumeResult.Offset.Value + 1) % _options.CommitPeriod == 0)
                {
                    Commit();
                    RestartCommitTimer();
                }
            }
            catch (Exception e) when (e is OperationCanceledException or ChannelClosedException)
            {
                break;
            }
        }

        StopCommitTimer();
    }

    private async Task<ConsumeResultAndProcessedMessageStatus> PeekAndAwaitProcessedMessages(CancellationToken cancellationToken)
    {
        await _processedMessages.Reader.WaitToReadAsync(cancellationToken);

        if (!_processedMessages.Reader.TryPeek(out var consumeAndProcessTask))
        {
            throw new InvalidOperationException("Awaited channel data has been removed by another consumer");
        }

        return await consumeAndProcessTask;
    }

    private void Commit()
    {
        lock (_commitLock)
        {
            if (_lastConsumeResultAndProcessedMessageStatus == null)
            {
                return;
            }

            try
            {
                _consumer?.Commit(_lastConsumeResultAndProcessedMessageStatus.ConsumeResult);
                _lastConsumeResultAndProcessedMessageStatus = null;
            }
            catch (KafkaException e)
            {
                _logger.LogError(LogEvents.CommitError, e, "Commit error: {Reason}", e.Error.Reason);
            }
        }
    }

    private void RestartCommitTimer()
    {
        var autoCommitIntervalMs = _options.AutoCommitIntervalMs;
        if (autoCommitIntervalMs != null)
        {
            _timer.Change(autoCommitIntervalMs.Value, Timeout.Infinite);
        }
    }

    private void StopCommitTimer()
    {
        _timer.Change(Timeout.Infinite, Timeout.Infinite);
    }


    private void HandleCommitTimer(object? state)
    {
        Commit();
        RestartCommitTimer();
    }

    private bool IsIrrecoverableFailure(ProcessedMessageStatus status)
    {
        switch (status)
        {
            case ProcessedMessageStatus.Success:
            case ProcessedMessageStatus.InvalidInput:
            case ProcessedMessageStatus.Failure:
                return false;
            case ProcessedMessageStatus.TemporaryFailure:
                _logger.LogCritical(LogEvents.FailureDespiteRetrying,
                    "Message consume fails despite retrying");
                return true;
            case ProcessedMessageStatus.CriticalFailure:
                _logger.LogCritical(LogEvents.CriticalFailureOnConsume,
                    "Message consume fails with critical failure");
                return true;
            default:
                _logger.LogCritical(LogEvents.UnknownProcessedMessageStatus, "Unknown processed message status {status}", status);
                return true;
        }
    }

    #endregion

    public MotorCloudEvent<byte[]> KafkaMessageToCloudEvent(Message<string?, byte[]> msg)
    {
        return msg.ToMotorCloudEvent(_applicationNameService, _cloudEventFormatter);
    }

    /// <remarks>
    /// For testing.
    /// </remarks>
    public IEnumerable<TopicPartitionOffset> Committed()
    {
        if (_consumer == null)
        {
            throw new InvalidOperationException("Consumer is not initialized");
        }

        return _consumer.Committed(TimeSpan.FromSeconds(10));
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
            _consumer?.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
    }
}
