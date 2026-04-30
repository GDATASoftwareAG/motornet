using Microsoft.Extensions.Logging;

namespace Motor.Extensions.Hosting.PgMq;

public static class LogEvents
{
    // Large base offset to avoid collisions with other Motor.NET components and user-defined event IDs.
    // In practice, log consumers typically filter by name rather than ID, but unique IDs avoid
    // ambiguity when multiple components are used together.
    private const int BaseOffset = 324_290_000;

    public static readonly EventId CriticalFailureOnConsume = new(BaseOffset + 0, nameof(CriticalFailureOnConsume));
    public static readonly EventId MessageHandlingUnexpectedException = new(BaseOffset + 1, nameof(MessageHandlingUnexpectedException));
    public static readonly EventId ConsumerNotStarted = new(BaseOffset + 2, nameof(ConsumerNotStarted));
    public static readonly EventId NoMessageReceived = new(BaseOffset + 3, nameof(NoMessageReceived));
    public static readonly EventId TemporaryFailureOnConsume = new(BaseOffset + 4, nameof(TemporaryFailureOnConsume));
    public static readonly EventId InvalidInputOnConsume = new(BaseOffset + 5, nameof(InvalidInputOnConsume));
    public static readonly EventId FailureOnConsume = new(BaseOffset + 6, nameof(FailureOnConsume));
    public static readonly EventId MessageSuccessfullyProcessed = new(BaseOffset + 7, nameof(MessageSuccessfullyProcessed));
}
