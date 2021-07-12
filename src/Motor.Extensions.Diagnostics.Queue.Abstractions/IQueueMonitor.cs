using System.Threading.Tasks;

namespace Motor.Extensions.Diagnostics.Queue.Abstractions
{
    public record QueueState(string QueueName, long ReadyMessages);

    public interface IQueueMonitor
    {
        Task<QueueState> GetCurrentState();
    }
}
