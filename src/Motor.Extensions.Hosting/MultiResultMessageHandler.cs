using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Diagnostics.Metrics;
using Motor.Extensions.Diagnostics.Metrics.Abstractions;
using Microsoft.Extensions.Logging;
using Prometheus.Client;
using Prometheus.Client.Abstractions;

namespace Motor.Extensions.Hosting
{
    public class MultiResultMessageHandler<TInput, TOutput> : IMessageHandler<TInput>
        where TInput : class 
        where TOutput : class
    {
        private readonly ILogger<MessageHandler<TInput, TOutput>> _logger;
        private readonly IMultiResultMessageConverter<TInput, TOutput> _converter;
        private readonly ITypedMessagePublisher<TOutput> _publisher;
        private readonly IMetricFamily<ISummary>? _messageProcessing;

        public MultiResultMessageHandler(ILogger<MessageHandler<TInput, TOutput>> logger,
            IMetricsFactory<MessageHandler<TInput, TOutput>> metrics, IMultiResultMessageConverter<TInput, TOutput> converter,
            ITypedMessagePublisher<TOutput> publisher)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _converter = converter ?? throw new ArgumentNullException(nameof(converter));
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
            _messageProcessing = metrics?.CreateSummary("message_processing", "Message processing duration in ms");
        }

        public async Task<ProcessedMessageStatus> HandleMessageAsync(MotorCloudEvent<TInput> dataCloudEvent, CancellationToken token = default)
        {
            try
            {
                var watch = new Stopwatch();
                watch.Start();
                IEnumerable<MotorCloudEvent<TOutput>> convertedMessages;
                try
                {
                    convertedMessages = await _converter.ConvertMessageAsync(dataCloudEvent, token)
                        .ConfigureAwait(false);
                }
                finally
                {
                    watch.Stop();
                    _messageProcessing?.Observe(watch.ElapsedMilliseconds);
                }

                foreach (var publishEvent in convertedMessages.Where(t => t.Data != null))
                {
                    await _publisher.PublishMessageAsync(publishEvent, token)
                        .ConfigureAwait(false);
                }

                return ProcessedMessageStatus.Success;
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception e)
            {
                _logger.LogError(LogEvents.ProcessingFailed, e, $"Processing failed.");
                return ProcessedMessageStatus.TemporaryFailure;
            }
        }
    }
}
