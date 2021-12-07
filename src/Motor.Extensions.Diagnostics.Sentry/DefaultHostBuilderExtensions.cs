using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Motor.Extensions.Utilities.Abstractions;
using Sentry;
using Sentry.Serilog;
using MSOptions = Microsoft.Extensions.Options.Options;

namespace Motor.Extensions.Diagnostics.Sentry;

public static class DefaultHostBuilderExtensions
{
    public static IMotorHostBuilder ConfigureSentry(this IMotorHostBuilder hostBuilder)
    {
        return hostBuilder
            .ConfigureServices((context, services) =>
            {
                var sentryOptions = new SentrySerilogOptions();
                context.Configuration.GetSection("Sentry").Bind(sentryOptions);
                SentrySdk.Init(sentryOptions);
                services.AddTransient<IOptions<SentrySerilogOptions>>(_ => MSOptions.Create(sentryOptions));
            });
    }
}
