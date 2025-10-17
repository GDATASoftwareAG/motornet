using Motor.Extensions.Utilities;
using Microsoft.Extensions.Hosting;
using AspNetExample;

await MotorHost.CreateDefaultBuilder()
    .UseStartup<Startup>()
    .RunConsoleAsync();
