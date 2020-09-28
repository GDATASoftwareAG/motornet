using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Motor.Extensions.Hosting.Abstractions
{
    public interface IMultiResultMessageConverter<TInput, TOutput>
        where TInput : class
        where TOutput : class
    {
        Task<IEnumerable<MotorCloudEvent<TOutput>>> ConvertMessageAsync(MotorCloudEvent<TInput> dataCloudEvent,
            CancellationToken token = default);
    }
}
