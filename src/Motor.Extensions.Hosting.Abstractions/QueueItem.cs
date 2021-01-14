using System.Threading.Tasks;

namespace Motor.Extensions.Hosting.Abstractions
{
    public record QueueItem<T> where T : notnull
    {
        public QueueItem(T item, TaskCompletionSource<ProcessedMessageStatus> taskCompletionStatus)
        {
            Item = item;
            TaskCompletionStatus = taskCompletionStatus;
        }

        public T Item { get; }
        public TaskCompletionSource<ProcessedMessageStatus> TaskCompletionStatus { get; }
    }
}
