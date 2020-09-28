using Microsoft.Extensions.Logging;

namespace Motor.Extensions.Hosting.RabbitMQ
{
    public static class LogEvents
    {
        public static readonly EventId UnexpectedErrorOnConsume = new EventId(0, nameof(UnexpectedErrorOnConsume));
        public static readonly EventId CriticalFailureOnConsume = new EventId(1, nameof(CriticalFailureOnConsume));
    }
}
