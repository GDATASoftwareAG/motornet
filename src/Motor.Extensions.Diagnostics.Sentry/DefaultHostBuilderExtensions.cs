using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Motor.Extensions.Utilities.Abstractions;
using Sentry;
using Sentry.Extensions.Logging;
using MSOptions = Microsoft.Extensions.Options.Options;

namespace Motor.Extensions.Diagnostics.Sentry;

public static class DefaultHostBuilderExtensions
{
    public static IMotorHostBuilder ConfigureSentry(this IMotorHostBuilder hostBuilder)
    {
        return hostBuilder
            .ConfigureServices((context, services) =>
            {
                var sentryOptions = new SentryLoggingOptions();
                context.Configuration.GetSection("Sentry").Bind(sentryOptions);
                SentrySdk.Init(sentryOptions);
                services.AddTransient<IOptions<SentryLoggingOptions>>(_ => MSOptions.Create(sentryOptions));
            });
    }
}
