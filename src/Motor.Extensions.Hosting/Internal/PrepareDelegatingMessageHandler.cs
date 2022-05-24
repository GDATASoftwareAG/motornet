using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;

namespace Motor.Extensions.Hosting.Internal;

public class PrepareDelegatingMessageHandler<TInput> : DelegatingMessageHandler<TInput>
    where TInput : class
{
    private readonly ILogger<PrepareDelegatingMessageHandler<TInput>> _logger;

    public PrepareDelegatingMessageHandler(ILogger<PrepareDelegatingMessageHandler<TInput>> logger)
    {
        _logger = logger;
    }

    public override async Task<ProcessedMessageStatus> HandleMessageAsync(MotorCloudEvent<TInput> dataCloudEvent,
        CancellationToken token = default)
    {
        ProcessedMessageStatus processedMessageStatus;
        try
        {
            processedMessageStatus = await base.HandleMessageAsync(dataCloudEvent, token)
                .ConfigureAwait(false);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(LogEvents.InvalidInput, ex, "Invalid input (first 100 chars): {message}",
                dataCloudEvent.Data?.ToString()?.Take(100));
            processedMessageStatus = ProcessedMessageStatus.InvalidInput;
        }
        catch (TemporaryFailureException ex)
        {
            switch (ex.Level)
            {
                case FailureLevel.Warning:
                    _logger.LogWarning(LogEvents.ProcessingFailed, ex, "Processing failed");
                    break;
                case FailureLevel.Error:
                    _logger.LogError(LogEvents.ProcessingFailed, ex, "Processing failed");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            processedMessageStatus = ProcessedMessageStatus.TemporaryFailure;
        }
        catch (FailureException e)
        {
            _logger.LogError(LogEvents.ProcessingFailed, e, "Message processing failed");
            return ProcessedMessageStatus.Failure;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(LogEvents.UnexpectedErrorOnMessageProcessing, ex,
                "Unexpected error on message processing.");

            processedMessageStatus = ProcessedMessageStatus.CriticalFailure;
        }

        return processedMessageStatus;
    }
}
