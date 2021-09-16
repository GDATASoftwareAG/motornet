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
        private ISyncSubscription? _subscription;

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
            if (_subscription == null)
            {
                return;
            }
            await Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await SingleMessageHandling(_subscription.NextMessage(), token)
                            .ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation("Terminating Nats Consumer.");
                        break;
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Failed to receive message.", e);
                    }
                }
            }, token).ConfigureAwait(false);
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
            _subscription = _client.SubscribeSync(_options.Topic, _options.Queue);
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken token = default)
        {
            _subscription?.Unsubscribe();
            await _client.DrainAsync();
            _client.Close();
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}
