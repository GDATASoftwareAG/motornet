using System;
using System.Threading;
using Microsoft.Extensions.Options;
using Motor.Extensions.Utilities;
using Xunit;

namespace Motor.Extensions.Utilities_UnitTest;

public class ThreadPoolSetupServiceTest
{
    [Fact]
    public async void TestStartAsync_DefaultSettings_EnvironmentProcessorCount()
    {
        ThreadPool.GetMinThreads(out var origMinWorkerThreads, out var origMinCompletionThreads);
        var threadPoolOptions = new ThreadPoolOptions();
        var threadPoolSetupSvc = new ThreadPoolSetupService(Options.Create(threadPoolOptions));

        await threadPoolSetupSvc.StartAsync();

        ThreadPool.GetMinThreads(out var newWorkerThreads, out var newCompletionThreads);
        Assert.Equal(Environment.ProcessorCount, newWorkerThreads);
        Assert.Equal(Environment.ProcessorCount, newCompletionThreads);

        /* Restore previous values */
        ThreadPool.SetMinThreads(origMinWorkerThreads, origMinCompletionThreads);
    }

    [Fact]
    public async void TestStartAsync_WorkerThreadCountNegative_ArgumentOutOfRangeException()
    {
        var threadPoolOptions = new ThreadPoolOptions { MinWorkerThreads = -1 };
        var threadPoolSetupSvc = new ThreadPoolSetupService(Options.Create(threadPoolOptions));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await threadPoolSetupSvc.StartAsync());
    }

    [Fact]
    public async void TestStartAsync_CompletionPortThreadCountNegative_ArgumentOutOfRangeException()
    {
        var threadPoolOptions = new ThreadPoolOptions { MinCompletionThreads = -1 };
        var threadPoolSetupSvc = new ThreadPoolSetupService(Options.Create(threadPoolOptions));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await threadPoolSetupSvc.StartAsync());
    }

    [Fact]
    public async void TestStartAsync_SettingsOverride_Override()
    {
        ThreadPool.GetMinThreads(out var origMinWorkerThreads, out var origMinCompletionThreads);
        var doubleProcCount = Environment.ProcessorCount * 2;
        var threadPoolOptions = new ThreadPoolOptions
        {
            MinWorkerThreads = doubleProcCount,
            MinCompletionThreads = doubleProcCount
        };
        var threadPoolSetupSvc = new ThreadPoolSetupService(Options.Create(threadPoolOptions));

        await threadPoolSetupSvc.StartAsync();

        ThreadPool.GetMinThreads(out var newWorkerThreads, out var newCompletionThreads);
        Assert.Equal(doubleProcCount, newWorkerThreads);
        Assert.Equal(doubleProcCount, newCompletionThreads);

        /* Restore previous values */
        ThreadPool.SetMinThreads(origMinWorkerThreads, origMinCompletionThreads);
    }

    [Fact]
    public async void TestStartAsync_GreaterThanMax_HonorMax()
    {
        ThreadPool.GetMinThreads(out var origMinWorkerThreads, out var origMinCompletionThreads);
        ThreadPool.GetMaxThreads(out var origMaxWorkerThreads, out var origMaxCompletionThreads);
        var syntheticMaxWorker = Environment.ProcessorCount;
        var syntheticMaxCompletion = Environment.ProcessorCount;
        ThreadPool.SetMaxThreads(syntheticMaxWorker, syntheticMaxCompletion);
        var doubleProcCount = Environment.ProcessorCount * 2;
        var threadPoolOptions = new ThreadPoolOptions
        {
            MinWorkerThreads = doubleProcCount,
            MinCompletionThreads = doubleProcCount
        };
        var threadPoolSetupSvc = new ThreadPoolSetupService(Options.Create(threadPoolOptions));

        await threadPoolSetupSvc.StartAsync();

        ThreadPool.GetMinThreads(out var newWorkerThreads, out var newCompletionThreads);
        Assert.Equal(syntheticMaxWorker, newWorkerThreads);
        Assert.Equal(syntheticMaxCompletion, newCompletionThreads);

        /* Restore previous values */
        ThreadPool.SetMinThreads(origMinWorkerThreads, origMinCompletionThreads);
        ThreadPool.SetMaxThreads(origMaxWorkerThreads, origMaxCompletionThreads);
    }
}
