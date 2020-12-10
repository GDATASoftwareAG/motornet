using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Motor.Extensions.Utilities;
using Motor.Extensions.Utilities.Abstractions;
using Xunit;

namespace Motor.Extensions.TestUtilities
{
    public class MotorHostApplicationFactory<TStartup> : IAsyncLifetime where TStartup : IMotorStartup
    {
        private TestServer? server;

        public async Task InitializeAsync()
        {
            var useSetting = new Dictionary<string, string>
            {
                {MotorHostDefaults.EnablePrometheusEndpointKey, false.ToString()}
            };

            var webHostBuilder = new WebHostBuilder();
            MotorHostBuilderHelper.ConfigureWebHost(webHostBuilder, s => useSetting.GetValueOrDefault(s), typeof(TStartup));
            webHostBuilder.ConfigureServices(collection =>
            {
                collection.AddHealthChecks();
            });
            
            server = new TestServer(webHostBuilder);
        }

        public HttpClient CreateClient() => server?.CreateClient()!;

        public async Task DisposeAsync()
        {
            server?.Dispose();
        }
    }
}
