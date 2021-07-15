using System.Threading;
using System.Threading.Tasks;
using Motor.Extensions.Hosting.CloudEvents;

namespace Motor.Extensions.Hosting.Abstractions
{
    public interface ISingleOutputService<TInput, TOutput>
        where TInput : class
        where TOutput : class
    {
        Task<MotorCloudEvent<TOutput>?> ConvertMessageAsync(MotorCloudEvent<TInput> dataCloudEvent,
            CancellationToken token = default);
    }
}
