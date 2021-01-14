using System;
using System.Threading;
using System.Threading.Tasks;

namespace Motor.Extensions.Hosting.Abstractions
{
    public abstract class DelegatingMessageHandler<TInput> : INoOutputService<TInput>
        where TInput : class
    {
        public INoOutputService<TInput>? InnerService { get; set; }

        public virtual Task<ProcessedMessageStatus> HandleMessageAsync(MotorCloudEvent<TInput> dataCloudEvent,
            CancellationToken token = default)
        {
            if (InnerService is null)
            {
                throw new IndexOutOfRangeException("No message handler was set.");
            }
            return InnerService.HandleMessageAsync(dataCloudEvent, token);
        }
    }
}
