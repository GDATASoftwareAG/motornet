using System;
using System.Threading;
using System.Threading.Tasks;

namespace Motor.Extensions.Hosting.Abstractions;

public interface IBackgroundTaskQueue<T>
    where T : notnull
{
    int ItemCount { get; }
    DateTimeOffset LastDequeuedAt { get; }
    Task<ProcessedMessageStatus> QueueBackgroundWorkItem(T item);

    Task<QueueItem<T>?> DequeueAsync(CancellationToken token);
}
