using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

namespace Motor.Extensions.Hosting.NATS_IntegrationTest
{
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

        [Fact(Timeout = 50000)]
        public async void PublishMessageWithoutException()
        {
            const string expectedMessage = "testMessage";
            var topicName = _randomizerString.Generate();
            var queueName = _randomizerString.Generate();

            var publisherOptions = GetNATSBaseOptions(topicName);
            var publisher = GetPublisher(new OptionsWrapper<NATSBaseOptions>(publisherOptions));

            var consumerOptions = GetNATSConsumerOptions(topicName, queueName);

            var consumer = GetConsumer<string>(new OptionsWrapper<NATSConsumerOptions>(consumerOptions), queueName);
            var rawConsumedNatsMessage =
                await RawConsumedNatsMessageWithNatsPublisherPublishedMessage(consumer, publisher, expectedMessage);

            Assert.NotNull(rawConsumedNatsMessage);
            Assert.Equal(expectedMessage, Encoding.UTF8.GetString(rawConsumedNatsMessage));
        }

        private static async Task<byte[]> RawConsumedNatsMessageWithNatsPublisherPublishedMessage(
            NATSConsumer<string> consumer, NATSPublisher publisher, string expectedMessage)
        {
            var rawConsumedNatsMessage = (byte[])null;
            var taskCompletionSource = new TaskCompletionSource();
            consumer.ConsumeCallbackAsync = async (dataEvent, _) =>
            {
                rawConsumedNatsMessage = dataEvent.TypedData;
                taskCompletionSource.TrySetResult();
                return await Task.FromResult(ProcessedMessageStatus.Success);
            };

            await consumer.StartAsync();
            var consumerStartTask = consumer.ExecuteAsync();

            await Task.Delay(TimeSpan.FromSeconds(5));

            await publisher.PublishMessageAsync(
                MotorCloudEvent.CreateTestCloudEvent(Encoding.UTF8.GetBytes(expectedMessage)));

            await Task.WhenAny(consumerStartTask, taskCompletionSource.Task, Task.Delay(TimeSpan.FromSeconds(30)));
            return rawConsumedNatsMessage;
        }

        [Fact(Timeout = 50000)]
        public async void Consume_RawPublishIntoNATSAndConsumeCreateCloudEvent_ConsumedEqualsPublished()
        {
            const string expectedMessage = "testMessage";
            var topicName = _randomizerString.Generate();
            var queueName = _randomizerString.Generate();
            var clientOptions = GetNATSConsumerOptions(topicName, queueName);

            var nats = new NATSClientFactory().From(clientOptions);

            var consumer = GetConsumer<string>(new OptionsWrapper<NATSConsumerOptions>(clientOptions), queueName);
            var rawConsumedNatsMessage = await RawConsumedNatsMessage(consumer, nats, topicName, expectedMessage);
            Assert.NotNull(rawConsumedNatsMessage);
            Assert.Equal(expectedMessage, Encoding.UTF8.GetString(rawConsumedNatsMessage));
        }

        private static async Task<byte[]> RawConsumedNatsMessage(NATSConsumer<string> consumer, IConnection nats,
            string topicName, string expectedMessage)
        {
            var rawConsumedNatsMessage = (byte[])null;
            var taskCompletionSource = new TaskCompletionSource();
            consumer.ConsumeCallbackAsync = async (dataEvent, _) =>
            {
                rawConsumedNatsMessage = dataEvent.TypedData;
                taskCompletionSource.TrySetResult();
                return await Task.FromResult(ProcessedMessageStatus.Success);
            };

            await consumer.StartAsync();
            var consumerStartTask = consumer.ExecuteAsync();

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

        private NATSPublisher GetPublisher(IOptions<NATSBaseOptions> clientOptions)
        {
            return new NATSPublisher(clientOptions, new NATSClientFactory());
        }

        private NATSConsumer<T> GetConsumer<T>(IOptions<NATSConsumerOptions> clientOptions, string queueName)
        {
            var fakeLoggerMock = Mock.Of<ILogger<NATSConsumer<T>>>();
            return new NATSConsumer<T>(clientOptions, fakeLoggerMock, GetApplicationNameService(),
                new NATSClientFactory());
        }

        private IApplicationNameService GetApplicationNameService(string source = "test://non")
        {
            var mock = new Mock<IApplicationNameService>();
            mock.Setup(t => t.GetSource()).Returns(new Uri(source));
            return mock.Object;
        }
    }
}
