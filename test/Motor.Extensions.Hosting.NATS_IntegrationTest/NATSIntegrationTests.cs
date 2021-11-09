using System;
using System.Text;
using System.Threading.Tasks;
using CloudNative.CloudEvents.SystemTextJson;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MSOptions = Microsoft.Extensions.Options.Options;
using Moq;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using Motor.Extensions.Hosting.NATS;
using Motor.Extensions.Hosting.NATS.Options;
using Motor.Extensions.TestUtilities;
using NATS.Client;
using RandomDataGenerator.FieldOptions;
using RandomDataGenerator.Randomizers;
using Xunit;

namespace Motor.Extensions.Hosting.NATS_IntegrationTest;

[Collection("NATSMessage")]
public class NATSIntegrationTests : IClassFixture<NATSFixture>
{
    private readonly IRandomizerString _randomizerString;
    private readonly string _natsUrl;

    public NATSIntegrationTests(NATSFixture fixture)
    {
        _randomizerString = RandomizerFactory.GetRandomizer(new FieldOptionsTextRegex { Pattern = @"^[A-Z]{10}" });
        _natsUrl = $"{fixture.Hostname}:{fixture.Port}";
    }

    [Fact(Timeout = 50000, Skip = "does not run on ci")]
    public async void PublishMessageWithoutException()
    {
        const string expectedMessage = "testMessage";
        var topicName = _randomizerString.Generate();
        var queueName = _randomizerString.Generate();

        var publisherOptions = GetNATSBaseOptions(topicName);
        var publisher = GetPublisher(MSOptions.Create(publisherOptions));

        var consumerOptions = GetNATSConsumerOptions(topicName, queueName);

        var consumer = GetConsumer<string>(MSOptions.Create(consumerOptions));
        var rawConsumedNatsMessage =
            await RawConsumedNatsMessageWithNatsPublisherPublishedMessage(consumer, publisher, expectedMessage);

        Assert.NotNull(rawConsumedNatsMessage);
        Assert.Equal(expectedMessage, Encoding.UTF8.GetString(rawConsumedNatsMessage));
    }

    [Fact(Timeout = 50000)]
    public async void PublishMessageAsJsonFormat()
    {
        const string expectedMessage = "testMessage";
        var topicName = _randomizerString.Generate();
        var queueName = _randomizerString.Generate();

        var publisherOptions = GetNATSBaseOptions(topicName);
        var publisher = GetPublisher(MSOptions.Create(publisherOptions), CloudEventFormat.Json);

        var consumerOptions = GetNATSConsumerOptions(topicName, queueName);

        var consumer = GetConsumer<string>(MSOptions.Create(consumerOptions));
        var rawConsumedNatsMessage =
            await RawConsumedNatsMessageWithNatsPublisherPublishedMessage(consumer, publisher, expectedMessage);

        Assert.NotNull(rawConsumedNatsMessage);
        var jsonEventFormatter = new JsonEventFormatter();
        var cloudEvent = jsonEventFormatter.DecodeStructuredModeMessage(rawConsumedNatsMessage, null, null);
        Assert.Equal(expectedMessage, Encoding.UTF8.GetString(cloudEvent.Data as byte[] ?? Array.Empty<byte>()));

    }

    [Fact(Timeout = 50000, Skip = "does not run on ci")]
    public async void Consume_RawPublishIntoNATSAndConsumeCreateCloudEvent_ConsumedEqualsPublished()
    {
        const string expectedMessage = "testMessage";
        var topicName = _randomizerString.Generate();
        var queueName = _randomizerString.Generate();
        var clientOptions = GetNATSConsumerOptions(topicName, queueName);

        var nats = new NATSClientFactory().From(clientOptions);

        var consumer = GetConsumer<string>(MSOptions.Create(clientOptions));
        var rawConsumedNatsMessage = await RawConsumedNatsMessage(consumer, nats, topicName, expectedMessage);
        Assert.NotNull(rawConsumedNatsMessage);
        Assert.Equal(expectedMessage, Encoding.UTF8.GetString(rawConsumedNatsMessage));
    }

