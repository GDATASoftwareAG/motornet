using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace Motor.Extensions.Http;

public static class DefaultHostBuilderExtensions
{
    public static IHostBuilder ConfigureDefaultHttpClient(this IHostBuilder hostBuilder,
        string configSection = "Request")
    {
        return hostBuilder
            .ConfigureServices((context, services) =>
            {
                services
                    .AddTransient<PrometheusDelegatingHandler>()
                    .Configure<HttpOptions>(context.Configuration.GetSection(configSection))
                    .AddHttpClient(Options.DefaultName);
            });
    }

    public static IHttpClientBuilder AddDefaultHttpClient(this IServiceCollection services, string name)
    {
        return services.AddHttpClient(name).AddDefaultBehaviour();
    }

    public static IHttpClientBuilder AddDefaultHttpClient<TClient, TImplementation>(
        this IServiceCollection services)
        where TClient : class
        where TImplementation : class, TClient
    {
        return services.AddHttpClient<TClient, TImplementation>().AddDefaultBehaviour();
    }

    private static IHttpClientBuilder AddDefaultBehaviour(this IHttpClientBuilder clientBuilder)
    {
        return clientBuilder
            .AddPolicyHandler((provider, _) =>
            {
                var config = (IOptions<HttpOptions>?)provider.GetService(typeof(IOptions<HttpOptions>));
                return HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .Or<TimeoutRejectedException>() // thrown by Polly's TimeoutPolicy if the inner call times out
                    .WaitAndRetryAsync(
                        config?.Value.NumberOfRetries ?? HttpOptions.DefaultNumberOfRetries,
                        i => TimeSpan.FromSeconds(Math.Pow(2, i)));
            })
            .AddPolicyHandler((provider, _) =>
            {
                var config = (IOptions<HttpOptions>?)provider.GetService(typeof(IOptions<HttpOptions>));
                return Policy.TimeoutAsync<HttpResponseMessage>(config?.Value.TimeoutInSeconds ??
                                                                HttpOptions.DefaultTimeoutInSeconds);
            })
            .AddHttpMessageHandler<PrometheusDelegatingHandler>();
    }
}
