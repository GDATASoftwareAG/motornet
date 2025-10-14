using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace Motor.Extensions.TestUtilities;

public static class MotorTestHost
{
    public static MotorTestHost<T> BasedOn<T>()
        where T : class
    {
        return new MotorTestHost<T>();
    }
}

public class MotorTestHost<TStartup>
    where TStartup : class
{
    private readonly List<Action<IServiceCollection>> _overrideDependencies = new();
    private readonly List<Action<IServiceCollection>> _serviceConfiguration = new();

    internal MotorTestHost() { }

    public MotorWebApplicationFactory<TStartup> Build() =>
        new(
            services => _overrideDependencies.ForEach(service => service.Invoke(services)),
            services => _serviceConfiguration.ForEach(service => service.Invoke(services))
        );

    public MotorTestHost<TStartup> SubstituteTransient<TInterface>(Action<TInterface>? setup = null)
        where TInterface : class => SubstituteService(ServiceDescriptor.Transient, setup);

    public MotorTestHost<TStartup> SubstituteScoped<TInterface>(Action<TInterface>? setup = null)
        where TInterface : class => SubstituteService(ServiceDescriptor.Scoped, setup);

    public MotorTestHost<TStartup> SubstituteSingleton<TInterface>(Action<TInterface>? setup = null)
        where TInterface : class => SubstituteService(ServiceDescriptor.Singleton, setup);

    private MotorTestHost<TStartup> SubstituteService<TInterface>(
        Func<Func<IServiceProvider, TInterface>, ServiceDescriptor> sd, Action<TInterface>? setup = null)
        where TInterface : class
    {
        var substitute = Substitute.For<TInterface>();
        setup?.Invoke(substitute);
        return Replace(sd(_ => substitute));
    }

    private MotorTestHost<TStartup> Replace(ServiceDescriptor replacement)
    {
        _overrideDependencies.Add(services => services.Replace(replacement));
        return this;
    }

    public MotorTestHost<TStartup> Configure<TOptions>(Action<TOptions> configuration) where TOptions : class
    {
        _serviceConfiguration.Add(services => services.Configure(configuration));
        return this;
    }
}
