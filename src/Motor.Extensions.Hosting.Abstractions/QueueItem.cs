using System.Threading.Tasks;

namespace Motor.Extensions.Hosting.Abstractions
{
    public class QueueItem<T> {
        public T Item { get; }
        public TaskCompletionSource<ProcessedMessageStatus> TaskCompletionStatus { get; }
        public QueueItem(T item, TaskCompletionSource<ProcessedMessageStatus> taskCompletionStatus)
        {
            Item = item;
            TaskCompletionStatus = taskCompletionStatus;
        }
    }
}
