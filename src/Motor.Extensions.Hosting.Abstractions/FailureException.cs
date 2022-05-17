using System;

namespace Motor.Extensions.Hosting.Abstractions;

public class FailureException : Exception
{
    public FailureException()
    {
    }

    public FailureException(string? message) : base(message)
    {
    }

    public FailureException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
