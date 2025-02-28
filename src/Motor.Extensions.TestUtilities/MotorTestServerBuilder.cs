using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Motor.Extensions.Utilities;
using Motor.Extensions.Utilities.Abstractions;
using NSubstitute;

namespace Motor.Extensions.TestUtilities;

public class MotorTestServerBuilder<TStartup> where TStartup : IMotorStartup
{
    private readonly List<Action<IServiceCollection>> _overrideDependencies = new();

    public MotorTestServerBuilder<TStartup> SubstituteTransient<TInterface>(Action<TInterface>? setup = null)
        where TInterface : class => SubstituteService(ServiceDescriptor.Transient, setup);

    public MotorTestServerBuilder<TStartup> SubstituteScoped<TInterface>(Action<TInterface>? setup = null)
        where TInterface : class => SubstituteService(ServiceDescriptor.Scoped, setup);

    public MotorTestServerBuilder<TStartup> SubstituteSingleton<TInterface>(Action<TInterface>? setup = null)
        where TInterface : class => SubstituteService(ServiceDescriptor.Singleton, setup);

    public MotorTestServerBuilder<TStartup> SubstituteService<TInterface>(
        Func<Func<IServiceProvider, TInterface>, ServiceDescriptor> sd, Action<TInterface>? setup = null)
        where TInterface : class
    {
        var substitute = Substitute.For<TInterface>();
        setup?.Invoke(substitute);
        return Replace(sd(_ => substitute));
    }

    public MotorTestServerBuilder<TStartup> Replace(ServiceDescriptor replacement)
    {
        _overrideDependencies.Add(services => services.Replace(replacement));
        return this;
    }

    public TestServer Build()
    {
        var webHostBuilder = new WebHostBuilder();
        var useSetting = new Dictionary<string, string>
        {
            { MotorHostDefaults.EnablePrometheusEndpointKey, false.ToString() }
        };

        MotorHostBuilderHelper.ConfigureWebHost(webHostBuilder, s => useSetting.GetValueOrDefault(s), typeof(TStartup));

        webHostBuilder.UseEnvironment("Development");
        webHostBuilder.ConfigureServices(services =>
        {
            services.AddHealthChecks();
            _overrideDependencies.ForEach(overrideDep => overrideDep(services));
        });

        return new TestServer(webHostBuilder);
    }
}
