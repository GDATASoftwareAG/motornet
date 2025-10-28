using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Motor.Extensions.Diagnostics.Queue.Abstractions;
using Motor.Extensions.Hosting.RabbitMQ.Options;
using static Motor.Extensions.Hosting.RabbitMQ.LogEvents;

namespace Motor.Extensions.Hosting.RabbitMQ;

public class RabbitMQQueueMonitor<T> : IQueueMonitor
    where T : notnull
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

    public async Task<QueueState> GetCurrentStateAsync()
    {
        try
        {
            _logger.LogDebug(QueueStateRetrieval, "Retrieving current state of queue");
            var currentChannel = await _connectionFactory.CurrentChannelAsync();
            var result = await currentChannel.QueueDeclarePassiveAsync(_options.Value.Queue.Name);
            return new QueueState(_options.Value.Queue.Name, result.MessageCount, result.ConsumerCount);
        }
        catch (Exception e)
        {
            _logger.LogWarning(
                QueueStateRetrievalFailed,
                e,
                "Failed to QueueDeclarePassive for queue {QueueName}",
                _options.Value.Queue.Name
            );
            return new QueueState(_options.Value.Queue.Name, -1, -1);
        }
    }
}
