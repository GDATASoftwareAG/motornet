using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CloudNative.CloudEvents;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Motor.Extensions.Diagnostics.Metrics.Abstractions;
using Motor.Extensions.Hosting.Abstractions;
using Prometheus.Client.Abstractions;

namespace Motor.Extensions.Hosting.Kafka
{
    public sealed class KafkaMessageConsumer<TData> : IMessageConsumer<TData>, IDisposable
    {
        private readonly IApplicationNameService _applicationNameService;
        private readonly ICloudEventFormatter _cloudEventFormatter;
        private readonly KafkaConsumerConfig<TData> _config;
        private readonly IMetricFamily<IGauge>? _consumerLagGauge;
        private readonly IMetricFamily<ISummary>? _consumerLagSummary;
        private readonly ILogger<KafkaMessageConsumer<TData>> _logger;
        private IConsumer<string, byte[]>? _consumer;

        public KafkaMessageConsumer(ILogger<KafkaMessageConsumer<TData>> logger,
            IOptions<KafkaConsumerConfig<TData>> config,
            IMetricsFactory<KafkaMessageConsumer<TData>>? metricsFactory,
            IApplicationNameService applicationNameService,
            ICloudEventFormatter cloudEventFormatter)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _applicationNameService = applicationNameService ?? throw new ArgumentNullException(nameof(config));
            _cloudEventFormatter = cloudEventFormatter;
            _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
            _consumerLagSummary = metricsFactory?.CreateSummary("consumer_lag_distribution",
                "Contains a summary of current consumer lag of each partition", "topic", "partition");
            _consumerLagGauge = metricsFactory?.CreateGauge("consumer_lag",
                "Contains current number consumer lag of each partition", "topic", "partition");
        }

        public Func<MotorCloudEvent<byte[]>, CancellationToken, Task<ProcessedMessageStatus>>? ConsumeCallbackAsync
        {
            get;
            set;
        }

        public Task StartAsync(CancellationToken stoppingToken = default)
        {
            if (ConsumeCallbackAsync == null) throw new InvalidOperationException("ConsumeCallback is null");

            var consumerBuilder = new ConsumerBuilder<string, byte[]>(_config)
                .SetLogHandler((_, logMessage) => WriteLog(logMessage))
                .SetStatisticsHandler((_, json) => WriteStatistics(json));

            _consumer = consumerBuilder.Build();
            _consumer.Subscribe(_config.Topic);
            return Task.CompletedTask;
        }

        public async Task ExecuteAsync(CancellationToken stoppingToken = default)
        {
            await Task.Run(() =>
            {
                while (!stoppingToken.IsCancellationRequested)
                    try
                    {
                        var msg = _consumer?.Consume(stoppingToken);
                        if (msg != null && !msg.IsPartitionEOF)
                            SingleMessageHandling(stoppingToken, msg);
                        else
                            _logger.LogDebug("No messages received");
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation("Terminating Kafka listener...");
                        break;
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Failed to receive message.", e);
                    }
            }, stoppingToken);
        }

        public Task StopAsync(CancellationToken stoppingToken = default)
        {
            _consumer?.Close();
            return Task.CompletedTask;
        }

        private void WriteLog(LogMessage logMessage)
        {
            switch (logMessage.Level)
            {
                case SyslogLevel.Emergency:
                case SyslogLevel.Alert:
                case SyslogLevel.Critical:
                    _logger.LogCritical($"{logMessage.Message} -(Facility: {{facility}}, Name: {{name}})",
                        logMessage.Facility, logMessage.Name);
                    break;
                case SyslogLevel.Error:
                    _logger.LogError($"{logMessage.Message} -(Facility: {{facility}}, Name: {{name}})",
                        logMessage.Facility, logMessage.Name);
                    break;
                case SyslogLevel.Warning:
                    _logger.LogWarning($"{logMessage.Message} -(Facility: {{facility}}, Name: {{name}})",
                        logMessage.Facility, logMessage.Name);
                    break;
                case SyslogLevel.Notice:
                case SyslogLevel.Info:
                    _logger.LogInformation($"{logMessage.Message} -(Facility: {{facility}}, Name: {{name}})",
                        logMessage.Facility, logMessage.Name);
                    break;
                case SyslogLevel.Debug:
                    _logger.LogDebug($"{logMessage.Message} -(Facility: {{facility}}, Name: {{name}})",
                        logMessage.Facility, logMessage.Name);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void WriteStatistics(string json)
        {
            var partitionConsumerLags = JsonSerializer
                .Deserialize<KafkaStatistics>(json)?
                .Topics?
                .Select(t => t.Value)
                .SelectMany(t => t.Partitions)
                .Select(t => (Parition: t.Key.ToString(), t.Value.ConsumerLag));
            if (partitionConsumerLags == null) return;
            foreach (var (partition, consumerLag) in partitionConsumerLags)
            {
                var lag = consumerLag;
                if (lag == -1) lag = 0;

                _consumerLagSummary?.WithLabels(_config.Topic, partition)?.Observe(lag);
                _consumerLagGauge?.WithLabels(_config.Topic, partition)?.Set(lag);
            }
        }

        private void SingleMessageHandling(CancellationToken stoppingToken, ConsumeResult<string, byte[]> msg)
        {
            _logger.LogDebug(
                $"Received message from topic '{msg.Topic}:{msg.Partition}' with offset: '{msg.Offset}[{msg.TopicPartitionOffset}]'");
            var cloudEvent = msg.ToMotorCloudEvent<TData>(_applicationNameService, _cloudEventFormatter);

            var taskAwaiter = ConsumeCallbackAsync?.Invoke(cloudEvent, stoppingToken)?.GetAwaiter();
            taskAwaiter?.OnCompleted(() =>
            {
                var processedMessageStatus = taskAwaiter?.GetResult();
                switch (processedMessageStatus)
                {
                    case ProcessedMessageStatus.Success:
                        break;
                    case ProcessedMessageStatus.TemporaryFailure:
                        break;
                    case ProcessedMessageStatus.InvalidInput:
                        break;
                    case ProcessedMessageStatus.CriticalFailure:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (msg.Offset % _config.CommitPeriod != 0) return;

                try
                {
                    _consumer?.Commit(msg);
                }
                catch (KafkaException e)
                {
                    _logger.LogError($"Commit error: {e.Error.Reason}");
                }
            });
        }

        private void Dispose(bool disposing)
        {
            if (disposing) _consumer?.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
