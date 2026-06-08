using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Motor.Extensions.Utilities.Abstractions;

public interface IMotorHostBuilder : IHostBuilder
{
    new IMotorHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate);
    IMotorHostBuilder UseStartup<T>()
        where T : IMotorStartup;
}
