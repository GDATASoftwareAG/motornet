using System;

namespace Motor.Extensions.Hosting.Abstractions;

public enum FailureLevel
{
    Warning,
    Error
}
public class TemporaryFailureException : Exception
{
    public FailureLevel Level { get; private set; }
    public TemporaryFailureException()
    {
    }

    public TemporaryFailureException(string message, FailureLevel level = FailureLevel.Error) : base(message)
    {
        Level = level;
    }

    public TemporaryFailureException(string message, Exception ex, FailureLevel level = FailureLevel.Error) : base(message, ex)
    {
        Level = level;
    }
}
