using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Sentry;
using Sentry.Serilog;
using MSOptions = Microsoft.Extensions.Options.Options;

namespace Motor.Extensions.Diagnostics.Sentry;

public static class DefaultHostBuilderExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder ConfigureSentry()
        {
            var sentryOptions = new SentrySerilogOptions();
            builder.Configuration.GetSection("Sentry").Bind(sentryOptions);
            sentryOptions.Dsn ??= string.Empty;
            SentrySdk.Init(sentryOptions);
            builder.Services.AddTransient<IOptions<SentrySerilogOptions>>(_ => MSOptions.Create(sentryOptions));

            return builder;
        }
    }

    extension(IHostBuilder builder)
    {
        public IHostBuilder ConfigureSentry() =>
            builder.ConfigureServices(
                (context, services) =>
                {
                    var sentryOptions = new SentrySerilogOptions();
                    context.Configuration.GetSection("Sentry").Bind(sentryOptions);
                    sentryOptions.Dsn ??= string.Empty;
                    SentrySdk.Init(sentryOptions);
                    services.AddTransient<IOptions<SentrySerilogOptions>>(_ => MSOptions.Create(sentryOptions));
                }
            );
    }
}
