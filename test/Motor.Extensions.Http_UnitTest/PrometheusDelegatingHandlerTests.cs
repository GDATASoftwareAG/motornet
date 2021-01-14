using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Motor.Extensions.Diagnostics.Metrics;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Http;
using Motor.Extensions.Utilities;
using Xunit;

namespace Motor.Extensions.Http_UnitTest
{
    [Collection("GenericHosting")]
    public class PrometheusDelegatingHandlerTests
    {
        [Fact]
        public void GetHttpClient_GetTwoDifferentHttpClients()
        {
            var hostBuilder = new MotorHostBuilder(new HostBuilder())
                .ConfigurePrometheus()
                .ConfigureDefaultHttpClient()
                .ConfigureServices((_, services) =>
                {
                    services.AddTransient(_ =>
                    {
                        var mock = new Mock<IApplicationNameService>();
                        mock.Setup(t => t.GetVersion()).Returns("test");
                        mock.Setup(t => t.GetLibVersion()).Returns("test");
                        return mock.Object;
                    });
                    services.AddDefaultHttpClient("test1");
                    services.AddDefaultHttpClient("test2");
                });
            var httpClientFactory = hostBuilder.Build().Services.GetService<IHttpClientFactory>();
            var httpClient1 = httpClientFactory.CreateClient("test1");
            var httpClient2 = httpClientFactory.CreateClient("test2");

            Assert.NotNull(httpClient1);
            Assert.NotNull(httpClient2);
        }
    }
}
