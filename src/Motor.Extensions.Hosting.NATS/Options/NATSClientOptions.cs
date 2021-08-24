namespace Motor.Extensions.Hosting.NATS.Options
{
    public class NATSClientOptions
    {
        public string Topic { get; set; } = "";
        public string Queue { get; set; } = "";
        public string Url { get; set; } = "";
    }
}
