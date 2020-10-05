using Microsoft.Extensions.Logging;

namespace Motor.Extensions.Diagnostics.Tracing
{
    public static class LogEvents
    {
        private static int _id;
        public static readonly EventId JaegerConfigurationFailed = new EventId(++_id, nameof(JaegerConfigurationFailed));
    }
}
