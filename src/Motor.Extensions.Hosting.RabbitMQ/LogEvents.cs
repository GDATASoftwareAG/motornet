using Microsoft.Extensions.Logging;

namespace Motor.Extensions.Hosting.RabbitMQ
{
    public static class LogEvents
    {
        public static readonly EventId UnexpectedErrorOnConsume = new(0, nameof(UnexpectedErrorOnConsume));
        public static readonly EventId CriticalFailureOnConsume = new(1, nameof(CriticalFailureOnConsume));
    }
}
