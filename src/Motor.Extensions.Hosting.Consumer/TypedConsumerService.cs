using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Motor.Extensions.Conversion.Abstractions;
using Motor.Extensions.Hosting.Abstractions;

namespace Motor.Extensions.Hosting.Consumer
{
    public class TypedConsumerService<TInput> : BackgroundService
        where TInput : class
    {
        private readonly IMessageConsumer<TInput> _consumer;
        private readonly IMessageDeserializer<TInput> _deserializer;
        private readonly ILogger<TypedConsumerService<TInput>> _logger;
        private readonly IBackgroundTaskQueue<MotorCloudEvent<TInput>> _queue;

        public TypedConsumerService(
            ILogger<TypedConsumerService<TInput>> logger,
            IBackgroundTaskQueue<MotorCloudEvent<TInput>> queue,
            IMessageDeserializer<TInput> deserializer,
            IMessageConsumer<TInput> consumer)
        {
            _logger = logger;
            _queue = queue;
            _deserializer = deserializer;
            _consumer = consumer;
            _consumer.ConsumeCallbackAsync = SingleMessageConsumeAsync;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await _consumer.StartAsync(cancellationToken);
            await base.StartAsync(cancellationToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await base.StopAsync(cancellationToken);
            await _consumer.StopAsync(cancellationToken);
        }

        private async Task<ProcessedMessageStatus> SingleMessageConsumeAsync(MotorCloudEvent<byte[]> dataCloudEvent,
            CancellationToken stoppingToken)
        {
            try
            {
                var deserialize = _deserializer.Deserialize(dataCloudEvent.TypedData);
                return await _queue
                    .QueueBackgroundWorkItem(dataCloudEvent.CreateNew(deserialize, true))
                    .ConfigureAwait(true);
            }
            catch (ArgumentException e)
            {
                _logger.LogError(LogEvents.InvalidInput, e, "Invalid Input");
                return ProcessedMessageStatus.InvalidInput;
            }
            catch (Exception e)
            {
                _logger.LogError(LogEvents.UnexpectedErrorOnMessageProcessing, e, "Invalid Input");
                return ProcessedMessageStatus.CriticalFailure;
            }
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _consumer.ConsumeCallbackAsync = SingleMessageConsumeAsync;
            return _consumer.ExecuteAsync(stoppingToken);
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
