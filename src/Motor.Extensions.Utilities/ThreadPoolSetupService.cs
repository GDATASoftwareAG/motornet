using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Motor.Extensions.Utilities;

public record ThreadPoolOptions
{
    public int MinWorkerThreads { get; init; } = Environment.ProcessorCount;
    public int MinCompletionThreads { get; init; } = Environment.ProcessorCount;
}

public sealed class ThreadPoolSetupService : IHostedService
{
    private readonly IOptions<ThreadPoolOptions> _threadPoolOptions;


    public ThreadPoolSetupService(IOptions<ThreadPoolOptions> threadPoolOptions)
    {
        _threadPoolOptions = threadPoolOptions;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        SetupGlobalThreadPool(_threadPoolOptions.Value.MinWorkerThreads,
            _threadPoolOptions.Value.MinCompletionThreads);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    private static void SetupGlobalThreadPool(int desiredMinWorkerThreads, int desiredMinCompletionThreads)
    {
        if (desiredMinWorkerThreads <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(desiredMinWorkerThreads), desiredMinWorkerThreads, "Minimum number of worker threads must be positive");
        }

        if (desiredMinCompletionThreads <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(desiredMinCompletionThreads), desiredMinCompletionThreads, "Minimum number of completion port threads must be positive");
        }

        ThreadPool.GetMinThreads(out var minWorkerThreads, out var minCompletionThreads);
        ThreadPool.GetMaxThreads(out var maxWorkerThreads, out var maxCompletionThreads);

        desiredMinWorkerThreads = Math.Min(desiredMinWorkerThreads, maxWorkerThreads);
        desiredMinCompletionThreads = Math.Min(desiredMinCompletionThreads, maxCompletionThreads);

        desiredMinWorkerThreads = Math.Max(minWorkerThreads, desiredMinWorkerThreads);
        desiredMinCompletionThreads = Math.Max(minCompletionThreads, desiredMinCompletionThreads);
        ThreadPool.SetMinThreads(desiredMinWorkerThreads, desiredMinCompletionThreads);
    }
}
