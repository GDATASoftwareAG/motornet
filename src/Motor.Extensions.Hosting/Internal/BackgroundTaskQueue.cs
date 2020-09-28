using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Motor.Extensions.Diagnostics.Metrics.Abstractions;
using Motor.Extensions.Hosting.Abstractions;
using Prometheus.Client.Abstractions;

namespace Motor.Extensions.Hosting.Internal
{
    public class BackgroundTaskQueue<T> : IBackgroundTaskQueue<T>
    {
        private readonly IGauge? _elementsInQueue;
        private readonly SemaphoreSlim _signal = new SemaphoreSlim(0);
        private readonly ICounter? _totalMessages;
        private readonly ConcurrentQueue<QueueItem<T>> _workItems = new ConcurrentQueue<QueueItem<T>>();

        public BackgroundTaskQueue(IMetricsFactory<BackgroundTaskQueue<T>>? metricsFactory)
        {
            _elementsInQueue = metricsFactory?.CreateGauge("task_queue_enqueued_elements", "", "type")
                ?.WithLabels(typeof(T).Name);
            _totalMessages = metricsFactory?.CreateCounter("total_messages", "", "type")?.WithLabels(typeof(T).Name);
        }

        public Task<ProcessedMessageStatus> QueueBackgroundWorkItem(T item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            var taskCompletionStatus = new TaskCompletionSource<ProcessedMessageStatus>();

            _workItems.Enqueue(new QueueItem<T>(item, taskCompletionStatus));
            _elementsInQueue?.Inc();
            _totalMessages?.Inc();
            _signal.Release();
            ItemCount++;
            return taskCompletionStatus.Task;
        }

        public async Task<QueueItem<T>> DequeueAsync(CancellationToken cancellationToken)
        {
            await _signal.WaitAsync(cancellationToken).ConfigureAwait(false);
            _workItems.TryDequeue(out var workItem);
            _elementsInQueue?.Dec();
            LastDequeuedAt = DateTimeOffset.UtcNow;
            ItemCount--;
            return workItem;
        }

        public int ItemCount { get; private set; }
        public DateTimeOffset LastDequeuedAt { get; private set; }
    }
}