    private static async Task<byte[]> RawConsumedNatsMessageWithNatsPublisherPublishedMessage(
        NATSMessageConsumer<string> messageConsumer, NATSMessagePublisher<string> messagePublisher, string expectedMessage)
    {
        var rawConsumedNatsMessage = (byte[])null;
        var taskCompletionSource = new TaskCompletionSource();
        messageConsumer.ConsumeCallbackAsync = async (dataEvent, _) =>
        {
            rawConsumedNatsMessage = dataEvent.TypedData;
            taskCompletionSource.TrySetResult();
            return await Task.FromResult(ProcessedMessageStatus.Success);
        };

        await messageConsumer.StartAsync();
        var consumerStartTask = messageConsumer.ExecuteAsync();

        await Task.Delay(TimeSpan.FromSeconds(5));
        await messagePublisher.PublishMessageAsync(
            MotorCloudEvent.CreateTestCloudEvent(Encoding.UTF8.GetBytes(expectedMessage)));

        await Task.WhenAny(consumerStartTask, taskCompletionSource.Task, Task.Delay(TimeSpan.FromSeconds(30)));
        return rawConsumedNatsMessage;
    }

    private static async Task<byte[]> RawConsumedNatsMessage(NATSMessageConsumer<string> messageConsumer, IConnection nats,
        string topicName, string expectedMessage)
    {
        var rawConsumedNatsMessage = (byte[])null;
        var taskCompletionSource = new TaskCompletionSource();
        messageConsumer.ConsumeCallbackAsync = async (dataEvent, _) =>
        {
            rawConsumedNatsMessage = dataEvent.TypedData;
            taskCompletionSource.TrySetResult();
            return await Task.FromResult(ProcessedMessageStatus.Success);
        };

        await messageConsumer.StartAsync();
        var consumerStartTask = messageConsumer.ExecuteAsync();

        await Task.Delay(TimeSpan.FromSeconds(5));
        PublishMessage(nats, topicName, expectedMessage);

        await Task.WhenAny(consumerStartTask, taskCompletionSource.Task, Task.Delay(TimeSpan.FromSeconds(30)));
        return rawConsumedNatsMessage;
    }

    private NATSBaseOptions GetNATSBaseOptions(string topicName)
    {
        var clientOptions = new NATSBaseOptions()
        {
            Url = _natsUrl,
            Topic = topicName,
        };
        return clientOptions;
    }

    private NATSConsumerOptions GetNATSConsumerOptions(string topicName, string queueName)
    {
        var clientOptions = new NATSConsumerOptions
        {
            Url = _natsUrl,
            Topic = topicName,
            Queue = queueName
        };
        return clientOptions;
    }

    private static void PublishMessage(IConnection natsClient, string topic, string message)
    {
        natsClient.Publish(topic, Encoding.UTF8.GetBytes(message));
    }

    private NATSMessagePublisher<string> GetPublisher(IOptions<NATSBaseOptions> clientOptions, CloudEventFormat cloudEventFormat = CloudEventFormat.Protocol)
    {
        return new NATSMessagePublisher<string>(clientOptions, new NATSClientFactory(), new JsonEventFormatter(),
            MSOptions.Create(new PublisherOptions { CloudEventFormat = cloudEventFormat }));
    }

    private NATSMessageConsumer<T> GetConsumer<T>(IOptions<NATSConsumerOptions> clientOptions)
    {
        var fakeLoggerMock = Mock.Of<ILogger<NATSMessageConsumer<T>>>();
        return new NATSMessageConsumer<T>(clientOptions, fakeLoggerMock, GetApplicationNameService(),
            new NATSClientFactory());
    }

    private IApplicationNameService GetApplicationNameService(string source = "test://non")
    {
        var mock = new Mock<IApplicationNameService>();
        mock.Setup(t => t.GetSource()).Returns(new Uri(source));
        return mock.Object;
    }
}
