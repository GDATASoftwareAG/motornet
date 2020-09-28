using Microsoft.Extensions.DependencyInjection;
using Motor.Extensions.Hosting.Timer.Config;
using Motor.Extensions.Utilities;
using Motor.Extensions.Utilities.Abstractions;
using Quartz;

namespace Motor.Extensions.Hosting.Timer
{
    public static class TimerHostBuilderExtensions
    {
        public static IMotorHostBuilder ConfigureTimer(this IMotorHostBuilder hostBuilder,
            string configSection = "Timer")
        {
            return hostBuilder
                .ConfigureDefaultGenericService<IJobExecutionContext>()
                .ConfigureServices((hostContext, services) =>
                {
                    var config = hostContext.Configuration.GetSection(configSection);
                    services.Configure<TimerConfig>(config);
                    services.AddHostedService<Timer>();
                });
        }
    }
}
