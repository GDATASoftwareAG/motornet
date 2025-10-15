using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Motor.Extensions.Hosting.BackgroundService_IntegrationTest.TestExample;
using Motor.Extensions.Utilities;

await MotorHost
    .CreateDefaultBuilder()
    .ConfigureServices((_, services) =>
    {
        services.AddSingleton<ISharedService, SharedService>();
    })
    .RunConsoleAsync();

public class ExampleProgram
{
    protected ExampleProgram()
    {
    }
}
