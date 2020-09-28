using System.Reflection;
using Motor.Extensions.Diagnostics.Logging;
using Motor.Extensions.Diagnostics.Metrics;
using Motor.Extensions.Diagnostics.Tracing;
using Motor.Extensions.Hosting;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Http;
using Motor.Extensions.Utilities.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Motor.Extensions.Utilities
{
    public static class MotorHost
    {
        private static IMotorHostBuilder ToMotorHostBuilder(this IHostBuilder hostBuilder, bool enableConfigureWebDefaults = true)
        {
            return new MotorHostBuilder(hostBuilder, enableConfigureWebDefaults);
        }

        public static IMotorHostBuilder CreateDefaultBuilder(Assembly? assembly = null, bool enableConfigureWebDefaults = true)
        {
            assembly ??= Assembly.GetCallingAssembly();

            var hostBuilder = Host.CreateDefaultBuilder()
                .ToMotorHostBuilder(enableConfigureWebDefaults);

            return (IMotorHostBuilder)hostBuilder
                .ConfigureServices(collection =>
                {
                    collection.AddTransient<IApplicationNameService>(provider =>
                        new DefaultApplicationNameService(assembly));
                })
                .ConfigureSerilog()
                .ConfigureJaegerTracing()
                .ConfigurePrometheus()
                .ConfigureDefaultHttpClient()
                .UseConsoleLifetime();
        }
    }
}
