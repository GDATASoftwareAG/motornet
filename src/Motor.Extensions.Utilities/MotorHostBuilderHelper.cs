using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Motor.Extensions.Diagnostics.Metrics;
using Motor.Extensions.Utilities.Abstractions;

namespace Motor.Extensions.Utilities
{
    public static class MotorHostBuilderHelper
    {
        public static void ConfigureWebHost(IWebHostBuilder builder, Func<string, string?> getSetting, Type? motorStartup)
        {
            IMotorStartup? startup = null;
            if (motorStartup is not null) startup = Activator.CreateInstance(motorStartup) as IMotorStartup;

            var urls = builder.GetSetting(WebHostDefaults.ServerUrlsKey);
            const string defaultUrl = "http://0.0.0.0:9110";
            if (string.IsNullOrEmpty(urls))
                builder.UseUrls(defaultUrl);
            else if (!urls.Contains(defaultUrl)) builder.UseUrls($"{urls};{defaultUrl}");

            builder.Configure((context, applicationBuilder) =>
            {
                applicationBuilder.UseRouting();
                var enablePrometheusSetting = getSetting(MotorHostDefaults.EnablePrometheusEndpointKey);
                if (string.IsNullOrEmpty(enablePrometheusSetting) || bool.Parse(enablePrometheusSetting))
                    applicationBuilder.UsePrometheusServer();
                startup?.Configure(context, applicationBuilder);
                applicationBuilder.UseEndpoints(endpoints => { endpoints.MapHealthChecks("/health"); });
            });
            if (motorStartup is not null)
                builder.UseSetting(WebHostDefaults.ApplicationKey, motorStartup.Assembly.GetName().Name);

            builder.ConfigureServices((context, collection) =>
            {
                startup?.ConfigureServices(context, collection);
            });
        }
    }
}
