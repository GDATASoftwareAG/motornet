using System.Text;
using Motor.Extensions.Conversion.Abstractions;
using Motor.Extensions.Hosting.RabbitMQ_IntegrationTest;
using RabbitMQ.Client;
using RandomDataGenerator.FieldOptions;
using RandomDataGenerator.Randomizers;

namespace Motor.Extensions.Utilities_IntegrationTest;

public abstract class GenericHostingTestBase
{
    private static readonly Random Random = new();

    protected RabbitMQFixture Fixture { get; }
    protected string RoutingKey { get; }
    protected string ConsumerQueueName { get; }
    protected string DestinationQueueName { get; }

    protected GenericHostingTestBase(RabbitMQFixture fixture)
    {
        Fixture = fixture;

        var randomizerString = RandomizerFactory.GetRandomizer(new FieldOptionsTextRegex { Pattern = @"^[A-Z]{10}" });

        RoutingKey = randomizerString.Generate()!;
        ConsumerQueueName = randomizerString.Generate()!;
        DestinationQueueName = randomizerString.Generate()!;
    }

    protected ushort RandomHttpPort { get; } = (ushort)Random.Next(ushort.MaxValue);

    protected async Task CreateQueueForServicePublisherWithPublisherBindingFromConfigAsync(IChannel channel)
    {
        const string destinationExchange = "amq.topic";
        var emptyArguments = new Dictionary<string, object?>();
        await channel.QueueDeclareAsync(DestinationQueueName, true, false, false, emptyArguments);
        await channel.QueueBindAsync(DestinationQueueName, destinationExchange, RoutingKey, emptyArguments);
        await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
    }

    protected async Task PublishMessageIntoQueueOfServiceAsync(
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
            "serviceQueue",
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
