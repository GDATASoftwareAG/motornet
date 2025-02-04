using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using Motor.Extensions.Hosting.RabbitMQ.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Motor.Extensions.Hosting.RabbitMQ;

public class RabbitMQMessageConsumer<T> : IMessageConsumer<T> where T : notnull
{
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly IApplicationNameService _applicationNameService;
    private readonly RabbitMQConsumerOptions<T> _options;
    private readonly ILogger<RabbitMQMessageConsumer<T>> _logger;
    private bool _started;
    private IChannel? _channel;
    private CancellationTokenSource _stoppingTokenSource = new();

    internal IRabbitMQConnectionFactory<T> ConnectionFactory { get; }

    public RabbitMQMessageConsumer(ILogger<RabbitMQMessageConsumer<T>> logger,
        IRabbitMQConnectionFactory<T> connectionFactory,
        IOptions<RabbitMQConsumerOptions<T>> config,
        IHostApplicationLifetime applicationLifetime,
        IApplicationNameService applicationNameService)
    {
        _logger = logger;
        ConnectionFactory = connectionFactory;
        _options = config.Value;
        _applicationLifetime = applicationLifetime;
        _applicationNameService = applicationNameService;
    }

    public Func<MotorCloudEvent<byte[]>, CancellationToken, Task<ProcessedMessageStatus>>? ConsumeCallbackAsync
    {
        get;
        set;
    }

