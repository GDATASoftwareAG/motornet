using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using CloudNative.CloudEvents;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using Polly;

namespace Motor.Extensions.Hosting.Kafka;

/// <summary>
/// Represents the processing state for a single Kafka partition.
/// Each partition gets its own bounded input channel and processes
/// messages sequentially — one at a time. Results are written to
/// an output channel for the commit loop to consume.
/// </summary>
public sealed class PartitionProcessor
{
    public TopicPartition TopicPartition { get; }

    /// <summary>
    /// Inbound channel: raw consumed messages waiting to be processed.
    /// </summary>
    public Channel<ConsumeResult<string?, byte[]>> InputChannel { get; }

    /// <summary>
    /// Outbound channel: processed results ready for offset commit.
    /// </summary>
    public Channel<ConsumeResultAndProcessedMessageStatus> OutputChannel { get; }

    public int Capacity { get; }

    /// <summary>
    /// Returns true if the input channel has space for at least one more item.
    /// </summary>
    public bool HasCapacity => InputChannel.Reader.Count < Capacity;

    /// <summary>
    /// The background task running the sequential processing loop.
    /// </summary>
    public Task ProcessingTask { get; }

    public PartitionProcessor(
        TopicPartition topicPartition,
        int capacity,
        Func<MotorCloudEvent<byte[]>, CancellationToken, Task<ProcessedMessageStatus>> consumeCallbackAsync,
        IApplicationNameService applicationNameService,
        CloudEventFormatter cloudEventFormatter,
        AsyncPolicy<ProcessedMessageStatus> retryPolicy,
        IHostApplicationLifetime applicationLifetime,
        ILogger logger,
        CancellationToken cancellationToken
    )
    {
        TopicPartition = topicPartition;
        Capacity = capacity;
        InputChannel = Channel.CreateBounded<ConsumeResult<string?, byte[]>>(capacity);
        OutputChannel = Channel.CreateBounded<ConsumeResultAndProcessedMessageStatus>(capacity);

        ProcessingTask = Task.Run(
            () =>
                ProcessMessagesAsync(
                    consumeCallbackAsync,
                    applicationNameService,
                    cloudEventFormatter,
                    retryPolicy,
                    applicationLifetime,
                    logger,
                    cancellationToken
                ),
            cancellationToken
        );
    }

    private async Task ProcessMessagesAsync(
        Func<MotorCloudEvent<byte[]>, CancellationToken, Task<ProcessedMessageStatus>> consumeCallbackAsync,
        IApplicationNameService applicationNameService,
        CloudEventFormatter cloudEventFormatter,
        AsyncPolicy<ProcessedMessageStatus> retryPolicy,
        IHostApplicationLifetime applicationLifetime,
        ILogger logger,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await foreach (var msg in InputChannel.Reader.ReadAllAsync(cancellationToken))
            {
                var result = await HandleSingleMessageAsync(
                    msg,
                    consumeCallbackAsync,
                    applicationNameService,
                    cloudEventFormatter,
                    retryPolicy,
                    applicationLifetime,
                    logger,
                    cancellationToken
                );

                // Write the result to the output channel for the commit loop.
                // This will block if the output channel is full, providing backpressure.
                await OutputChannel.Writer.WriteAsync(result, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Processing was cancelled
        }
        catch (ChannelClosedException)
        {
            // Input channel was completed (partition revoked)
        }
        finally
        {
            OutputChannel.Writer.TryComplete();
        }
    }

    private static async Task<ConsumeResultAndProcessedMessageStatus> HandleSingleMessageAsync(
        ConsumeResult<string?, byte[]> msg,
        Func<MotorCloudEvent<byte[]>, CancellationToken, Task<ProcessedMessageStatus>> consumeCallbackAsync,
        IApplicationNameService applicationNameService,
        CloudEventFormatter cloudEventFormatter,
        AsyncPolicy<ProcessedMessageStatus> retryPolicy,
        IHostApplicationLifetime applicationLifetime,
        ILogger logger,
        CancellationToken token
    )
    {
        try
        {
            logger.LogDebug(
                LogEvents.ReceivedMessage,
                "Received message from topic '{Topic}:{Partition}' with offset: '{Offset}[{TopicPartitionOffset}]'",
                msg.Topic,
                msg.Partition,
                msg.Offset,
                msg.TopicPartitionOffset
            );
            var cloudEvent = msg.Message.ToMotorCloudEvent(applicationNameService, cloudEventFormatter);

            var status = await retryPolicy.ExecuteAsync(
                cancellationToken => consumeCallbackAsync.Invoke(cloudEvent, cancellationToken),
                token
            );
            return new ConsumeResultAndProcessedMessageStatus(msg, status);
        }
        catch (Exception e)
        {
            logger.LogCritical(
                LogEvents.MessageHandlingUnexpectedException,
                e,
                "Unexpected exception in message handling"
            );
            applicationLifetime.StopApplication();
        }

        return new ConsumeResultAndProcessedMessageStatus(msg, ProcessedMessageStatus.CriticalFailure);
    }
}
