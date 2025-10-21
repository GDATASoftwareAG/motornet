using Microsoft.Extensions.Logging;
using Motor.Extensions.Diagnostics.Metrics.Abstractions;
using Motor.Extensions.Hosting.Abstractions;

namespace Motor.Extensions.Hosting;

public class SingleOutputServiceAdapter<TInput, TOutput> : MultiOutputServiceAdapter<TInput, TOutput>
    where TInput : class
    where TOutput : class
{
    public SingleOutputServiceAdapter(
        ILogger<SingleOutputServiceAdapter<TInput, TOutput>> logger,
        ISingleOutputService<TInput, TOutput> service,
        ITypedMessagePublisher<TOutput> publisher
    )
        : base(logger, new SingleToMultiOutputAdapter<TInput, TOutput>(service), publisher) { }
}
