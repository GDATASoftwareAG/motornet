namespace Motor.Extensions.Http
{
    public class HttpOptions
    {
        public const int DefaultNumberOfRetries = 2;
        public const int DefaultTimeoutInSeconds = 30;
        public int NumberOfRetries { get; set; } = DefaultNumberOfRetries;
        public int TimeoutInSeconds { get; set; } = DefaultTimeoutInSeconds;
    }
}
