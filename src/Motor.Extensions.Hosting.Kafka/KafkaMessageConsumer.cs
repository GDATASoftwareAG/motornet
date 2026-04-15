using System;
using System.Collections.Concurrent;
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

public record ConsumeResultAndProcessedMessageStatus(
    ConsumeResult<string?, byte[]> ConsumeResult,
    ProcessedMessageStatus ProcessedMessageStatus
);

public sealed class KafkaMessageConsumer<TData> : IMessageConsumer<TData>, IDisposable
    where TData : notnull
{
    private readonly IApplicationNameService _applicationNameService;
    private readonly CloudEventFormatter _cloudEventFormatter;
    private readonly KafkaConsumerOptions<TData> _options;
    private readonly IMetricFamily<IGauge>? _consumerLagGauge;
    private readonly IMetricFamily<ISummary>? _consumerLagSummary;
    private readonly ILogger<KafkaMessageConsumer<TData>> _logger;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly AsyncPolicy<ProcessedMessageStatus> _retryPolicy;
    private IConsumer<string?, byte[]>? _consumer;
    private readonly CancellationTokenSource _internalCts = new();

    // Per-partition processing state
    private readonly ConcurrentDictionary<TopicPartition, PartitionProcessor> _partitionProcessors = new();
    private readonly object _pauseLock = new();
    private readonly HashSet<TopicPartition> _pausedPartitions = new();

    public KafkaMessageConsumer(
        ILogger<KafkaMessageConsumer<TData>> logger,
        IOptions<KafkaConsumerOptions<TData>> config,
        IHostApplicationLifetime applicationLifetime,
        IMetricsFactory<KafkaMessageConsumer<TData>>? metricsFactory,
        IApplicationNameService applicationNameService,
        CloudEventFormatter cloudEventFormatter
    )
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(config.Value);
        ArgumentNullException.ThrowIfNull(applicationNameService);
        ArgumentNullException.ThrowIfNull(cloudEventFormatter);

        _logger = logger;
        _applicationLifetime = applicationLifetime;
        _applicationNameService = applicationNameService;
        _cloudEventFormatter = cloudEventFormatter;
        _options = config.Value;

        _consumerLagSummary = metricsFactory?.CreateSummary(
            "consumer_lag_distribution",
            "Contains a summary of current consumer lag of each partition",
            new[] { "topic", "partition" }
        );
        _consumerLagGauge = metricsFactory?.CreateGauge(
            "consumer_lag",
            "Contains current number consumer lag of each partition",
            false,
            "topic",
            "partition"
        );

        _timer = new Timer(HandleCommitTimer);

        _retryPolicy = Policy
            .HandleResult<ProcessedMessageStatus>(status => status == ProcessedMessageStatus.TemporaryFailure)
            .WaitAndRetryAsync(
                _options.RetriesOnTemporaryFailure,
                retryAttempt => _options.RetryBasePeriod * Math.Pow(2, retryAttempt)
            );
    }

    public Func<
        MotorCloudEvent<byte[]>,
        CancellationToken,
        Task<ProcessedMessageStatus>
    >? ConsumeCallbackAsync { get; set; }

    public Task StartAsync(CancellationToken token = default)
    {
        if (ConsumeCallbackAsync is null)
        {
            throw new InvalidOperationException("ConsumeCallback is null");
        }

        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_internalCts.Token, token);

        var consumerBuilder = new ConsumerBuilder<string?, byte[]>(_options)
            .SetLogHandler((_, logMessage) => WriteLog(logMessage))
            .SetStatisticsHandler((_, json) => WriteStatistics(json))
            .SetPartitionsAssignedHandler(
                (_, partitions) =>
                {
                    foreach (var tp in partitions)
                    {
                        var processor = new PartitionProcessor(
                            tp,
                            _options.MaxConcurrentMessagesPerPartition,
                            ConsumeCallbackAsync,
                            _applicationNameService,
                            _cloudEventFormatter,
                            _retryPolicy,
                            _applicationLifetime,
                            _logger,
                            linkedCts.Token
                        );
                        _partitionProcessors[tp] = processor;
                        _logger.LogInformation(LogEvents.PartitionAssigned, "Partition assigned: {TopicPartition}", tp);
                    }
                }
            )
            .SetPartitionsRevokedHandler(
                (consumer, partitions) =>
                {
                    foreach (var tp in partitions.Select(tpo => tpo.TopicPartition))
                    {
                        if (_partitionProcessors.TryRemove(tp, out var processor))
                        {
                            DrainPartitionProcessor(processor);
                            _logger.LogInformation(
                                LogEvents.PartitionRevoked,
                                "Partition revoked: {TopicPartition}",
                                tp
                            );
                        }

                        lock (_pauseLock)
                        {
                            _pausedPartitions.Remove(tp);
                        }
                    }

                    Commit();
                }
            );

        _consumer = consumerBuilder.Build();
        _consumer.Subscribe(_options.Topic);
        return Task.CompletedTask;
    }

    public async Task ExecuteAsync(CancellationToken token = default)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(_internalCts.Token, token);
        await Task.Run(
                async () =>
                {
                    var committer = ExecuteCommitLoopAsync(cts.Token);

                    try
                    {
                        while (!cts.Token.IsCancellationRequested)
                        {
                            try
                            {
                                ResumePartitionsWithCapacity();

                                var msg = _consumer?.Consume(TimeSpan.FromMilliseconds(100));
                                if (msg is null || msg.IsPartitionEOF)
                                {
                                    _logger.LogDebug(LogEvents.NoMessageReceived, "No messages received");
                                    continue;
                                }

                                var tp = msg.TopicPartition;

                                if (!_partitionProcessors.TryGetValue(tp, out var processor))
                                {
                                    // Partition not assigned (race condition during rebalance), skip
                                    _logger.LogWarning(
                                        "Received message for unassigned partition {TopicPartition}, skipping",
                                        tp
                                    );
                                    continue;
                                }

                                // Try to write the raw message into the partition's input channel.
                                // The PartitionProcessor will process it sequentially.
                                if (processor.HasCapacity)
                                {
                                    processor.InputChannel.Writer.TryWrite(msg);
                                }
                                else
                                {
                                    // Channel is full — pause this partition and wait for space.
                                    PausePartition(tp);

                                    if (await processor.InputChannel.Writer.WaitToWriteAsync(cts.Token))
                                    {
                                        processor.InputChannel.Writer.TryWrite(msg);
                                    }
                                }
                            }
                            catch (ChannelClosedException)
                            {
                                // Channel was closed due to partition revocation, skip and continue
                            }
                            catch (Exception e) when (e is not OperationCanceledException)
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
                },
                cts.Token
            )
            .ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken token = default)
    {
        CloseOrDispose();
        return Task.CompletedTask;
    }

    // PausePartition is implemented in the client, it tells the partition fetcher to stop fetching an otherwise active partition.
    // https://github.com/confluentinc/librdkafka/issues/1849#issuecomment-397763904
    private void PausePartition(TopicPartition tp)
    {
        lock (_pauseLock)
        {
            if (_pausedPartitions.Add(tp))
            {
                try
                {
                    _consumer?.Pause(new[] { tp });
                    _logger.LogDebug(LogEvents.PartitionPaused, "Paused partition {TopicPartition}", tp);
                }
                catch (KafkaException)
                {
                    // Partition may have been revoked
                    _pausedPartitions.Remove(tp);
                }
            }
        }
    }

    // ResumePartition is implemented in the client, it tells the partition fetcher to stop fetching an otherwise active partition.
    // https://github.com/confluentinc/librdkafka/issues/1849#issuecomment-397763904
    private void ResumePartitionsWithCapacity()
    {
        lock (_pauseLock)
        {
            if (_pausedPartitions.Count == 0)
            {
                return;
            }

            var toResume = new List<TopicPartition>();
            foreach (var tp in _pausedPartitions)
            {
                if (
                    _partitionProcessors.TryGetValue(tp, out var processor)
                    && processor.InputChannel.Reader.Count < processor.Capacity
                )
                {
                    toResume.Add(tp);
                }
            }

            foreach (var tp in toResume)
            {
                try
                {
                    _consumer?.Resume(new[] { tp });
                    _pausedPartitions.Remove(tp);
                    _logger.LogDebug(LogEvents.PartitionResumed, "Resumed partition {TopicPartition}", tp);
                }
                catch (KafkaException)
                {
                    _pausedPartitions.Remove(tp);
                }
            }
        }
    }

    private void DrainPartitionProcessor(PartitionProcessor processor)
    {
        // Complete the input channel so the processing loop finishes
        processor.InputChannel.Writer.TryComplete();

        // Read any already-completed results from the output channel
        while (processor.OutputChannel.Reader.TryRead(out var result))
        {
            try
            {
                _consumer?.StoreOffset(result.ConsumeResult);
            }
            catch
            {
                // Best effort drain
            }
        }
    }

    private void WriteLog(LogMessage logMessage)
    {
        switch (logMessage.Level)
        {
            case SyslogLevel.Emergency:
            case SyslogLevel.Alert:
            case SyslogLevel.Critical:
                _logger.LogCritical(
                    "{Message} -(Facility: {Facility}, Name: {Name})",
                    logMessage.Message,
                    logMessage.Facility,
                    logMessage.Name
                );
                break;
            case SyslogLevel.Error:
                _logger.LogError(
                    "{Message} -(Facility: {Facility}, Name: {Name})",
                    logMessage.Message,
                    logMessage.Facility,
                    logMessage.Name
                );
                break;
            case SyslogLevel.Warning:
                _logger.LogWarning(
                    "{Message} -(Facility: {Facility}, Name: {Name})",
                    logMessage.Message,
                    logMessage.Facility,
                    logMessage.Name
                );
                break;
            case SyslogLevel.Notice:
            case SyslogLevel.Info:
                _logger.LogInformation(
                    "{Message} -(Facility: {Facility}, Name: {Name})",
                    logMessage.Message,
                    logMessage.Facility,
                    logMessage.Name
                );
                break;
            case SyslogLevel.Debug:
                _logger.LogDebug(
                    "{Message} -(Facility: {Facility}, Name: {Name})",
                    logMessage.Message,
                    logMessage.Facility,
                    logMessage.Name
                );
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(logMessage.Level));
        }
    }

    private void WriteStatistics(string json)
    {
        var partitionConsumerLags = JsonSerializer
            .Deserialize<KafkaStatistics>(json)
            ?.Topics?.Select(t => t.Value)
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

    #region Commit

    private readonly Timer _timer;
    private readonly object _commitLock = new();
    private bool _pendingCommit;
    private int _messagesSinceLastCommit;

    private async Task ExecuteCommitLoopAsync(CancellationToken cancellationToken)
    {
        RestartCommitTimer();

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Poll all partition processors in a round-robin fashion
                var didWork = false;

                foreach (var kvp in _partitionProcessors)
                {
                    var outputChannel = kvp.Value.OutputChannel;

                    // Try to read the next completed result from this partition's output channel
                    if (!outputChannel.Reader.TryRead(out var result))
                    {
                        continue;
                    }

                    didWork = true;

                    if (IsIrrecoverableFailure(result.ProcessedMessageStatus))
                    {
                        await _internalCts.CancelAsync();
                        _applicationLifetime.StopApplication();
                        return;
                    }

                    lock (_commitLock)
                    {
                        _consumer?.StoreOffset(result.ConsumeResult);
                        _pendingCommit = true;
                        _messagesSinceLastCommit++;
                    }

                    // Use message count since last commit instead of offset-based check.
                    // This works correctly across multiple partitions with non-contiguous offsets.
                    if (_messagesSinceLastCommit >= _options.CommitPeriod)
                    {
                        Commit();
                        RestartCommitTimer();
                    }
                }

                if (!didWork)
                {
                    // No partition had a completed result; wait for any partition to produce one
                    await WaitForAnyPartitionCompletion(cancellationToken);
                }
            }
            catch (Exception e) when (e is OperationCanceledException or ChannelClosedException)
            {
                break;
            }
        }

        StopCommitTimer();
    }

    private async Task WaitForAnyPartitionCompletion(CancellationToken cancellationToken)
    {
        var waitTasks = new List<Task>();

        foreach (var kvp in _partitionProcessors)
        {
            var outputChannel = kvp.Value.OutputChannel;
            waitTasks.Add(outputChannel.Reader.WaitToReadAsync(cancellationToken).AsTask());
        }

        if (waitTasks.Count == 0)
        {
            // No partitions assigned yet, just wait briefly
            await Task.Delay(10, cancellationToken);
            return;
        }

        await Task.WhenAny(waitTasks);
    }

    private void Commit()
    {
        lock (_commitLock)
        {
            if (!_pendingCommit)
            {
                return;
            }

            _pendingCommit = false;
            _messagesSinceLastCommit = 0;
            try
            {
                _consumer?.Commit();
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
                _logger.LogCritical(LogEvents.FailureDespiteRetrying, "Message consume fails despite retrying");
                return true;
            case ProcessedMessageStatus.CriticalFailure:
                _logger.LogCritical(LogEvents.CriticalFailureOnConsume, "Message consume fails with critical failure");
                return true;
            default:
                _logger.LogCritical(
                    LogEvents.UnknownProcessedMessageStatus,
                    "Unknown processed message status {Status}",
                    status
                );
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
            CloseOrDispose();
        }
    }

    private void CloseOrDispose()
    {
        // Cancel the internal CTS first so that any in-flight message handlers
        // are cancelled before Close() triggers partition revocation (which drains channels).
        try
        {
            _internalCts.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // CTS already disposed
        }

        try
        {
            _consumer?.Close();
        }
        catch (ObjectDisposedException)
        {
            // thrown if the consumer is already closed/disposed
        }
        finally
        {
            _consumer?.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        _internalCts.Dispose();
    }
}
