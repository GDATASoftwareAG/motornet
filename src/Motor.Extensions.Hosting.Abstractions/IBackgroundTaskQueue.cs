using System;
using System.Threading;
using System.Threading.Tasks;

namespace Motor.Extensions.Hosting.Abstractions
{
    public interface IBackgroundTaskQueue<T>
    {
        Task<ProcessedMessageStatus> QueueBackgroundWorkItem(T item);

        Task<QueueItem<T>> DequeueAsync(CancellationToken cancellationToken);

        int ItemCount { get; }
        DateTimeOffset LastDequeuedAt { get; }
    }
}
