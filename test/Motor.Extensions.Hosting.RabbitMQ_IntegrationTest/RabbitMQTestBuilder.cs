using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CloudNative.CloudEvents.SystemTextJson;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using Motor.Extensions.Hosting.RabbitMQ;
using Motor.Extensions.Hosting.RabbitMQ.Options;
using Motor.Extensions.TestUtilities;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RandomDataGenerator.FieldOptions;
using RandomDataGenerator.Randomizers;
using Xunit;
using MSOptions = Microsoft.Extensions.Options.Options;

namespace Motor.Extensions.Hosting.RabbitMQ_IntegrationTest;

public struct Message
{
    public Message(byte[] body, byte priority)
    {
        Body = body;
        Priority = priority;
    }

    public byte[] Body { get; }
    public byte Priority { get; }
}

public class RabbitMQTestBuilder
{
    public const ushort PrefetchCount = 100;

    private static readonly Random _random = new();
    private Func<MotorCloudEvent<byte[]>, CancellationToken, Task<ProcessedMessageStatus>> Callback;
    private bool createQueue;
    private RabbitMQFixture Fixture;
    private bool isBuilt;
    private readonly IList<Message> messages = new List<Message>();
    internal string QueueName { get; init; }
    internal string RoutingKey { get; init; }

    public static RabbitMQTestBuilder CreateWithoutQueueDeclare(RabbitMQFixture fixture)
    {
        var randomizerString = RandomizerFactory.GetRandomizer(new FieldOptionsTextRegex { Pattern = @"^[A-Z]{10}" });

        return new RabbitMQTestBuilder
        {
            QueueName = randomizerString.Generate(),
            RoutingKey = randomizerString.Generate(),
            Fixture = fixture
        };
    }

    public static RabbitMQTestBuilder CreateWithQueueDeclare(RabbitMQFixture fixture)
    {
        var q = CreateWithoutQueueDeclare(fixture);
        q.createQueue = true;
        return q;
    }

    public RabbitMQTestBuilder WithConsumerCallback(Func<MotorCloudEvent<byte[]>, CancellationToken, Task<ProcessedMessageStatus>> callback)
    {
        Callback = callback;
        createQueue = true;
        return this;
    }

    public RabbitMQTestBuilder WithSinglePublishedMessage(byte priority, byte[] message)
    {
        messages.Add(new Message(message, priority));
        return this;
    }

    public RabbitMQTestBuilder WithSingleRandomPublishedMessage()
    {
        var bytes = new byte[10];
        _random.NextBytes(bytes);
        return WithSinglePublishedMessage(100, bytes);
    }

    public RabbitMQTestBuilder WithMultipleRandomPublishedMessage(ushort number = PrefetchCount)
    {
        for (var i = 0; i < number; i++) WithSingleRandomPublishedMessage();

        return this;
    }

    public RabbitMQTestBuilder Build(int retries = 3)
    {
        if (createQueue)
        {
            var config = GetConsumerConfig<string>();
            using (var channel = Fixture.Connection.CreateModel())
            {
                DeclareQueue(config, channel);
                foreach (var message in messages) PublishSingleMessage(channel, message, config);
            }

            Policy
                .Handle<Exception>()
                .WaitAndRetry(retries, retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                )
                .Execute(() =>
                {
                    var messageInConsumerQueue = MessageInConsumerQueue();
                    Assert.Equal((uint)messages.Count, messageInConsumerQueue);
                });
        }

        isBuilt = true;
        return this;
    }

    private void PublishSingleMessage<T>(IModel channel, Message message,
        RabbitMQConsumerOptions<T> options)
    {
        var properties = channel.CreateBasicProperties();
        properties.DeliveryMode = 2;
        properties.Priority = message.Priority;
        properties.Headers = new Dictionary<string, object>();

        var bindings = options.Queue.Bindings[0];
        channel.BasicPublish(bindings.Exchange, bindings.RoutingKey, true, properties, message.Body);
    }

    private void DeclareQueue<T>(RabbitMQConsumerOptions<T> options, IModel channel)
    {
        var arguments = options.Queue.Arguments.ToDictionary(t => t.Key, t => t.Value);
        arguments.Add("x-max-priority", options.Queue.MaxPriority);
        arguments.Add("x-max-length", options.Queue.MaxLength);
        arguments.Add("x-max-length-bytes", options.Queue.MaxLengthBytes);
        arguments.Add("x-message-ttl", options.Queue.MessageTtl);
        channel.QueueDeclare(
            options.Queue.Name,
            options.Queue.Durable,
            false,
            options.Queue.AutoDelete,
            arguments
        );
        foreach (var routingKeyConfig in options.Queue.Bindings)
            channel.QueueBind(
                options.Queue.Name,
                routingKeyConfig.Exchange,
                routingKeyConfig.RoutingKey,
                routingKeyConfig.Arguments);
    }

