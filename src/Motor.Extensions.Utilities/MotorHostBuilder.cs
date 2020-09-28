using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Motor.Extensions.Diagnostics.HealthChecks;
using Motor.Extensions.Diagnostics.Metrics;
using Motor.Extensions.Utilities.Abstractions;

namespace Motor.Extensions.Utilities
{
    public class MotorHostBuilder : IMotorHostBuilder
    {
        private readonly IHostBuilder _builder;
        private readonly IConfiguration _config;
        private readonly bool _enableConfigureWebDefaults;
        private readonly List<HealthCheckData> _healthChecks = new List<HealthCheckData>();
        private Type? _type;

        public MotorHostBuilder(IHostBuilder builder, bool enableConfigureWebDefaults = true)
        {
            _builder = builder;
            _enableConfigureWebDefaults = enableConfigureWebDefaults;

            _config = new ConfigurationBuilder()
                .AddEnvironmentVariables(MotorHostDefaults.OptionsPrefix)
                .Build();
        }

        public IHostBuilder ConfigureHostConfiguration(Action<IConfigurationBuilder> configureDelegate)
        {
            _builder.ConfigureHostConfiguration(configureDelegate);
            return this;
        }

        public IHostBuilder ConfigureAppConfiguration(
            Action<HostBuilderContext, IConfigurationBuilder> configureDelegate)
        {
            _builder.ConfigureAppConfiguration(configureDelegate);
            return this;
        }

        IMotorHostBuilder IMotorHostBuilder.ConfigureServices(
            Action<HostBuilderContext, IServiceCollection> configureDelegate)
        {
            _builder.ConfigureServices(configureDelegate);
            return this;
        }

        public IMotorHostBuilder UseStartup<T>() where T : IMotorStartup
        {
            _type = typeof(T);
            return this;
        }

        public IMotorHostBuilder AddHealthCheck<T>(
            string name,
            HealthStatus? failureStatus = null,
            IEnumerable<string>? tags = null,
            TimeSpan? timeout = null)
        {
            _healthChecks.Add(new HealthCheckData(typeof(T), name, failureStatus, tags, timeout));
            return this;
        }

        public IHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate)
        {
            _builder.ConfigureServices(configureDelegate);
            return this;
        }

        public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(
            IServiceProviderFactory<TContainerBuilder> factory)
        {
            _builder.UseServiceProviderFactory(factory);
            return this;
        }

        public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(
            Func<HostBuilderContext, IServiceProviderFactory<TContainerBuilder>> factory)
        {
            _builder.UseServiceProviderFactory(factory);
            return this;
        }

        public IHostBuilder ConfigureContainer<TContainerBuilder>(
            Action<HostBuilderContext, TContainerBuilder> configureDelegate)
        {
            _builder.ConfigureContainer(configureDelegate);
            return this;
        }

        public IHost Build()
        {
            if (_enableConfigureWebDefaults)
                _builder
                    .ConfigureWebHostDefaults(builder =>
                    {
                        IMotorStartup? startup = null;
                        if (_type != null) startup = Activator.CreateInstance(_type) as IMotorStartup;

                        var urls = builder.GetSetting(WebHostDefaults.ServerUrlsKey);
                        const string defaultUrl = "http://0.0.0.0:9110";
                        if (string.IsNullOrEmpty(urls))
                            builder.UseUrls(defaultUrl);
                        else if (!urls.Contains(defaultUrl)) builder.UseUrls($"{urls};{defaultUrl}");

                        builder.Configure((context, applicationBuilder) =>
                        {
                            applicationBuilder.UseRouting();
                            var enablePrometheusSetting = GetSetting(MotorHostDefaults.EnablePrometheusEndpointKey);
                            if (string.IsNullOrEmpty(enablePrometheusSetting) || bool.Parse(enablePrometheusSetting))
                                applicationBuilder.UsePrometheusServer();
                            startup?.Configure(context, applicationBuilder);
                            applicationBuilder.UseEndpoints(endpoints => { endpoints.MapHealthChecks("/health"); });
                        });
                        if (_type != null)
                            builder.UseSetting(WebHostDefaults.ApplicationKey, _type.Assembly?.GetName()?.Name);

                        builder.ConfigureServices((context, collection) =>
                        {
                            startup?.ConfigureServices(context, collection);
                        });
                    })
                    .ConfigureHealthChecks(builder =>
                    {
                        foreach (var healthCheck in _healthChecks)
                            builder.Add(new HealthCheckRegistration(
                                healthCheck.Name,
                                s => (IHealthCheck) ActivatorUtilities.GetServiceOrCreateInstance(s, healthCheck.Type),
                                healthCheck.FailureStatus,
                                healthCheck.Tags,
                                healthCheck.Timeout)
                            );
                    });

            return _builder.Build();
        }

        public IDictionary<object, object> Properties => _builder.Properties;

        public string? GetSetting(string key)
        {
            return _config[key];
        }

        public IMotorHostBuilder UseSetting(string key, string value)
        {
            _config[key] = value;
            return this;
        }

        private struct HealthCheckData
        {
            public readonly Type Type;
            public readonly string Name;
            public readonly HealthStatus? FailureStatus;
            public readonly IEnumerable<string> Tags;
            public readonly TimeSpan? Timeout;

            public HealthCheckData(
                Type type,
                string name,
                HealthStatus? failureStatus = null,
                IEnumerable<string>? tags = null,
                TimeSpan? timeout = null)
            {
                Type = type;
                Name = name;
                FailureStatus = failureStatus;
                Tags = tags ?? new List<string>();
                Timeout = timeout;
            }
        }
    }
}
