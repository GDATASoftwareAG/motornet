using System.Text;
using Motor.Extensions.Conversion.Abstractions;
using Motor.Extensions.Hosting.RabbitMQ_IntegrationTest;
using RabbitMQ.Client;

namespace Motor.Extensions.Utilities_IntegrationTest;

public abstract class GenericHostingTestBase
{
    private static readonly Random _random = new();

    protected RabbitMQFixture Fixture { get; }

    protected GenericHostingTestBase(RabbitMQFixture fixture)
    {
        Fixture = fixture;
    }

    protected ushort RandomHttpPort { get; } = (ushort)_random.Next(ushort.MaxValue);

    protected static async Task CreateQueueForServicePublisherWithPublisherBindingFromConfigAsync(IChannel channel)
    {
        var destinationQueueName = Environment.GetEnvironmentVariable("DestinationQueueName") ?? "DefaultQueueName";
        const string destinationExchange = "amq.topic";
        var destinationRoutingKey =
            Environment.GetEnvironmentVariable("RabbitMQPublisher__PublishingTarget__RoutingKey")
            ?? "DefaultRoutingKey";
        var emptyArguments = new Dictionary<string, object?>();
        await channel.QueueDeclareAsync(destinationQueueName, true, false, false, emptyArguments);
        await channel.QueueBindAsync(destinationQueueName, destinationExchange, destinationRoutingKey, emptyArguments);
        await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
    }

    protected static async Task PublishMessageIntoQueueOfServiceAsync(
        IChannel channel,
        string messageToPublish,
        IDictionary<string, object?>? rabbitMqHeaders = null
    )
    {
        var basicProperties = new BasicProperties();

        if (rabbitMqHeaders is not null)
        {
            basicProperties.Headers = rabbitMqHeaders;
        }

        await channel.BasicPublishAsync(
            "amq.topic",
            Environment.GetEnvironmentVariable("RabbitMQPublisher__PublishingTarget__RoutingKey") ?? "serviceQueue",
            true,
            basicProperties,
            Encoding.UTF8.GetBytes(messageToPublish)
        );
    }
}

internal class StringSerializer : IMessageSerializer<string>
{
    public byte[] Serialize(string message)
    {
        return Encoding.UTF8.GetBytes(message);
    }
}

internal class StringDeserializer : IMessageDeserializer<string>
{
    public string Deserialize(byte[] message)
    {
        return Encoding.UTF8.GetString(message);
    }
}
