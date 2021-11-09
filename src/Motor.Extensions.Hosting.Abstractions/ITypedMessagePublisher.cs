using System.Threading;
using System.Threading.Tasks;
using Motor.Extensions.Hosting.CloudEvents;

namespace Motor.Extensions.Hosting.Abstractions;

public interface ITypedMessagePublisher<TOutput>
    where TOutput : class
{
    Task PublishMessageAsync(MotorCloudEvent<TOutput> motorCloudEvent, CancellationToken token = default);
}
