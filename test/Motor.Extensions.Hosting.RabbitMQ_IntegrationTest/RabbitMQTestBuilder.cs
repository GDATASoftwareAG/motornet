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
    private bool withDeadLetterExchange;
    private bool withRepublishToDeadLetterExchangeOnInvalidInput;
    private RabbitMQFixture Fixture;
    private bool isBuilt;
    private readonly IList<Message> messages = new List<Message>();
    internal string QueueName { get; init; }
    internal string DlxQueueName { get; init; }
    internal string RoutingKey { get; init; }

    public static RabbitMQTestBuilder CreateWithoutQueueDeclare(RabbitMQFixture fixture)
    {
        var randomizerString = RandomizerFactory.GetRandomizer(new FieldOptionsTextRegex { Pattern = @"^[A-Z]{10}" });
        var queueName = randomizerString.Generate();
        return new RabbitMQTestBuilder
        {
            QueueName = queueName,
            DlxQueueName = $"{queueName}Dlx",
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

    public RabbitMQTestBuilder WithDeadLetterExchange()
    {
        withDeadLetterExchange = true;
        return this;
    }

    public RabbitMQTestBuilder WithRepublishToDeadLetterExchangeOnInvalidInput(bool republishToDeadLetterExchange)
    {
        withRepublishToDeadLetterExchangeOnInvalidInput = republishToDeadLetterExchange;
        return this;
    }

    public RabbitMQTestBuilder WithConsumerCallback(Func<MotorCloudEvent<byte[]>, CancellationToken, Task<ProcessedMessageStatus>> callback, bool create = true)
    {
        Callback = callback;
        createQueue = create;
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
        for (var i = 0; i < number; i++)
        {
            WithSingleRandomPublishedMessage();
        }

        return this;
    }

    public async Task<RabbitMQTestBuilder> BuildAsync(int retries = 3)
    {
        if (createQueue)
        {
            var config = GetConsumerConfig<string>();
            using (var channel = await (await Fixture.ConnectionAsync()).CreateChannelAsync())
            {
                await DeclareQueueAsync(config, channel);
                foreach (var message in messages)
                {
                    await PublishSingleMessageAsync(channel, message, config);
                }
            }

            await Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(retries, retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                )
                .ExecuteAsync(async () =>
                {
                    var messageInConsumerQueue = await MessagesInQueueAsync(QueueName);
                    Assert.Equal((uint)messages.Count, messageInConsumerQueue);
                });
        }

        isBuilt = true;
        return this;
    }

    private async Task PublishSingleMessageAsync<T>(IChannel channel, Message message,
        RabbitMQConsumerOptions<T> options)
    {
        var properties = new BasicProperties();
        properties.DeliveryMode = DeliveryModes.Persistent;
        properties.Priority = message.Priority;
        properties.Headers = new Dictionary<string, object>();

        var bindings = options.Queue.Bindings[0];
        await channel.BasicPublishAsync(bindings.Exchange, bindings.RoutingKey, true, properties, message.Body);
    }

    private async Task DeclareQueueAsync<T>(RabbitMQConsumerOptions<T> options, IChannel channel)
    {
        var arguments = options.Queue.Arguments.ToDictionary(t => t.Key, t => t.Value);
        arguments.Add("x-max-priority", options.Queue.MaxPriority);
        arguments.Add("x-max-length", options.Queue.MaxLength);
        arguments.Add("x-max-length-bytes", options.Queue.MaxLengthBytes);
        arguments.Add("x-message-ttl", options.Queue.MessageTtl);
        if (options.Queue.DeadLetterExchange is not null)
        {
            arguments.Add("x-dead-letter-exchange", options.Queue.DeadLetterExchange.Binding.Exchange);
            arguments.Add("x-dead-letter-routing-key", options.Queue.DeadLetterExchange.Binding.RoutingKey);
        }
        await channel.QueueDeclareAsync(
            options.Queue.Name,
            options.Queue.Durable,
            false,
            options.Queue.AutoDelete,
            arguments
        );
        foreach (var routingKeyConfig in options.Queue.Bindings)
        {
            await channel.QueueBindAsync(
                options.Queue.Name,
                routingKeyConfig.Exchange,
                routingKeyConfig.RoutingKey,
                routingKeyConfig.Arguments);
        }
    }

    private RabbitMQConsumerOptions<T> GetConsumerConfig<T>()
    {
        var deadLetterExchange = withDeadLetterExchange
            ? new RabbitMQDeadLetterExchangeOptions
            {
                RepublishInvalidInputToDeadLetterExchange = withRepublishToDeadLetterExchangeOnInvalidInput,
                Binding = new RabbitMQBindingOptions
                {
                    Exchange = "amq.topic",
                    RoutingKey = $"dlx.{RoutingKey}"
                }
            }
            : null;
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
                },
                DeadLetterExchange = deadLetterExchange
            },
            PrefetchCount = PrefetchCount
        };
    }

    public async Task<IMessageConsumer<T>> GetConsumerAsync<T>(IHostApplicationLifetime applicationLifetime = null)
    {
        if (!isBuilt)
        {
            throw new InvalidOperationException();
        }

        var rabbitConnectionFactoryMock = new Mock<IRabbitMQConnectionFactory<T>>();

        var channel = await (await Fixture.ConnectionAsync()).CreateChannelAsync();

        applicationLifetime ??= new Mock<IHostApplicationLifetime>().Object;

        rabbitConnectionFactoryMock
            .Setup(f => f.CurrentConnectionAsync())
            .ReturnsAsync(await Fixture.ConnectionAsync());

        rabbitConnectionFactoryMock
            .Setup(f => f.CurrentChannelAsync())
            .ReturnsAsync(channel);

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

    public async Task<bool> IsConsumerQueueDeclaredAsync()
    {
        if (!isBuilt)
        {
            throw new InvalidOperationException();
        }

        await using var channel = await (await Fixture.ConnectionAsync()).CreateChannelAsync();
        await channel.QueueDeclarePassiveAsync(QueueName);

        if (withDeadLetterExchange)
        {
            await channel.QueueDeclarePassiveAsync(DlxQueueName);
        }

        return true;
    }

    public async Task<uint> MessagesInQueueAsync(string queueName)
    {
        await using var channel = await (await Fixture.ConnectionAsync()).CreateChannelAsync();
        var queueDeclarePassive = await channel.QueueDeclarePassiveAsync(queueName);
        return queueDeclarePassive.MessageCount;
    }

    public async Task<IRawMessagePublisher<T>> GetPublisherAsync<T>()
    {
        if (!isBuilt)
        {
            throw new InvalidOperationException();
        }

        var rabbitConnectionFactoryMock = new Mock<IRabbitMQConnectionFactory<T>>();

        var channel = await (await Fixture.ConnectionAsync()).CreateChannelAsync();

        rabbitConnectionFactoryMock
            .Setup(f => f.CurrentConnectionAsync())
            .ReturnsAsync(await Fixture.ConnectionAsync());

        rabbitConnectionFactoryMock
            .Setup(f => f.CurrentChannelAsync())
            .ReturnsAsync(channel);

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

    public async Task<MotorCloudEvent<byte[]>> GetMessageFromQueueAsync(string queueName)
    {
        var message = (byte[])null;
        var priority = (byte)0;
        var taskCompletionSource = new TaskCompletionSource();

        await using (var channel = await (await Fixture.ConnectionAsync()).CreateChannelAsync())
        {
            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += (_, args) =>
            {
                priority = args.BasicProperties.Priority;
                message = args.Body.ToArray();
                taskCompletionSource.TrySetResult();
                return Task.CompletedTask;
            };

            await channel.BasicConsumeAsync(queueName, false, consumer);
            await Task.WhenAny(taskCompletionSource.Task, Task.Delay(TimeSpan.FromSeconds(10)));
        }

        var cloudEvent = MotorCloudEvent.CreateTestCloudEvent(message);
        cloudEvent.SetRabbitMQPriority(priority);

        return cloudEvent;
    }
}
