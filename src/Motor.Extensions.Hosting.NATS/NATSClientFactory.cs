using Motor.Extensions.Hosting.NATS.Options;
using NATS.Client;

namespace Motor.Extensions.Hosting.NATS
{
    public interface INATSClientFactory
    {
        IConnection From(NATSBaseOptions consumerOptions);
    }

    public class NATSClientFactory : INATSClientFactory
    {
        public IConnection From(NATSBaseOptions consumerOptions)
        {
            var connectionFactory = new ConnectionFactory();
            return connectionFactory.CreateConnection(consumerOptions.Url);
        }
    }
}
