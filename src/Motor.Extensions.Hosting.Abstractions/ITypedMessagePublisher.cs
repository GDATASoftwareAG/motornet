using System.Threading;
using System.Threading.Tasks;
using Motor.Extensions.Hosting.CloudEvents;

namespace Motor.Extensions.Hosting.Abstractions;

public interface ITypedMessagePublisher<TOutput>
    where TOutput : notnull
{
    Task PublishMessageAsync(MotorCloudEvent<TOutput> motorCloudEvent, CancellationToken token = default);

    Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
