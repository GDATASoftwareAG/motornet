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
    extension(IHostBuilder builder)
    {
        public IHostBuilder ConfigureDefaultHttpClient(string configSection = "Request") =>
            builder.ConfigureServices(
                (context, services) =>
                {
                    services
                        .AddTransient<PrometheusDelegatingHandler>()
                        .Configure<HttpOptions>(context.Configuration.GetSection(configSection))
                        .AddHttpClient(Options.DefaultName);
                }
            );
    }

    extension(IServiceCollection services)
    {
        public IHttpClientBuilder AddDefaultHttpClient(string name) =>
            services.AddHttpClient(name).AddDefaultBehaviour();

        public IHttpClientBuilder AddDefaultHttpClient<TClient, TImplementation>()
            where TClient : class
            where TImplementation : class, TClient =>
            services.AddHttpClient<TClient, TImplementation>().AddDefaultBehaviour();
    }

    extension(IHttpClientBuilder clientBuilder)
    {
        private IHttpClientBuilder AddDefaultBehaviour() =>
            clientBuilder
                .AddPolicyHandler(
                    (provider, _) =>
                    {
                        var config = (IOptions<HttpOptions>?)provider.GetService(typeof(IOptions<HttpOptions>));
                        var nonTransientStatusCodesToRetry =
                            config?.Value.NonTransientStatusCodesToRetry ?? Array.Empty<int>();
                        return HttpPolicyExtensions
                            .HandleTransientHttpError()
                            .Or<TimeoutRejectedException>() // thrown by Polly's TimeoutPolicy if the inner call times out
                            .OrResult(r => nonTransientStatusCodesToRetry.Contains((int)r.StatusCode))
                            .WaitAndRetryAsync(
                                config?.Value.NumberOfRetries ?? HttpOptions.DefaultNumberOfRetries,
                                i => TimeSpan.FromSeconds(Math.Pow(2, i))
                            );
                    }
                )
                .AddPolicyHandler(
                    (provider, _) =>
                    {
                        var config = (IOptions<HttpOptions>?)provider.GetService(typeof(IOptions<HttpOptions>));
                        return Policy.TimeoutAsync<HttpResponseMessage>(
                            config?.Value.TimeoutInSeconds ?? HttpOptions.DefaultTimeoutInSeconds
                        );
                    }
                )
                .AddHttpMessageHandler<PrometheusDelegatingHandler>();
    }
}
