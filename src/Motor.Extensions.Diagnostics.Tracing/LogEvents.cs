using Microsoft.Extensions.Logging;

namespace Motor.Extensions.Diagnostics.Tracing
{
    public static class LogEvents
    {
        private static int _id;
        public static readonly EventId JaegerConfigurationFailed = new(++_id, nameof(JaegerConfigurationFailed));
    }
}
