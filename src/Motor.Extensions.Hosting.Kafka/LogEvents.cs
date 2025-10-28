using Microsoft.Extensions.Logging;

namespace Motor.Extensions.Hosting.Kafka;

public static class LogEvents
{
    public static readonly EventId CriticalFailureOnConsume = new(0, nameof(CriticalFailureOnConsume));
    public static readonly EventId FailureDespiteRetrying = new(1, nameof(FailureDespiteRetrying));
    public static readonly EventId CommitError = new(2, nameof(CommitError));
    public static readonly EventId NoMessageReceived = new(3, nameof(NoMessageReceived));
    public static readonly EventId TerminatingKafkaListener = new(4, nameof(TerminatingKafkaListener));
    public static readonly EventId MessageReceivedFailure = new(5, nameof(MessageReceivedFailure));
    public static readonly EventId ReceivedMessage = new(6, nameof(ReceivedMessage));
    public static readonly EventId UnknownProcessedMessageStatus = new(7, nameof(UnknownProcessedMessageStatus));

    public static readonly EventId MessageHandlingUnexpectedException = new(
        7,
        nameof(MessageHandlingUnexpectedException)
    );
}
