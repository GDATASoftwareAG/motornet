using System;
using System.Threading;
using System.Threading.Tasks;
using Motor.Extensions.Hosting.Abstractions;

namespace Motor.Extensions.Hosting.Nano
{
    public class AnonymousNoOutputService<TInput> : INoOutputService<TInput> where TInput : class
    {
        private readonly Func<MotorCloudEvent<TInput>, CancellationToken, Task<ProcessedMessageStatus>> _handler;

        public AnonymousNoOutputService(Func<MotorCloudEvent<TInput>, CancellationToken, Task<ProcessedMessageStatus>> handler)
        {
            _handler = handler;
        }

        public AnonymousNoOutputService(Func<MotorCloudEvent<TInput>, ProcessedMessageStatus> handler)
        {
            _handler = (cloudEvent, _) => Task.FromResult(handler(cloudEvent));
        }

        public Task<ProcessedMessageStatus> HandleMessageAsync(MotorCloudEvent<TInput> dataCloudEvent,
            CancellationToken token = default) => _handler(dataCloudEvent, token);
    }
}
