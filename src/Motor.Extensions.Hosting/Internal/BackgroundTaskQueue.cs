using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Motor.Extensions.Diagnostics.Metrics.Abstractions;
using Motor.Extensions.Hosting.Abstractions;
using Prometheus.Client;

namespace Motor.Extensions.Hosting.Internal;

public class BackgroundTaskQueue<T> : IBackgroundTaskQueue<T>, IDisposable where T : notnull
{
    private readonly IGauge? _elementsInQueue;
    private readonly SemaphoreSlim _signal = new(0);
    private readonly ICounter? _totalMessages;
    private readonly ConcurrentQueue<QueueItem<T>> _workItems = new();

    public BackgroundTaskQueue(IMetricsFactory<BackgroundTaskQueue<T>>? metricsFactory)
    {
        _elementsInQueue = metricsFactory?.CreateGauge("task_queue_enqueued_elements", "", false, "type")
            ?.WithLabels(typeof(T).Name);
        _totalMessages = metricsFactory?.CreateCounter("total_messages", "", false, "type")?.WithLabels(typeof(T).Name);
        LastDequeuedAt = DateTimeOffset.UtcNow;
    }

    public Task<ProcessedMessageStatus> QueueBackgroundWorkItem(T item)
    {
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        var taskCompletionStatus = new TaskCompletionSource<ProcessedMessageStatus>();

        if (_workItems.IsEmpty)
        {
            // Simulate recent dequeue to avoid HealthCheck triggering immediately
            LastDequeuedAt = DateTimeOffset.UtcNow;
        }
        _workItems.Enqueue(new QueueItem<T>(item, taskCompletionStatus));
        _elementsInQueue?.Inc();
        _totalMessages?.Inc();
        _signal.Release();
        ItemCount++;
        return taskCompletionStatus.Task;
    }

    public async Task<QueueItem<T>?> DequeueAsync(CancellationToken token)
    {
        await _signal.WaitAsync(token).ConfigureAwait(false);
        _workItems.TryDequeue(out var workItem);
        _elementsInQueue?.Dec();
        LastDequeuedAt = DateTimeOffset.UtcNow;
        ItemCount--;
        return workItem;
    }

    public int ItemCount { get; private set; }
    public DateTimeOffset LastDequeuedAt { get; private set; }

    public void Dispose()
    {
        _signal.Dispose();
    }
}
