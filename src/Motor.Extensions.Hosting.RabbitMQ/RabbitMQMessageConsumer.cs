using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.RabbitMQ.Config;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Motor.Extensions.Hosting.RabbitMQ
{
    public class RabbitMQMessageConsumer<T> : RabbitMQConnectionHandler, IMessageConsumer<T>
    {
        private readonly IHostApplicationLifetime _applicationLifetime;
        private readonly IApplicationNameService _applicationNameService;
        private readonly ICloudEventFormatter _cloudEventFormatter;
        private readonly RabbitMQConsumerConfig<T> _config;
        private readonly IRabbitMQConnectionFactory _connectionFactory;
        private readonly ILogger<RabbitMQMessageConsumer<T>> _logger;
        private bool _started;
        private CancellationToken StoppingToken;

        public RabbitMQMessageConsumer(ILogger<RabbitMQMessageConsumer<T>> logger,
            IRabbitMQConnectionFactory connectionFactory, IOptions<RabbitMQConsumerConfig<T>> config,
            IHostApplicationLifetime applicationLifetime, IApplicationNameService applicationNameService,
            ICloudEventFormatter cloudEventFormatter)
        {
            _logger = logger;
            _connectionFactory = connectionFactory;
            _config = config.Value;
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
            StoppingToken = token;
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
            if (ConsumeCallbackAsync == null)
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
            ConnectionFactory = _connectionFactory.From(_config);
        }

        private void ConfigureChannel()
        {
            Channel?.BasicQos(0, _config.PrefetchCount, false);
        }

        private void DeclareQueue()
        {
            var arguments = _config.Queue.Arguments.ToDictionary(t => t.Key, t => t.Value);
            if (_config.Queue.MaxPriority != null) arguments.Add("x-max-priority", _config.Queue.MaxPriority);

            if (_config.Queue.MaxLength != null) arguments.Add("x-max-length", _config.Queue.MaxLength);

            if (_config.Queue.MaxLengthBytes != null) arguments.Add("x-max-length-bytes", _config.Queue.MaxLengthBytes);

            if (_config.Queue.MessageTtl != null) arguments.Add("x-message-ttl", _config.Queue.MessageTtl);

            switch (_config.Queue.Mode)
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
                _config.Queue.Name,
                _config.Queue.Durable,
                false,
                _config.Queue.AutoDelete,
                arguments
            );
            foreach (var routingKeyConfig in _config.Queue.Bindings)
                Channel?.QueueBind(
                    _config.Queue.Name,
                    routingKeyConfig.Exchange,
                    routingKeyConfig.RoutingKey,
                    routingKeyConfig.Arguments);
        }

        private void StartConsumerOnChannel()
        {
            var consumer = new EventingBasicConsumer(Channel);
            consumer.Received += (_, args) => ConsumerCallback(args);
            _started = true;
            Channel.BasicConsume(_config.Queue.Name, false, consumer);
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

                var task = ConsumeCallbackAsync?.Invoke(cloudEvent, StoppingToken)?
                    .ConfigureAwait(false)
                    .GetAwaiter();
                task?.OnCompleted(() =>
                {
                    if (StoppingToken.IsCancellationRequested) return;

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
