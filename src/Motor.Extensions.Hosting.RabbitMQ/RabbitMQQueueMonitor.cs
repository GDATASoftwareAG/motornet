using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Motor.Extensions.Diagnostics.Queue.Abstractions;
using Motor.Extensions.Hosting.RabbitMQ.Options;
using static Motor.Extensions.Hosting.RabbitMQ.LogEvents;

namespace Motor.Extensions.Hosting.RabbitMQ;

public class RabbitMQQueueMonitor<T> : IQueueMonitor where T : notnull
{
    private readonly IOptions<RabbitMQConsumerOptions<T>> _options;
    private readonly IRabbitMQConnectionFactory<T> _connectionFactory;

    public RabbitMQQueueMonitor(
        ILogger<RabbitMQQueueMonitor<T>> logger,
        IOptions<RabbitMQConsumerOptions<T>> config,
        IRabbitMQConnectionFactory<T> connectionFactory
    )
    {
        _options = config;
        _logger = logger;
        _connectionFactory = connectionFactory;
    }

    private readonly ILogger<RabbitMQQueueMonitor<T>> _logger;

    public Task<QueueState> GetCurrentState()
    {
        try
        {
            _logger.LogDebug(QueueStateRetrieval, "Retrieving current state of queue");
            var result = _connectionFactory.CurrentChannel.QueueDeclarePassive(_options.Value.Queue.Name);
            var state = new QueueState(_options.Value.Queue.Name, result?.MessageCount ?? 0, result?.ConsumerCount ?? 0);
            return Task.FromResult(state);
        }
        catch (Exception e)
        {
            _logger.LogWarning(QueueStateRetrievalFailed, e, "Failed to QueueDeclarePassive for queue {QueueName}",
                _options.Value.Queue.Name);
            return Task.FromResult(new QueueState(_options.Value.Queue.Name, -1, -1));
        }
    }
}
