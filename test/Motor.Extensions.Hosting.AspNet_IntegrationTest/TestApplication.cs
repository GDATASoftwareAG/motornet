using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Motor.Extensions.Hosting.AspNet_IntegrationTest;
using Motor.Extensions.Utilities;
using Motor.Extensions.Utilities.Abstractions;

await MotorHost.CreateDefaultBuilder()
    .UseStartup<ActualStartup>()
    .RunConsoleAsync();

namespace Motor.Extensions.Hosting.AspNet_IntegrationTest
{
    public interface IRandomNumberGenerator
    {
        public int Next();
    }

    public class ActualRandomNumberGenerator : IRandomNumberGenerator
    {
        public int Next() => Random.Shared.Next();
    }

    [ApiController]
    [Route("[controller]")]
    public class RandomNumberController(IRandomNumberGenerator randomNumberGenerator) : Controller
    {
        [HttpGet]
        public string RandomNumber()
        {
            return randomNumberGenerator.Next().ToString();
        }
    }

    public class ActualStartup : IMotorStartup
    {
        public void ConfigureServices(WebHostBuilderContext context, IServiceCollection services)
        {
            services.AddTransient<IRandomNumberGenerator, ActualRandomNumberGenerator>();
            services.AddControllers();
        }

        public void Configure(WebHostBuilderContext context, IApplicationBuilder app)
        {
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
