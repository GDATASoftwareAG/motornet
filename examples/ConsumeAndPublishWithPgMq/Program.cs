using ConsumeAndPublishWithPgMq;
using ConsumeAndPublishWithPgMq.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.Consumer;
using Motor.Extensions.Hosting.PgMq;
using Motor.Extensions.Hosting.Publisher;
using Motor.Extensions.Utilities;

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
