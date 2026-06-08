using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Motor.Extensions.Utilities;
using Quartz;

namespace Motor.Extensions.Hosting.Timer;

public static class TimerHostBuilderExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder ConfigureTimer(string configSection = "Timer")
        {
            builder.ConfigureNoOutputService<IJobExecutionContext>();
            var config = builder.Configuration.GetSection(configSection);
            builder.Services.Configure<TimerOptions>(config);
            builder.Services.AddHostedService<Timer>();

            return builder;
        }
    }

    extension(IHostBuilder builder)
    {
        public IHostBuilder ConfigureTimer(string configSection = "Timer") =>
            builder
                .ConfigureNoOutputService<IJobExecutionContext>()
                .ConfigureServices(
                    (hostContext, services) =>
                    {
                        var config = hostContext.Configuration.GetSection(configSection);
                        services.Configure<TimerOptions>(config);
                        services.AddHostedService<Timer>();
                    }
                );
    }
}
