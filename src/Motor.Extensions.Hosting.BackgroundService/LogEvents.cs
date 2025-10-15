using Microsoft.Extensions.Logging;

namespace Motor.Extensions.Hosting.BackgroundService;

public static class LogEvents
{
    public static readonly EventId FailedToStart = new(0, nameof(FailedToStart));
    public static readonly EventId Failure = new(1, nameof(Failure));
    public static readonly EventId FinishedSuccessfully = new(2, nameof(FinishedSuccessfully));
}
