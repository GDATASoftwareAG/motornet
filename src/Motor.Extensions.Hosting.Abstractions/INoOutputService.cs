using System.Threading;
using System.Threading.Tasks;
using Motor.Extensions.Hosting.CloudEvents;

namespace Motor.Extensions.Hosting.Abstractions;

public interface INoOutputService<TInput>
    where TInput : class
{
    Task<ProcessedMessageStatus>
        HandleMessageAsync(MotorCloudEvent<TInput> dataCloudEvent, CancellationToken token = default);
}
