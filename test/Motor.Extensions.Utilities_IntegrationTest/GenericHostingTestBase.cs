using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Motor.Extensions.Conversion.Abstractions;
using Motor.Extensions.Hosting.RabbitMQ_IntegrationTest;
using RabbitMQ.Client;
using RandomDataGenerator.FieldOptions;
using RandomDataGenerator.Randomizers;

namespace Motor.Extensions.Utilities_IntegrationTest;

public abstract class GenericHostingTestBase
{
    protected RabbitMQFixture Fixture { get; }

    protected GenericHostingTestBase(RabbitMQFixture fixture)
    {
        Fixture = fixture;
    }

    protected void PrepareQueues(int prefetchCount = 1)
    {
        var randomizerString = RandomizerFactory.GetRandomizer(new FieldOptionsTextRegex { Pattern = @"^[A-Z]{10}" });
        Environment.SetEnvironmentVariable("RabbitMQConsumer__Port", Fixture.Port.ToString());
        Environment.SetEnvironmentVariable("RabbitMQConsumer__Host", Fixture.Hostname);
        Environment.SetEnvironmentVariable("RabbitMQConsumer__Queue__Name", randomizerString.Generate());
        Environment.SetEnvironmentVariable("RabbitMQConsumer__PrefetchCount", prefetchCount.ToString());
        Environment.SetEnvironmentVariable(
            "RabbitMQPublisher__PublishingTarget__RoutingKey",
            randomizerString.Generate()
        );
        Environment.SetEnvironmentVariable("RabbitMQPublisher__Port", Fixture.Port.ToString());
        Environment.SetEnvironmentVariable("RabbitMQPublisher__Host", Fixture.Hostname);
        Environment.SetEnvironmentVariable("DestinationQueueName", randomizerString.Generate());
    }

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
