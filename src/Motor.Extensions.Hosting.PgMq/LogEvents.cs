using Microsoft.Extensions.Logging;

namespace Motor.Extensions.Hosting.PgMq;

public static class LogEvents
{
    public static readonly EventId CriticalFailureOnConsume = new(0, nameof(CriticalFailureOnConsume));
    public static readonly EventId MessageHandlingUnexpectedException = new(1, nameof(MessageHandlingUnexpectedException));
    public static readonly EventId ConsumerNotStarted = new(2, nameof(ConsumerNotStarted));
    public static readonly EventId NoMessageReceived = new(3, nameof(NoMessageReceived));
}