    private RabbitMQConsumerOptions<T> GetConsumerConfig<T>()
    {
        return new()
        {
            Host = "host",
            User = "guest",
            Password = "guest",
            VirtualHost = "/",
            Queue = new RabbitMQQueueOptions
            {
                Name = QueueName,
                Bindings = new[]
                {
                        new RabbitMQBindingOptions
                        {
                            Exchange = "amq.topic",
                            RoutingKey = RoutingKey
                        }
                    }
            },
            PrefetchCount = PrefetchCount
        };
    }

    public IMessageConsumer<T> GetConsumer<T>(IHostApplicationLifetime applicationLifetime = null)
    {
        if (!isBuilt)
            throw new InvalidOperationException();

        var rabbitConnectionFactoryMock = new Mock<IRabbitMQConnectionFactory<T>>();

        var channel = Fixture.Connection.CreateModel();

        applicationLifetime ??= new Mock<IHostApplicationLifetime>().Object;

        rabbitConnectionFactoryMock
            .Setup(f => f.CurrentConnection)
            .Returns(Fixture.Connection);

        rabbitConnectionFactoryMock
            .Setup(f => f.CurrentChannel)
            .Returns(channel);

        rabbitConnectionFactoryMock
            .Setup(f => f.Dispose())
            .Callback(() =>
            {
                channel.Dispose();
            });

        var options = MSOptions.Create(GetConsumerConfig<T>());

        var consumer = new RabbitMQMessageConsumer<T>(
            Mock.Of<ILogger<RabbitMQMessageConsumer<T>>>(),
            rabbitConnectionFactoryMock.Object,
            options,
            applicationLifetime,
            null
        )
        {
            ConsumeCallbackAsync = Callback
        };
        return consumer;
    }

    public bool IsConsumerQueueDeclared()
    {
        if (!isBuilt)
            throw new InvalidOperationException();
        using var channel = Fixture.Connection.CreateModel();
        channel.QueueDeclarePassive(QueueName);
        return true;
    }

    public uint MessageInConsumerQueue()
    {
        using var channel = Fixture.Connection.CreateModel();
        var queueDeclarePassive = channel.QueueDeclarePassive(QueueName);
        return queueDeclarePassive.MessageCount;
    }

    public IRawMessagePublisher<T> GetPublisher<T>()
    {
        if (!isBuilt)
            throw new InvalidOperationException();
        var rabbitConnectionFactoryMock = new Mock<IRabbitMQConnectionFactory<T>>();

        var channel = Fixture.Connection.CreateModel();

        rabbitConnectionFactoryMock
            .Setup(f => f.CurrentConnection)
            .Returns(Fixture.Connection);

        rabbitConnectionFactoryMock
            .Setup(f => f.CurrentChannel)
            .Returns(channel);

        var options = MSOptions.Create(GetPublisherConfig<T>());
        var publisherOptions = MSOptions.Create(new PublisherOptions());
        return new RabbitMQMessagePublisher<T>(rabbitConnectionFactoryMock.Object, options, publisherOptions,
            new JsonEventFormatter());
    }

    private RabbitMQPublisherOptions<T> GetPublisherConfig<T>()
    {
        return new()
        {
            Host = "host",
            User = "guest",
            Password = "guest",
            VirtualHost = "/",
            PublishingTarget = new RabbitMQBindingOptions
            {
                Exchange = "amq.topic",
                RoutingKey = RoutingKey
            }
        };
    }

    public async Task<MotorCloudEvent<byte[]>> GetMessageFromQueue()
    {
        var message = (byte[])null;
        var priority = (byte)0;
        using (var channel = Fixture.Connection.CreateModel())
        {
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (_, args) =>
            {
                priority = args.BasicProperties.Priority;
                message = args.Body.ToArray();
            };

            channel.BasicConsume(QueueName, false, consumer);
            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        var cloudEvent = MotorCloudEvent.CreateTestCloudEvent(message);
        cloudEvent.SetRabbitMQPriority(priority);

        return cloudEvent;
    }
}
