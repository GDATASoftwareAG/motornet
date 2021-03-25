using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Motor.Extensions.Diagnostics.Metrics;
using Motor.Extensions.Diagnostics.Metrics.Abstractions;
using Motor.Extensions.Hosting.Abstractions;
using Prometheus.Client;

namespace Motor.Extensions.Hosting
{
    public class MultiOutputServiceAdapter<TInput, TOutput> : INoOutputService<TInput>
        where TInput : class
        where TOutput : class
    {
        private readonly IMultiOutputService<TInput, TOutput> _converter;
        private readonly ILogger<SingleOutputServiceAdapter<TInput, TOutput>> _logger;
        private readonly ISummary? _messageProcessing;
        private readonly ITypedMessagePublisher<TOutput> _publisher;

        public MultiOutputServiceAdapter(ILogger<SingleOutputServiceAdapter<TInput, TOutput>> logger,
            IMetricsFactory<SingleOutputServiceAdapter<TInput, TOutput>>? metrics,
            IMultiOutputService<TInput, TOutput> converter,
            ITypedMessagePublisher<TOutput> publisher)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _converter = converter ?? throw new ArgumentNullException(nameof(converter));
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
            _messageProcessing = metrics?.CreateSummary("message_processing", "Message processing duration in ms");
        }

        public async Task<ProcessedMessageStatus> HandleMessageAsync(MotorCloudEvent<TInput> dataCloudEvent,
            CancellationToken token = default)
        {
            try
            {
                using (new AutoObserveStopwatch(_messageProcessing))
                {
                    await foreach (var message in _converter.ConvertMessageAsync(dataCloudEvent, token)
                        .ConfigureAwait(false).WithCancellation(token))
                    {
                        if (message?.Data is not null)
                        {
                            await _publisher.PublishMessageAsync(message, token)
                                .ConfigureAwait(false);
                        }
                    }
                }

                return ProcessedMessageStatus.Success;
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception e)
            {
                _logger.LogError(LogEvents.ProcessingFailed, e, "Processing failed.");
                return ProcessedMessageStatus.TemporaryFailure;
            }
        }
    }
}
