using System.Threading.Tasks;

namespace Motor.Extensions.Diagnostics.Queue.Abstractions;

public record QueueState(string QueueName, long ReadyMessages, long ConsumerCount);

public interface IQueueMonitor
{
    Task<QueueState> GetCurrentState();
}
