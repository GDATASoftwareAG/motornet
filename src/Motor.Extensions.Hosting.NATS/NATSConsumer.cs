using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using Motor.Extensions.Hosting.NATS.Options;
using NATS.Client;

namespace Motor.Extensions.Hosting.NATS
{
    public class NATSConsumer<TData> : IMessageConsumer<TData>, IDisposable where TData : notnull
    {
        private readonly NATSClientOptions _options;
        private readonly ILogger<NATSConsumer<TData>> _logger;
        private readonly IApplicationNameService _applicationNameService;
        private readonly IConnection _client;

        public NATSConsumer(IOptions<NATSClientOptions> options, ILogger<NATSConsumer<TData>> logger,
            IApplicationNameService applicationNameService, INATSClientFactory natsClientFactory)
        {
            _options = options.Value;
            _logger = logger;
            _applicationNameService = applicationNameService;
            _client = natsClientFactory.From(_options);
        }

        public async Task ExecuteAsync(CancellationToken token = default)
        {
            using var subscription = _client.SubscribeAsync(_options.Topic, _options.Queue, async (sender, args) =>
                {
                    await SingleMessageHandling(args.Message, token);
                });
            
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(10, token);
            }
            
            subscription.Unsubscribe();
            await _client.DrainAsync();
            _client.Close();
        }

        private async Task SingleMessageHandling(Msg message, CancellationToken token)
        {
            var dataCloudEvent = message.Data.ToMotorCloudEvent(_applicationNameService);
            await ConsumeCallbackAsync?.Invoke(dataCloudEvent, token)!;
        }

        public Func<MotorCloudEvent<byte[]>, CancellationToken, Task<ProcessedMessageStatus>>? ConsumeCallbackAsync
        {
            get;
            set;
        }

        public Task StartAsync(CancellationToken token = default)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken token = default)
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}
