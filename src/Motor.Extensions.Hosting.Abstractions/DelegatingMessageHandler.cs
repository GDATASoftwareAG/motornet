using System;
using System.Threading;
using System.Threading.Tasks;

namespace Motor.Extensions.Hosting.Abstractions
{
    public abstract class DelegatingMessageHandler<TInput> : IMessageHandler<TInput>
        where TInput : class
    {
        public IMessageHandler<TInput>? InnerMessageHandler { get; set; }

        public virtual Task<ProcessedMessageStatus> HandleMessageAsync(MotorCloudEvent<TInput> dataCloudEvent,
            CancellationToken token = default)
        {
            if (InnerMessageHandler == null) {
                throw new IndexOutOfRangeException("No message handler was set.");
            }
            return InnerMessageHandler.HandleMessageAsync(dataCloudEvent, token);
        }
    }
}
