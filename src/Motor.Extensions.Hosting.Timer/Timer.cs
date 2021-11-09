using System;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using Quartz;
using Quartz.Impl;

namespace Motor.Extensions.Hosting.Timer;

public class Timer : BackgroundService
{
    private readonly IApplicationNameService _applicationNameService;
    private readonly TimerOptions _options;
    private readonly IBackgroundTaskQueue<MotorCloudEvent<IJobExecutionContext>> _queue;
    private IScheduler? _scheduler;
    private bool _started;

    public Timer(IOptions<TimerOptions> config,
        IBackgroundTaskQueue<MotorCloudEvent<IJobExecutionContext>> queue,
        IApplicationNameService applicationNameService)
    {
        _queue = queue;
        _applicationNameService = applicationNameService;
        _options = config.Value ?? throw new ArgumentNullException(nameof(config));
    }

    protected override async Task ExecuteAsync(CancellationToken token)
    {
        ThrowIfTimerAlreadyStarted();
        await ConfigureTimer().ConfigureAwait(false);
        StartTimer(token);
        // make function async
        await Task.Delay(1, token).ConfigureAwait(false);
        await Task.FromCanceled(token).ConfigureAwait(false);
        if (_scheduler is not null) await _scheduler.Shutdown(token);
    }

    private void StartTimer(CancellationToken token)
    {
        _scheduler?.Start(token);
        _started = true;
    }

    private void ThrowIfTimerAlreadyStarted()
    {
        if (_started)
            throw new InvalidOperationException("Cannot start timer as the timer was already started!");
    }

    private async Task ConfigureTimer()
    {
        var props = new NameValueCollection
            {
                {"quartz.serializer.type", "binary"}
            };
        var factory = new StdSchedulerFactory(props);
        var data = new JobDataMap
            {
                {"Queue", _queue},
                {"ApplicationNameService", _applicationNameService}
            };
        _scheduler = await factory.GetScheduler().ConfigureAwait(false);
        var job = JobBuilder.Create<TimerJob>()
            .SetJobData(data)
            .Build();

        var trigger = TriggerBuilder.Create()
            .StartNow()
            .WithCronSchedule(_options.GetCronString())
            .Build();

        await _scheduler.ScheduleJob(job, trigger).ConfigureAwait(false);
    }
}
