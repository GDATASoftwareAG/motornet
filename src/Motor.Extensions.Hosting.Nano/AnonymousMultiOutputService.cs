using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Motor.Extensions.Hosting.Abstractions;

namespace Motor.Extensions.Hosting.Nano
{
    public class AnonymousMultiOutputService<TInput, TOutput> : IMultiOutputService<TInput, TOutput> where TInput : class where TOutput : class
    {
        private readonly Func<MotorCloudEvent<TInput>, CancellationToken, Task<IEnumerable<MotorCloudEvent<TOutput>>>> _handler;

        public AnonymousMultiOutputService(Func<MotorCloudEvent<TInput>, CancellationToken, Task<IEnumerable<MotorCloudEvent<TOutput>>>> handler)
        {
            _handler = handler;
        }

        public AnonymousMultiOutputService(Func<MotorCloudEvent<TInput>, IEnumerable<MotorCloudEvent<TOutput>>> handler)
        {
            _handler = (cloudEvent, _) => Task.FromResult(handler(cloudEvent));
        }

        public Task<IEnumerable<MotorCloudEvent<TOutput>>> ConvertMessageAsync(MotorCloudEvent<TInput> dataCloudEvent,
            CancellationToken token = default) => _handler(dataCloudEvent, token);
    }
}
