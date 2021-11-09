namespace Motor.Extensions.Hosting.NATS.Options;

public class NATSConsumerOptions : NATSBaseOptions
{
    public string Queue { get; set; } = "";
}
