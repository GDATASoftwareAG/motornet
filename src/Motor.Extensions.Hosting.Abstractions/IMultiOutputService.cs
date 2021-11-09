using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Motor.Extensions.Hosting.CloudEvents;

namespace Motor.Extensions.Hosting.Abstractions;

public interface IMultiOutputService<TInput, TOutput>
    where TInput : class
    where TOutput : class
{
    IAsyncEnumerable<MotorCloudEvent<TOutput>> ConvertMessageAsync(MotorCloudEvent<TInput> dataCloudEvent,
        CancellationToken token = default);
}
