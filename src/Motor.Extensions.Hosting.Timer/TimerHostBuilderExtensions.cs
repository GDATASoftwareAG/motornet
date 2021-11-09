using Microsoft.Extensions.DependencyInjection;
using Motor.Extensions.Utilities;
using Motor.Extensions.Utilities.Abstractions;
using Quartz;

namespace Motor.Extensions.Hosting.Timer;

public static class TimerHostBuilderExtensions
{
    public static IMotorHostBuilder ConfigureTimer(this IMotorHostBuilder hostBuilder,
        string configSection = "Timer")
    {
        return hostBuilder
            .ConfigureNoOutputService<IJobExecutionContext>()
            .ConfigureServices((hostContext, services) =>
            {
                var config = hostContext.Configuration.GetSection(configSection);
                services.Configure<TimerOptions>(config);
                services.AddHostedService<Timer>();
            });
    }
}
