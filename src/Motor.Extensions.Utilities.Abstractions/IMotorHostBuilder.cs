using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace Motor.Extensions.Utilities.Abstractions
{
    public interface IMotorHostBuilder : IHostBuilder
    {
        new IMotorHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate);
        IMotorHostBuilder UseStartup<T>() where T : IMotorStartup;

        IMotorHostBuilder AddHealthCheck<T>(
            string name,
            HealthStatus? failureStatus = null,
            IEnumerable<string>? tags = null,
            TimeSpan? timeout = null);
    }
}
