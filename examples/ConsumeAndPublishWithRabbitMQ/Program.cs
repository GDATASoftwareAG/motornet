using System.Threading.Tasks;
using ConsumeAndPublishWithRabbitMQ.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Motor.Extensions.Conversion.SystemJson;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.Consumer;
using Motor.Extensions.Hosting.Publisher;
using Motor.Extensions.Hosting.RabbitMQ;
using Motor.Extensions.Utilities;

namespace ConsumeAndPublishWithRabbitMQ
{
    public static class Program
    {
        public static Task Main() =>
            MotorHost.CreateDefaultBuilder()
                // Configure the types of the input and output messages
                .ConfigureSingleOutputService<InputMessage, OutputMessage>()
                .ConfigureServices((hostContext, services) =>
                {
                    // Add a handler for the input message which returns an output message
                    // This handler is called for every new incoming message
                    services.AddTransient<ISingleOutputService<InputMessage, OutputMessage>, SingleOutputService>();
                })
                // Add the incomming communication module. 
                .ConfigureConsumer<InputMessage>((context, builder) =>
                {
                    // In this case the messages are received from RabbitMQ
                    builder.AddRabbitMQ();
                    // The encoding of the incoming message, such that the handler is able to deserialize the message
                    builder.AddSystemJson();
                })
                // Add the outgoing communication module.
                .ConfigurePublisher<OutputMessage>((context, builder) =>
                {
                    // In this case the messages are send to RabbitMQ
                    builder.AddRabbitMQ();
                    // The encoding of the outgoing message, such that the handler is able to serialize the message
                    builder.AddSystemJson();
                })
                .RunConsoleAsync();
    }
}
