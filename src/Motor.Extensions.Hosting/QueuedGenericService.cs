using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using Motor.Extensions.Hosting.Internal;

namespace Motor.Extensions.Hosting;

public class QueuedGenericService<TInput> : BackgroundService
    where TInput : class
{
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly ILogger<QueuedGenericService<TInput>> _logger;
    private readonly QueuedGenericServiceOptions _options;
    private readonly IBackgroundTaskQueue<MotorCloudEvent<TInput>> _queue;
    private readonly INoOutputService<TInput> _rootService;

    // ReSharper disable once SuggestBaseTypeForParameter
    public QueuedGenericService(
        ILogger<QueuedGenericService<TInput>> logger,
        IOptions<QueuedGenericServiceOptions>? options,
        IHostApplicationLifetime hostApplicationLifetime,
        IBackgroundTaskQueue<MotorCloudEvent<TInput>> queue,
        BaseDelegatingMessageHandler<TInput> rootService
    )
    {
        _logger = logger;
        _options = options?.Value ?? new QueuedGenericServiceOptions();
        _hostApplicationLifetime = hostApplicationLifetime;
        _queue = queue;
        _rootService = rootService;
    }

    protected override async Task ExecuteAsync(CancellationToken token)
    {
        var optionsParallelProcesses = _options.ParallelProcesses ?? Environment.ProcessorCount;
        await Task.WhenAll(Enumerable.Repeat(0, optionsParallelProcesses).Select(_ => CreateRunnerTaskAsync(token)))
            .ConfigureAwait(false);
    }

    private Task CreateRunnerTaskAsync(CancellationToken token)
    {
        return Task.Run(
            async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    var queueItem = await _queue.DequeueAsync(token).ConfigureAwait(false);
                    if (queueItem is null)
                    {
                        continue;
                    }

                    await HandleSingleMessageAsync(queueItem.Item, queueItem.TaskCompletionStatus, token)
                        .ConfigureAwait(false);
                }
            },
            token
        );
    }

    private async Task HandleSingleMessageAsync(
        MotorCloudEvent<TInput> dataCloudEvent,
        TaskCompletionSource<ProcessedMessageStatus> taskCompletionSource,
        CancellationToken token
    )
    {
        var status = ProcessedMessageStatus.CriticalFailure;
        try
        {
            status = await _rootService.HandleMessageAsync(dataCloudEvent, token).ConfigureAwait(false);
        }
        catch (OperationCanceledException e)
        {
            _logger.LogWarning(LogEvents.ServiceWasStopped, e, "Service was already stopped");
            status = ProcessedMessageStatus.TemporaryFailure;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(
                LogEvents.UnexpectedErrorOnMessageProcessing,
                ex,
                "HandleMessage processed failed to with an unexpected exception."
            );
        }
        finally
        {
            taskCompletionSource.TrySetResult(status);
            if (status == ProcessedMessageStatus.CriticalFailure)
            {
                _hostApplicationLifetime.StopApplication();
            }
        }
    }
}
