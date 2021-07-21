using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Motor.Extensions.Diagnostics.Logging;
using Motor.Extensions.Diagnostics.Metrics;
using Motor.Extensions.Diagnostics.Telemetry;
using Motor.Extensions.Hosting;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using Motor.Extensions.Http;
using Motor.Extensions.Utilities.Abstractions;

namespace Motor.Extensions.Utilities
{
    public static class MotorHost
    {
        private static IMotorHostBuilder ToMotorHostBuilder(this IHostBuilder hostBuilder,
            bool enableConfigureWebDefaults = true)
        {
            return new MotorHostBuilder(hostBuilder, enableConfigureWebDefaults);
        }

        public static IMotorHostBuilder CreateDefaultBuilder(Assembly? assembly = null,
            bool enableConfigureWebDefaults = true)
        {
            assembly ??= Assembly.GetCallingAssembly();

            var hostBuilder = Host.CreateDefaultBuilder()
                .ToMotorHostBuilder(enableConfigureWebDefaults);

            return (IMotorHostBuilder)hostBuilder
                .ConfigureServices(collection =>
                {
                    collection.AddTransient<IApplicationNameService>(_ =>
                        new DefaultApplicationNameService(assembly));
                })
                .ConfigureSerilog()
                .ConfigureOpenTelemetry()
                .ConfigurePrometheus()
                .ConfigureDefaultHttpClient()
                .UseConsoleLifetime();
        }
    }
}
