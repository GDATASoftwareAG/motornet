using System.Threading;
using System.Threading.Tasks;
using Motor.Extensions.Hosting.CloudEvents;

namespace Motor.Extensions.Hosting.Abstractions;

public interface IRawMessagePublisher<TOutput>
    where TOutput : notnull
{
    Task PublishMessageAsync(MotorCloudEvent<byte[]> motorCloudEvent, CancellationToken token = default);
}
