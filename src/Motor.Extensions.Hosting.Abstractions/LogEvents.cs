using Microsoft.Extensions.Logging;

namespace Motor.Extensions.Hosting.Abstractions
{
    public static class LogEvents
    {
        public static readonly EventId ServiceStarted = new EventId(0, nameof(ServiceStarted));
        public static readonly EventId ServiceStopped = new EventId(1, nameof(ServiceStopped));
        public static readonly EventId InvalidInput = new EventId(2, nameof(InvalidInput));
        public static readonly EventId ProcessingFailed = new EventId(3, nameof(ProcessingFailed));

        public static readonly EventId UnexpectedErrorOnMessageProcessing =
            new EventId(4, nameof(UnexpectedErrorOnMessageProcessing));

        public static readonly EventId EnforcedShutdown = new EventId(5, nameof(EnforcedShutdown));
    }
}
