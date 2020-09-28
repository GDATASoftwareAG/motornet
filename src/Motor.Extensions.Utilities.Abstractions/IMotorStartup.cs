using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Motor.Extensions.Utilities.Abstractions
{
    public interface IMotorStartup
    {
        void ConfigureServices(WebHostBuilderContext context, IServiceCollection services);
        void Configure(WebHostBuilderContext context, IApplicationBuilder builder);
    }
}
