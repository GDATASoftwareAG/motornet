using System;
using System.Threading;
using System.Threading.Tasks;

namespace Motor.Extensions.Hosting.Abstractions
{
    public interface IMessageHandler<TInput>
        where TInput : class
    {
        Task<ProcessedMessageStatus>
            HandleMessageAsync(MotorCloudEvent<TInput> dataCloudEvent, CancellationToken token = default);
    }
}
