namespace Motor.Extensions.Hosting.Abstractions;

public enum ProcessedMessageStatus
{
    Success,
    TemporaryFailure,
    InvalidInput,
    CriticalFailure,
    Failure
}
