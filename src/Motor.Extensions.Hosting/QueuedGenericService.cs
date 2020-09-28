using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.Internal;

namespace Motor.Extensions.Hosting
{
    public class QueuedGenericService<TInput> : BackgroundService
        where TInput : class
    {
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly ILogger<QueuedGenericService<TInput>> _logger;
        private readonly QueuedGenericServiceConfig _options;
        private readonly IBackgroundTaskQueue<MotorCloudEvent<TInput>> _queue;
        private readonly IMessageHandler<TInput> _rootMessageHandler;

        // ReSharper disable once SuggestBaseTypeForParameter
        public QueuedGenericService(
            ILogger<QueuedGenericService<TInput>> logger,
            IOptions<QueuedGenericServiceConfig>? options,
            IHostApplicationLifetime hostApplicationLifetime,
            IBackgroundTaskQueue<MotorCloudEvent<TInput>> queue,
            BaseDelegatingMessageHandler<TInput> rootMessageHandler)
        {
            _logger = logger;
            _options = options?.Value ?? new QueuedGenericServiceConfig();
            _hostApplicationLifetime = hostApplicationLifetime;
            _queue = queue;
            _rootMessageHandler = rootMessageHandler;
        }

        protected override async Task ExecuteAsync(CancellationToken token)
        {
            var tasks = new List<Task>();
            var optionsParallelProcesses = _options.ParallelProcesses ?? Environment.ProcessorCount;
            for (var i = 0; i < optionsParallelProcesses; i++) tasks.Add(CreateRunnerTaskAsync(token));

            await Task.WhenAll(tasks);
        }

        private Task CreateRunnerTaskAsync(CancellationToken token)
        {
            return Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    var queueItem = await _queue.DequeueAsync(token)
                        .ConfigureAwait(false);
                    await HandleSingleMessageAsync(queueItem.Item, queueItem.TaskCompletionStatus, token)
                        .ConfigureAwait(false);
                }
            }, token);
        }

        private async Task HandleSingleMessageAsync(MotorCloudEvent<TInput> dataCloudEvent,
            TaskCompletionSource<ProcessedMessageStatus> taskCompletionSource, CancellationToken token)
        {
            var status = ProcessedMessageStatus.CriticalFailure;
            try
            {
                status = await _rootMessageHandler.HandleMessageAsync(dataCloudEvent, token)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(LogEvents.UnexpectedErrorOnMessageProcessing, ex,
                    "HandleMessage processed failed to with an unexpected exception.");
            }
            finally
            {
                taskCompletionSource?.TrySetResult(status);
                if (status == ProcessedMessageStatus.CriticalFailure) _hostApplicationLifetime.StopApplication();
            }
        }
    }
}
