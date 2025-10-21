using System;
using System.Threading.Tasks;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using Quartz;

namespace Motor.Extensions.Hosting.Timer;

internal class TimerJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var dataMap = context.JobDetail.JobDataMap;
        var job =
            dataMap["Queue"] as IBackgroundTaskQueue<MotorCloudEvent<IJobExecutionContext>>
            ?? throw new ArgumentNullException("context.JobDetail.JobDataMap[\"Queue\"]");
        var applicationNameService =
            dataMap["ApplicationNameService"] as IApplicationNameService
            ?? throw new ArgumentNullException("context.JobDetail.JobDataMap[\"ApplicationNameService\"]");
        await job.QueueBackgroundWorkItem(
            new MotorCloudEvent<IJobExecutionContext>(applicationNameService, context, new Uri("timer://notset"))
        );
    }
}
