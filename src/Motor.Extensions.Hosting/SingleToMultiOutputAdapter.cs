using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;

namespace Motor.Extensions.Hosting;

public class SingleToMultiOutputAdapter<TInput, TOutput> : IMultiOutputService<TInput, TOutput>
    where TInput : class
    where TOutput : class
{
    private readonly ISingleOutputService<TInput, TOutput> _service;

    public SingleToMultiOutputAdapter(ISingleOutputService<TInput, TOutput> service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public async IAsyncEnumerable<MotorCloudEvent<TOutput>> ConvertMessageAsync(
        MotorCloudEvent<TInput> dataCloudEvent, [EnumeratorCancellation] CancellationToken token)
    {
        var convertMessage = await _service.ConvertMessageAsync(dataCloudEvent, token)
            .ConfigureAwait(false);
        if (convertMessage != null)
        {
            yield return convertMessage;
        }
    }
}
