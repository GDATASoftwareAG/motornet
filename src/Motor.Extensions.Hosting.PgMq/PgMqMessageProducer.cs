using System.Threading;
using System.Threading.Tasks;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;

namespace Motor.Extensions.Hosting.PgMq;

public class PgMqMessageProducer<TOutput> : IRawMessagePublisher<TOutput>
    where TOutput : notnull
{
    public Task PublishMessageAsync(MotorCloudEvent<byte[]> motorCloudEvent, CancellationToken token = default)
    {
        throw new System.NotImplementedException();
    }
}
