using Microsoft.Extensions.Logging;

namespace Motor.Extensions.Hosting.Abstractions;

public static class LogEvents
{
    public static readonly EventId ServiceWasStopped = new(1, nameof(ServiceWasStopped));
    public static readonly EventId InvalidInput = new(2, nameof(InvalidInput));
    public static readonly EventId ProcessingFailed = new(3, nameof(ProcessingFailed));

    public static readonly EventId UnexpectedErrorOnMessageProcessing = new(
        4,
        nameof(UnexpectedErrorOnMessageProcessing)
    );
}
