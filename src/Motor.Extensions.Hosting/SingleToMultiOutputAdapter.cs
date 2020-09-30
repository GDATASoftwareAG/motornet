using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Motor.Extensions.Hosting.Abstractions;

namespace Motor.Extensions.Hosting
{
    public class SingleToMultiOutputAdapter<TInput, TOutput> : IMultiOutputService<TInput, TOutput>
        where TInput : class
        where TOutput : class
    {
        private readonly ISingleOutputService<TInput, TOutput> _service;

        public SingleToMultiOutputAdapter(ISingleOutputService<TInput, TOutput> service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        public async Task<IEnumerable<MotorCloudEvent<TOutput>>> ConvertMessageAsync(
            MotorCloudEvent<TInput> dataCloudEvent, CancellationToken token)
        {
            var convertMessage = await _service.ConvertMessageAsync(dataCloudEvent, token)
                .ConfigureAwait(false);
            return convertMessage == null ? new MotorCloudEvent<TOutput>[0] : new[] {convertMessage};
        }
    }
}
