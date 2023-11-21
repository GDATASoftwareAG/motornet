using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;

namespace Motor.Extensions.Hosting;

public class MultiOutputServiceAdapter<TInput, TOutput> : INoOutputService<TInput>
    where TInput : class
    where TOutput : class
{
    private readonly IMultiOutputService<TInput, TOutput> _converter;
    private readonly ILogger<SingleOutputServiceAdapter<TInput, TOutput>> _logger;
    private readonly ITypedMessagePublisher<TOutput> _publisher;

    public MultiOutputServiceAdapter(ILogger<SingleOutputServiceAdapter<TInput, TOutput>> logger,
        IMultiOutputService<TInput, TOutput> converter,
        ITypedMessagePublisher<TOutput> publisher)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _converter = converter ?? throw new ArgumentNullException(nameof(converter));
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
    }

    public async Task<ProcessedMessageStatus> HandleMessageAsync(MotorCloudEvent<TInput> dataCloudEvent,
        CancellationToken token = default)
    {
        try
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

            return ProcessedMessageStatus.Success;
        }
        catch (OperationCanceledException e)
        {
            _logger.LogWarning(LogEvents.ServiceWasStopped, e, "Service was already stopped");
            return ProcessedMessageStatus.TemporaryFailure;
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (FailureException e)
        {
            _logger.LogError(LogEvents.ProcessingFailed, e, "Message processing failed");
            return ProcessedMessageStatus.Failure;
        }
        catch (Exception e)
        {
            _logger.LogError(LogEvents.ProcessingFailed, e, "Processing failed.");
            return ProcessedMessageStatus.TemporaryFailure;
        }
    }
}
