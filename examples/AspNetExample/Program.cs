using AspNetExample;
using Microsoft.Extensions.Hosting;
using Motor.Extensions.Utilities;

await MotorHost.CreateDefaultBuilder().UseStartup<Startup>().RunConsoleAsync();
