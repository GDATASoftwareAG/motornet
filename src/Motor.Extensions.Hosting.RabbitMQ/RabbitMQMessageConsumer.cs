using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CloudNative.CloudEvents;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.RabbitMQ.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Motor.Extensions.Hosting.RabbitMQ
{
    public class RabbitMQMessageConsumer<T> : RabbitMQConnectionHandler, IMessageConsumer<T> where T : notnull
    {
        private readonly IHostApplicationLifetime _applicationLifetime;
        private readonly IApplicationNameService _applicationNameService;
        private readonly ICloudEventFormatter _cloudEventFormatter;
        private readonly RabbitMQConsumerOptions<T> _options;
        private readonly IRabbitMQConnectionFactory _connectionFactory;
        private readonly ILogger<RabbitMQMessageConsumer<T>> _logger;
        private bool _started;
        private CancellationToken _stoppingToken;

        public RabbitMQMessageConsumer(ILogger<RabbitMQMessageConsumer<T>> logger,
            IRabbitMQConnectionFactory connectionFactory, IOptions<RabbitMQConsumerOptions<T>> config,
            IHostApplicationLifetime applicationLifetime, IApplicationNameService applicationNameService,
            ICloudEventFormatter cloudEventFormatter)
        {
            _logger = logger;
            _connectionFactory = connectionFactory;
            _options = config.Value;
            _applicationLifetime = applicationLifetime;
            _applicationNameService = applicationNameService;
            _cloudEventFormatter = cloudEventFormatter;
        }

        public Func<MotorCloudEvent<byte[]>, CancellationToken, Task<ProcessedMessageStatus>>? ConsumeCallbackAsync
        {
            get;
            set;
        }

        public async Task ExecuteAsync(CancellationToken token = default)
        {
            _stoppingToken = token;
            while (token.IsCancellationRequested) await Task.Delay(TimeSpan.FromSeconds(100), token);
        }

        public Task StartAsync(CancellationToken token = default)
        {
            ThrowIfNoCallbackConfigured();
            ThrowIfConsumerAlreadyStarted();
            SetConnectionFactory();
            EstablishConnection();
            EstablishChannel();
            ConfigureChannel();
            DeclareQueue();
            StartConsumerOnChannel();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken token = default)
        {
            _started = false;
            Channel?.Close();
            return Task.CompletedTask;
        }

        private void ThrowIfNoCallbackConfigured()
        {
            if (ConsumeCallbackAsync is null)
                throw new InvalidOperationException(
                    $"Cannot start consuming as no {nameof(ConsumeCallbackAsync)} was configured!");
        }

        private void ThrowIfConsumerAlreadyStarted()
        {
            if (_started)
                throw new InvalidOperationException("Cannot start consuming as the consumer was already started!");
        }

        private void SetConnectionFactory()
        {
            ConnectionFactory = _connectionFactory.From(_options);
        }

        private void ConfigureChannel()
        {
            Channel?.BasicQos(0, _options.PrefetchCount, false);
        }

        private void DeclareQueue()
        {
            var arguments = _options.Queue.Arguments.ToDictionary(t => t.Key, t => t.Value);
            if (_options.Queue.MaxPriority is not null) arguments.Add("x-max-priority", _options.Queue.MaxPriority);

            if (_options.Queue.MaxLength is not null) arguments.Add("x-max-length", _options.Queue.MaxLength);

            if (_options.Queue.MaxLengthBytes is not null) arguments.Add("x-max-length-bytes", _options.Queue.MaxLengthBytes);

            if (_options.Queue.MessageTtl is not null) arguments.Add("x-message-ttl", _options.Queue.MessageTtl);

            switch (_options.Queue.Mode)
            {
                case QueueMode.Default:
                    break;
                case QueueMode.Lazy:
                    arguments.Add("x-queue-mode", "lazy");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            Channel?.QueueDeclare(
                _options.Queue.Name,
                _options.Queue.Durable,
                false,
                _options.Queue.AutoDelete,
                arguments
            );
            foreach (var routingKeyConfig in _options.Queue.Bindings)
                Channel?.QueueBind(
                    _options.Queue.Name,
                    routingKeyConfig.Exchange,
                    routingKeyConfig.RoutingKey,
                    routingKeyConfig.Arguments);
        }

        private void StartConsumerOnChannel()
        {
            var consumer = new EventingBasicConsumer(Channel);
            consumer.Received += (_, args) => ConsumerCallback(args);
            _started = true;
            Channel.BasicConsume(_options.Queue.Name, false, consumer);
        }

        private void ConsumerCallback(BasicDeliverEventArgs args)
        {
            try
            {
                var extensions = new List<ICloudEventExtension>();

                if (args.BasicProperties.IsPriorityPresent())
                {
                    var priority = args.BasicProperties.Priority;
                    extensions.Add(new RabbitMQPriorityExtension(priority));
                }

                var cloudEvent = args.BasicProperties.ExtractCloudEvent<T>(_applicationNameService,
                    _cloudEventFormatter, args.Body, extensions);

                var task = ConsumeCallbackAsync?.Invoke(cloudEvent, _stoppingToken)?
                    .ConfigureAwait(false)
                    .GetAwaiter();
                task?.OnCompleted(() =>
                {
                    if (_stoppingToken.IsCancellationRequested) return;

                    var processedMessageStatus = task?.GetResult();
                    switch (processedMessageStatus)
                    {
                        case ProcessedMessageStatus.Success:
                            Channel?.BasicAck(args.DeliveryTag, false);
                            break;
                        case ProcessedMessageStatus.TemporaryFailure:
                            Channel?.BasicReject(args.DeliveryTag, true);
                            break;
                        case ProcessedMessageStatus.InvalidInput:
                            Channel?.BasicReject(args.DeliveryTag, false);
                            break;
                        case ProcessedMessageStatus.CriticalFailure:
                            _logger.LogWarning(LogEvents.CriticalFailureOnConsume,
                                "Message consume fails with critical failure.");
                            _applicationLifetime.StopApplication();
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(processedMessageStatus),
                                processedMessageStatus.ToString());
                    }
                });
            }
            catch (Exception e)
            {
                _logger.LogCritical(LogEvents.UnexpectedErrorOnConsume, e, "Unexpected error on consume.");
                _applicationLifetime.StopApplication();
            }
        }
    }
}
