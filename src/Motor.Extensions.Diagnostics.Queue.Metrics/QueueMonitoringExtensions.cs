using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Motor.Extensions.Diagnostics.Metrics;
using Motor.Extensions.Diagnostics.Queue.Abstractions;

namespace Motor.Extensions.Diagnostics.Queue.Metrics
{
    public static class QueueMonitoringExtensions
    {
        public static IApplicationBuilder UseQueueMonitoring(this IApplicationBuilder app)
        {
            var motorsFactory = app.ApplicationServices.GetService<IMotorMetricsFactory>();
            var queueMonitors = app.ApplicationServices.GetServices<IQueueMonitor>();
            motorsFactory?.CollectorRegistry.GetOrAdd(
                QueueMonitorMetricsCollector.QueueMonitorCollectorConfig,
                _ => new QueueMonitorMetricsCollector(queueMonitors)
            );

            return app;
        }
    }
}
