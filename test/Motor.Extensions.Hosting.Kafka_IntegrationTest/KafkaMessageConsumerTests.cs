using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CloudNative.CloudEvents;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.Kafka;
using Motor.Extensions.TestUtilities;
using RandomDataGenerator.FieldOptions;
using RandomDataGenerator.Randomizers;
using Xunit;

namespace Motor.Extensions.Hosting.Kafka_IntegrationTest
{
    [Collection("KafkaMessage")]
    public class KafkaMessageConsumerTests : IClassFixture<KafkaFixture>
    {
        private readonly KafkaFixture _fixture;
        private IRandomizerString randomizerString;

        public KafkaMessageConsumerTests(KafkaFixture fixture)
        {
            _fixture = fixture;
            randomizerString = RandomizerFactory.GetRandomizer(new FieldOptionsTextRegex {Pattern = @"^[A-Z]{10}"});
        }

        [Fact(Timeout = 50000)]
        public async Task Consume_PublishIntoKafkaAndConsumeCreateCloudEvent_ConsumedEqualsPublished()
        {
            var topic = randomizerString.Generate();
            const string message = "testMessage";
            await PublishMessage(topic, "someKey", message);
            var consumer = GetConsumer<string>(topic);
            var rawConsumedKafkaMessage = (byte[]) null;
            var taskCompletionSource = new TaskCompletionSource();
            consumer.ConsumeCallbackAsync = async (dataEvent, _) =>
            {
                rawConsumedKafkaMessage = dataEvent.TypedData;
                taskCompletionSource.TrySetResult();
                return await Task.FromResult(ProcessedMessageStatus.Success);
            };

            await consumer.StartAsync();
            var consumerStartTask = consumer.ExecuteAsync();

            await Task.WhenAny(consumerStartTask, taskCompletionSource.Task);
            Assert.Equal(message, Encoding.UTF8.GetString(rawConsumedKafkaMessage));
        }

        [Fact(Timeout = 50000)]
        public async Task Consume_PublishIntoKafkaAndConsumeWithCloudEvent_ConsumedEqualsPublished()
        {
            var topic = randomizerString.Generate();
            var message = "testMessage";
            var publisher = GetPublisher<byte[]>(topic);
            var motorCloudEvent =
                MotorCloudEvent.CreateTestCloudEvent(message).CreateNew(Encoding.UTF8.GetBytes(message));
            await publisher.PublishMessageAsync(motorCloudEvent, CancellationToken.None);
            var consumer = GetConsumer<byte[]>(topic);
            string id = null;
            var taskCompletionSource = new TaskCompletionSource();
            consumer.ConsumeCallbackAsync = async (dataEvent, _) =>
            {
                id = dataEvent.Id;
                taskCompletionSource.TrySetResult();
                return await Task.FromResult(ProcessedMessageStatus.Success);
            };

            await consumer.StartAsync();
            var consumerStartTask = consumer.ExecuteAsync();

            await Task.WhenAny(consumerStartTask, taskCompletionSource.Task);
            Assert.Equal(motorCloudEvent.Id, id);
        }

        [Fact(Timeout = 50000)]
        public async Task Consume_PublishIntoExtensionDefinedTopic_ConsumedEqualsPublished()
        {
            var topic = randomizerString.Generate();
            var message = "testMessage";
            var publisher = GetPublisher<byte[]>("wrong_topic");
            var motorCloudEvent =
                MotorCloudEvent.CreateTestCloudEvent(message).CreateNew(Encoding.UTF8.GetBytes(message));
            motorCloudEvent.GetExtensionOrCreate(() => new KafkaTopicExtension(topic));
            await publisher.PublishMessageAsync(motorCloudEvent, CancellationToken.None);
            var consumer = GetConsumer<byte[]>(topic);
            string id = null;
            var taskCompletionSource = new TaskCompletionSource();
            consumer.ConsumeCallbackAsync = async (dataEvent, _) =>
            {
                id = dataEvent.Id;
                taskCompletionSource.TrySetResult();
                return await Task.FromResult(ProcessedMessageStatus.Success);
            };

            await consumer.StartAsync();
            var consumerStartTask = consumer.ExecuteAsync();

            await Task.WhenAny(consumerStartTask, taskCompletionSource.Task);
            Assert.Equal(motorCloudEvent.Id, id);
        }

        private async Task PublishMessage(string topic, string key, string value)
        {
            using var producer = new ProducerBuilder<string, byte[]>(GetPublisherConfig<string>(topic)).Build();
            await producer.ProduceAsync(topic,
                new Message<string, byte[]> {Key = key, Value = Encoding.UTF8.GetBytes(value)});
            producer.Flush();
        }

        private KafkaMessageConsumer<T> GetConsumer<T>(string topic)
        {
            var options = new OptionsWrapper<KafkaConsumerConfig<T>>(GetConsumerConfig<T>(topic));
            var fakeLoggerMock = Mock.Of<ILogger<KafkaMessageConsumer<T>>>();
            return new KafkaMessageConsumer<T>(fakeLoggerMock, options, null, GetApplicationNameService(),
                new JsonEventFormatter());
        }

        private KafkaMessagePublisher<T> GetPublisher<T>(string topic)
        {
            var options = new OptionsWrapper<KafkaPublisherConfig<T>>(GetPublisherConfig<T>(topic));
            return new KafkaMessagePublisher<T>(options, new JsonEventFormatter());
        }

        private KafkaPublisherConfig<T> GetPublisherConfig<T>(string topic)
        {
            return new()
            {
                Topic = topic,
                BootstrapServers = $"{_fixture.Hostname}:9092",
            };
        }

        private IApplicationNameService GetApplicationNameService(string source = "test://non")
        {
            var mock = new Mock<IApplicationNameService>();
            mock.Setup(t => t.GetSource()).Returns(new Uri(source));
            return mock.Object;
        }

        private KafkaConsumerConfig<T> GetConsumerConfig<T>(string topic, string groupId = "group_id")
        {
            return new()
            {
                Topic = topic,
                GroupId = groupId,
                CommitPeriod = 1,
                BootstrapServers = $"{_fixture.Hostname}:9092",
                EnableAutoCommit = false,
                StatisticsIntervalMs = 5000,
                SessionTimeoutMs = 6000,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };
        }
    }
}