    public async Task ExecuteAsync(CancellationToken token = default)
    {
        _stoppingTokenSource.Dispose();
        _stoppingTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
        while (token.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(100), token);
        }
    }

    public async Task StartAsync(CancellationToken token = default)
    {
        ThrowIfNoCallbackConfigured();
        ThrowIfConsumerAlreadyStarted();
        _channel = await ConnectionFactory.CurrentChannelAsync();
        await ConfigureChannelAsync(token);
        await DeclareQueueAsync();
        await StartConsumerOnChannelAsync();
    }

    public async Task StopAsync(CancellationToken token = default)
    {
        _started = false;
        await _stoppingTokenSource.CancelAsync();
        _stoppingTokenSource.Dispose();
        ConnectionFactory.Dispose();
    }

    private void ThrowIfNoCallbackConfigured()
    {
        if (ConsumeCallbackAsync is null)
        {
            throw new InvalidOperationException(
                $"Cannot start consuming as no {nameof(ConsumeCallbackAsync)} was configured!");
        }
    }

    private void ThrowIfConsumerAlreadyStarted()
    {
        if (_started)
        {
            throw new InvalidOperationException("Cannot start consuming as the consumer was already started!");
        }
    }

    private async Task ConfigureChannelAsync(CancellationToken ct)
    {
        if (_channel != null)
        {
            await _channel.BasicQosAsync(0, _options.PrefetchCount, false, ct);
        }
    }

    private async Task DeclareQueueAsync()
    {
        if (!_options.DeclareQueue)
        {
            return;
        }

        await DeclareAndBindConsumerQueueAsync();
        await DeclareAndBindConsumerDeadLetterExchangeQueueAsync();
    }

    private async Task StartConsumerOnChannelAsync()
    {
        if (_channel is null)
        {
            _logger.LogError(LogEvents.ConsumerNotStarted,
                "Message consumer could not start because channel is null");
            _applicationLifetime.StopApplication();
            return;
        }
        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += (_, args) =>
        {
#pragma warning disable CS4014
            /*
             * Awaiting the ConsumerCallback causes our service to wait for the processing of one message to complete
             * before fetching the next one from RabbitMQ. That way, our internal background queue is always empty even
             * though there are still messages left to process. This in turn causes our MessageProcessingHealthCheck
             * not to work properly.
             */
            ConsumerCallback(args);
#pragma warning restore CS4014
            return Task.CompletedTask;
        };
        _started = true;
        await _channel.BasicConsumeAsync(_options.Queue.Name, false, consumer,
            cancellationToken: _stoppingTokenSource.Token);
    }

    private async Task ConsumerCallback(BasicDeliverEventArgs args)
    {
        if (ConsumeCallbackAsync is null)
        {
            return;
        }

        try
        {
            var cloudEvent = args.ExtractCloudEvent(_applicationNameService, args.Body, _options.ExtractBindingKey);

            var processedMessageStatus = await ConsumeCallbackAsync.Invoke(cloudEvent, _stoppingTokenSource.Token)
                .ConfigureAwait(false);

            if (_stoppingTokenSource.IsCancellationRequested)
            {
                return;
            }

            if (_channel is null)
            {
                _logger.LogWarning(LogEvents.ChannelNullAfterProcessingComplete,
                    "Message processing status could not be returned to the channel because the channel is null");
                return;
            }

            switch (processedMessageStatus)
            {
                case ProcessedMessageStatus.Success:
                    await _channel.BasicAckAsync(args.DeliveryTag, false, _stoppingTokenSource.Token);
                    break;
                case ProcessedMessageStatus.TemporaryFailure:
                    await _channel.BasicRejectAsync(args.DeliveryTag, true, _stoppingTokenSource.Token);
                    break;
                case ProcessedMessageStatus.Failure:
                    await _channel.BasicRejectAsync(args.DeliveryTag, false, _stoppingTokenSource.Token);
                    break;
                case ProcessedMessageStatus.InvalidInput:
                    if (_options.Queue.DeadLetterExchange is null || _options.Queue.DeadLetterExchange
                            .RepublishInvalidInputToDeadLetterExchange)
                    {
                        await _channel.BasicRejectAsync(args.DeliveryTag, false, _stoppingTokenSource.Token);
                    }
                    else
                    {
                        await _channel.BasicAckAsync(args.DeliveryTag, false, _stoppingTokenSource.Token);
                    }

                    break;
                case ProcessedMessageStatus.CriticalFailure:
                    _logger.LogWarning(LogEvents.CriticalFailureOnConsume,
                        "Message consume fails with critical failure");
                    _applicationLifetime.StopApplication();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(processedMessageStatus),
                        processedMessageStatus.ToString());
            }
        }
        catch (Exception e)
        {
            _logger.LogCritical(LogEvents.UnexpectedErrorOnConsume, e, "Unexpected error on consume");
            _applicationLifetime.StopApplication();
        }

    }

    private Dictionary<string, object?> BuildQueueDeclareArguments(RabbitMQQueueLimitOptions rabbitMqQueueLimitOptions, bool includeDeadLetterExchangeArguments = true)
    {
        var arguments = _options.Queue.Arguments.ToDictionary(t => t.Key, t => t.Value);
        if (rabbitMqQueueLimitOptions.MaxPriority is not null)
        {
            arguments.Add("x-max-priority", rabbitMqQueueLimitOptions.MaxPriority);
        }

        if (rabbitMqQueueLimitOptions.MaxLength is not null)
        {
            arguments.Add("x-max-length", rabbitMqQueueLimitOptions.MaxLength);
        }

        if (rabbitMqQueueLimitOptions.MaxLengthBytes is not null)
        {
            arguments.Add("x-max-length-bytes", rabbitMqQueueLimitOptions.MaxLengthBytes);
        }

        if (rabbitMqQueueLimitOptions.MessageTtl is not null)
        {
            arguments.Add("x-message-ttl", rabbitMqQueueLimitOptions.MessageTtl);
        }

        if (_options.Queue.DeadLetterExchange is not null && includeDeadLetterExchangeArguments)
        {
            arguments.Add("x-dead-letter-exchange", _options.Queue.DeadLetterExchange.Binding.Exchange);
            arguments.Add("x-dead-letter-routing-key", _options.Queue.DeadLetterExchange.Binding.RoutingKey);
        }

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

        return arguments;
    }

    private async Task DeclareAndBindConsumerQueueAsync()
    {
        var limits = _options.Queue as RabbitMQQueueLimitOptions;
        var arguments = BuildQueueDeclareArguments(limits);
        if (_channel is null)
        {
            return;
        }
        await _channel.QueueDeclareAsync(
            _options.Queue.Name,
            _options.Queue.Durable,
            false,
            _options.Queue.AutoDelete,
            arguments, cancellationToken: _stoppingTokenSource.Token);
        foreach (var routingKeyConfig in _options.Queue.Bindings)
        {
            await _channel.QueueBindAsync(
                _options.Queue.Name,
                routingKeyConfig.Exchange,
                routingKeyConfig.RoutingKey,
                routingKeyConfig.Arguments, cancellationToken: _stoppingTokenSource.Token);
        }
    }

    private async Task DeclareAndBindConsumerDeadLetterExchangeQueueAsync()
    {
        if (_options.Queue.DeadLetterExchange is null)
        {
            return;
        }

        var limits = _options.Queue.DeadLetterExchange as RabbitMQQueueLimitOptions;
        var argumentsWithoutDeadLetterExchange = BuildQueueDeclareArguments(limits, false);

        var deadLetterExchangeQueueName = string.IsNullOrWhiteSpace(_options.Queue.DeadLetterExchange.Name)
            ? $"{_options.Queue.Name}Dlx"
            : _options.Queue.DeadLetterExchange.Name;
        if (_channel is null)
        {
            return;
        }

        await _channel.QueueDeclareAsync(
            deadLetterExchangeQueueName,
            _options.Queue.Durable,
            false,
            _options.Queue.AutoDelete,
            argumentsWithoutDeadLetterExchange, cancellationToken: _stoppingTokenSource.Token);
        await _channel.QueueBindAsync(
            deadLetterExchangeQueueName,
            _options.Queue.DeadLetterExchange.Binding.Exchange,
            _options.Queue.DeadLetterExchange.Binding.RoutingKey,
            _options.Queue.DeadLetterExchange.Binding.Arguments, cancellationToken: _stoppingTokenSource.Token);
    }
}
