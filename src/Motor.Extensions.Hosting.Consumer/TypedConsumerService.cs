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

        public override async Task StartAsync(CancellationToken token)
        {
            await _consumer.StartAsync(token);
            await base.StartAsync(token);
        }

        public override async Task StopAsync(CancellationToken token)
        {
            await base.StopAsync(token);
            await _consumer.StopAsync(token);
        }

        private async Task<ProcessedMessageStatus> SingleMessageConsumeAsync(MotorCloudEvent<byte[]> dataCloudEvent,
            CancellationToken token)
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

        protected override Task ExecuteAsync(CancellationToken token)
        {
            _consumer.ConsumeCallbackAsync = SingleMessageConsumeAsync;
            return _consumer.ExecuteAsync(token);
        }
    }
}
