using System;
using System.Collections.Generic;
using System.Linq;
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

    public MotorTestHost<TStartup> ConfigureServices(Action<IServiceCollection> action)
    {
        _overrideDependencies.Add(action);
        return this;
    }

    private MotorTestHost<TStartup> Replace(ServiceDescriptor replacement)
    {
        _overrideDependencies.Add(services => services.Replace(replacement));
        return this;
    }

    public MotorTestHost<TStartup> ReplaceTransient<TInterface, TOldInstance, TNewInstance>() where TNewInstance : class => Replace<TInterface, TOldInstance>(new ServiceDescriptor(typeof(TInterface), typeof(TNewInstance), ServiceLifetime.Transient));

    public MotorTestHost<TStartup> ReplaceScoped<TInterface, TOldInstance, TNewInstance>() where TNewInstance : class => Replace<TInterface, TOldInstance>(new ServiceDescriptor(typeof(TInterface), typeof(TNewInstance), ServiceLifetime.Scoped));

    public MotorTestHost<TStartup> ReplaceSingleton<TInterface, TOldInstance, TNewInstance>() where TNewInstance : class => Replace<TInterface, TOldInstance>(new ServiceDescriptor(typeof(TInterface), typeof(TNewInstance), ServiceLifetime.Singleton));

    private MotorTestHost<TStartup> Replace<TInterface, TOldInstance>(ServiceDescriptor newServiceDescriptor)
    {
        _overrideDependencies.Add(services =>
        {
            var oldServiceDescriptor = services.SingleOrDefault(s => s.Lifetime == newServiceDescriptor.Lifetime && s.ServiceType == typeof(TInterface) && s.ImplementationType == typeof(TOldInstance));
            if (oldServiceDescriptor is not null)
            {
                services.Remove(oldServiceDescriptor);
                services.Add(newServiceDescriptor);
            }
            else
            {
                throw new InvalidOperationException($"Could not find {newServiceDescriptor.Lifetime} service descriptor for {typeof(TInterface)} and {typeof(TOldInstance)}");
            }
        });
        return this;
    }

    public MotorTestHost<TStartup> Configure<TOptions>(Action<TOptions> configuration) where TOptions : class
    {
        _serviceConfiguration.Add(services => services.Configure(configuration));
        return this;
    }
}
