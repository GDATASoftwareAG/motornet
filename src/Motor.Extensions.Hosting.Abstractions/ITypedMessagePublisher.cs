using System.Threading;
using System.Threading.Tasks;

namespace Motor.Extensions.Hosting.Abstractions
{
    public interface ITypedMessagePublisher<TOutput>
        where TOutput : class
    {
        Task PublishMessageAsync(MotorCloudEvent<TOutput> cloudEvent, CancellationToken token = default);
    }
}
