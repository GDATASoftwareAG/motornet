using Motor.Extensions.Hosting.NATS.Options;
using NATS.Client;

namespace Motor.Extensions.Hosting.NATS;

public interface INATSClientFactory
{
    IConnection From(NATSClientOptions clientOptions);
}

public class NATSClientFactory : INATSClientFactory
{
    public IConnection From(NATSClientOptions clientOptions)
    {
        var connectionFactory = new ConnectionFactory();
        return connectionFactory.CreateConnection(clientOptions.Url);
    }
}
