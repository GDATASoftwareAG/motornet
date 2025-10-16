using AspNetExample.Models;
using AspNetExample.Persistency;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Motor.Extensions.Utilities.Abstractions;
using Prometheus.Client.HttpRequestDurations;
using Scalar.AspNetCore;

namespace AspNetExample;
public class Startup : IMotorStartup
{
    private const string CorsPolicy = nameof(AspNetExample);
    public void ConfigureServices(WebHostBuilderContext context, IServiceCollection services)
    {
        // Register custom services for this application
        services.AddSingleton<ICustomerValidator, AtLeast18Validator>();
        services.AddSingleton<IStorage<Customer>, InMemoryStorage<Customer>>();

        // Register controllers for endpoints
        services.AddControllers();

        // Configure options for Kestrel Server from appsettings
        services.Configure<KestrelServerOptions>(context.Configuration.GetSection("Kestrel"));

#if NET9_0_OR_GREATER
        services.AddOpenApi();
#endif

        // Add example cross-origin policy
        services.AddCors(options => options.AddPolicy(
            CorsPolicy, builder => builder
                .WithOrigins("http://localhost:4200")
                .AllowAnyHeader()
                .SetIsOriginAllowedToAllowWildcardSubdomains())
        );
    }
    public void Configure(WebHostBuilderContext context, IApplicationBuilder app)
    {
        app.UseEndpoints(endpoints =>
        {
            // Make controllers for each endpoint available
            endpoints.MapControllers();

#if NET9_0_OR_GREATER
            // For non-production environments, make Scalar API reference available at /scalar/v1
            endpoints.MapOpenApi();
            endpoints.MapScalarApiReference(options =>
            {
                options.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
            });
#endif
        });

        // Add some Prometheus metrics for request durations, excluding specified endpoints
        app.UsePrometheusRequestDurations(q =>
        {
            q.IncludeMethod = true;
            q.IncludePath = true;
            q.Buckets = [0.01, 0.5, 1, 2.5, 5, 7.5, 10, 15, 20, 30, 45, 60, 90, 120];
            q.IgnoreRoutesConcrete = ["/health/", "/favicon.ico"];
        });

        // Use cross-origin policy defined above
        app.UseCors(CorsPolicy);

        // Show useful exceptions in development environment
        if (context.HostingEnvironment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/error");
        }
    }
}
