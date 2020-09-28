namespace Motor.Extensions.Http
{
    public class HttpConfig
    {
        public int NumberOfRetries { get; set; } = 2;
        public int TimeoutInSeconds { get; set; } = 30;
    }
}
