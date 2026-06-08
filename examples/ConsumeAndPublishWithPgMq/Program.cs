using ConsumeAndPublishWithPgMq;
using ConsumeAndPublishWithPgMq.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.Consumer;
using Motor.Extensions.Hosting.PgMq;
using Motor.Extensions.Hosting.Publisher;
using Motor.Extensions.Utilities;

var useMotorHost = bool.Parse(Environment.GetEnvironmentVariable("USE_MOTOR_HOST") ?? "false");

if (useMotorHost)
{
    await MotorHost
        .CreateDefaultBuilder()
        .ConfigureSingleOutputService<InputMessage, OutputMessage>()
        .ConfigureServices(
            (_, services) =>
            {
                services.AddTransient<ISingleOutputService<InputMessage, OutputMessage>, SingleOutputService>();
            }
        )
        .ConfigureConsumer<InputMessage>(
            (_, builder) =>
            {
                builder.AddPgMq();
            }
        )
        .ConfigurePublisher<OutputMessage>(
            (_, builder) =>
            {
                builder.AddPgMq();
            }
        )
        .RunConsoleAsync();
}
else
{
    var builder = Host.CreateApplicationBuilder(args);

    // Configure opinionated defaults for Motor applications, such as logging.
    builder.AddMotorDefaults();

    builder.ConfigureSingleOutputService<InputMessage, OutputMessage>();
    builder.Services.AddTransient<ISingleOutputService<InputMessage, OutputMessage>, SingleOutputService>();

    builder.AddConsumer<InputMessage>().AddPgMq();

    builder.AddPublisher<OutputMessage>().AddPgMq();

    await builder.Build().RunAsync();
}
