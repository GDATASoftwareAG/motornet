using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Motor.Extensions.TestUtilities;
using Motor.Extensions.Utilities.Abstractions;
using Xunit;

namespace Motor.Extensions.Utilities_IntegrationTest
{
    public class TestService
    {
    }

    public class HomeController : Controller
    {
        private TestService service;

        public HomeController(TestService testService)
        {
            service = testService ?? throw new ArgumentNullException();
        }

        // 
        // GET: /
        public string Index()
        {
            return "This is my default action...";
        }
    }

    public class TestStartup : IMotorStartup
    {
        public void ConfigureServices(WebHostBuilderContext context, IServiceCollection services)
        {
            services.AddControllers();
            services.AddTransient<TestService>();
        }

        public void Configure(WebHostBuilderContext context, IApplicationBuilder app)
        {
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }


    public class DefaultHostingTest : IClassFixture<MotorHostApplicationFactory<TestStartup>>
    {
        private readonly MotorHostApplicationFactory<TestStartup> _factory;

        public DefaultHostingTest(MotorHostApplicationFactory<TestStartup> factory)
        {
            _factory = factory;
        }

        [Theory]
        [InlineData("/")]
        public async Task Get_EndpointsReturnSuccessAndCorrectContentType(string url)
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync(url).ConfigureAwait(false);

            // Assert
            response.EnsureSuccessStatusCode(); // Status Code 200-299
        }
    }
}
