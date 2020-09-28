using System.Threading;
using System.Threading.Tasks;

namespace Motor.Extensions.Hosting.Abstractions
{
    public interface IMessageConverter<TInput, TOutput>
        where TInput : class 
        where TOutput : class
    {
        Task<MotorCloudEvent<TOutput>> ConvertMessageAsync(MotorCloudEvent<TInput> dataCloudEvent,
            CancellationToken token = default);
    }
}
