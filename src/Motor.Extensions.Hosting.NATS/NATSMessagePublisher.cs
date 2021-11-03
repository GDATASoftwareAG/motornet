using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using Motor.Extensions.Hosting.NATS.Options;
using NATS.Client;

namespace Motor.Extensions.Hosting.NATS
{
    public class NATSMessagePublisher : ITypedMessagePublisher<byte[]>, IDisposable
    {
        private readonly NATSBaseOptions _options;
        private readonly IConnection _client;

        public NATSMessagePublisher(IOptions<NATSBaseOptions> options, INATSClientFactory natsClientFactory)
        {
            _options = options.Value;
            _client = natsClientFactory.From(_options);
        }

        public Task PublishMessageAsync(MotorCloudEvent<byte[]> motorCloudEvent, CancellationToken token = default)
        {
            _client.Publish(_options.Topic, motorCloudEvent.TypedData);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}
