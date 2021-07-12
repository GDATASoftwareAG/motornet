using MetricsExample.DifferentNamespace;
using MetricsExample.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Motor.Extensions.Diagnostics.Queue.Metrics;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Utilities.Abstractions;

namespace MetricsExample
{
    public class Startup : IMotorStartup
    {
        public void ConfigureServices(WebHostBuilderContext context, IServiceCollection services)
        {
            // Add a handler for the input message which returns an output message
            // This handler is called for every new incoming message
            services.AddTransient<INoOutputService<InputMessage>, ServiceWithMetrics>();
            services.AddTransient<IServiceInDifferentNamespace, ServiceInDifferentNamespace>();
        }

        public void Configure(WebHostBuilderContext context, IApplicationBuilder builder)
        {
            builder.UseQueueMonitoring();
        }
    }
}
