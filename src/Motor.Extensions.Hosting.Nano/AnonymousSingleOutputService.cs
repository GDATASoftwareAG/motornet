using System;
using System.Threading;
using System.Threading.Tasks;
using Motor.Extensions.Hosting.Abstractions;

namespace Motor.Extensions.Hosting.Nano
{
    public class AnonymousSingleOutputService<TInput, TOutput> : ISingleOutputService<TInput, TOutput> where TInput : class where TOutput : class
    {
        private readonly Func<MotorCloudEvent<TInput>, CancellationToken, Task<MotorCloudEvent<TOutput>>> _handler;

        public AnonymousSingleOutputService(Func<MotorCloudEvent<TInput>, CancellationToken, Task<MotorCloudEvent<TOutput>>> handler)
        {
            _handler = handler;
        }

        public AnonymousSingleOutputService(Func<MotorCloudEvent<TInput>, MotorCloudEvent<TOutput>> handler)
        {
            _handler = (cloudEvent, _) => Task.FromResult(handler(cloudEvent));
        }

        public Task<MotorCloudEvent<TOutput>> ConvertMessageAsync(MotorCloudEvent<TInput> dataCloudEvent,
            CancellationToken token = default) => _handler(dataCloudEvent, token);
    }
}
