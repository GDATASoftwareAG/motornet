using System;
using System.Threading;
using System.Threading.Tasks;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;

namespace Motor.Extensions.Hosting.PgMq;

public sealed class PgMqMessageConsumer<TData> : IMessageConsumer<TData>
    where TData : notnull
{
    public Func<MotorCloudEvent<byte[]>, CancellationToken, Task<ProcessedMessageStatus>>? ConsumeCallbackAsync
    {
        get;
        set;
    }
    
    /// <summary>
    /// Starts the <see cref="PgMqMessageConsumer{TOutput}"/>.
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
    public Task StartAsync(CancellationToken token = default)
    {
        throw new NotImplementedException();
    }
    /// <summary>
    /// Executes the <see cref="PgMqMessageConsumer{TOutput}"/>.
    /// </summary>
    /// <param name="token">A cancellation token.</param>
    /// <remarks>
    /// <list type="bullet">
    /// <item>Consumes messages from the queue <see cref="NpgmqClient"/>.</item>
    /// <item>For each message calls ConsumeCallbackAsync </item>
    /// <item>Processes message sequentially.</item>
    /// <item>Uses long polling.</item>
    /// <item>if ConsumeCallbackAsync returns ProcessedMessageStatus.Success, ack message.</item>
    /// <item>if exception is thrown or ConsumeCallbackAsync doesn't return ProcessedMessageStatus.Success shutdown.</item>
    /// </list>
    /// </remarks>
    async Task ExecuteAsync(CancellationToken token = default)
    {
        try
        {
            throw new NotImplementedException();
        } catch (Exception e)
        {
            _logger.LogCritical(
                LogEvents.MessageHandlingUnexpectedException,
                e,
                "Unexpected exception in message handling"
            );
            _applicationLifetime.StopApplication();
        }
    }

    public Task StopAsync(CancellationToken token = default)
    {
        throw new NotImplementedException();
    }
}
