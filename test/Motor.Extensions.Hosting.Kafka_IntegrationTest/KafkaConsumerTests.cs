using System;
using System.Text;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.Kafka;
using Xunit;

namespace Motor.Extensions.Hosting.Kafka_IntegrationTest
{
    [Collection("Kafka")]
    public class KafkaConsumerTests : IClassFixture<KafkaFixture>
    {
        private readonly KafkaFixture _fixture;

        public KafkaConsumerTests(KafkaFixture fixture)
        {
            _fixture = fixture;
        }

        //[Fact(Timeout = 50000)]
        [Fact(Skip = "Times out")]
        public async Task Consume_PublishIntoKafkaAndConsume_ConsumedEqualsPublished()
        {
            const string topic = "test_topic_123";
            const string message = "testMessage";
            await PublishMessage(topic, "someKey", message);
            var consumer = GetConsumer<string>(topic);
            var rawConsumedKafkaMessage = (byte[]) null;
            consumer.ConsumeCallbackAsync = async (dataEvent, _token) =>
            {
                rawConsumedKafkaMessage = dataEvent.TypedData;
                return await Task.FromResult(ProcessedMessageStatus.Success);
            };

            await consumer.StartAsync();
            var consumerStartTask = consumer.ExecuteAsync();

            await Task.WhenAny(consumerStartTask, Task.Delay(TimeSpan.FromSeconds(15)));
            Assert.Equal(message, Encoding.UTF8.GetString(rawConsumedKafkaMessage));
        }

        private async Task PublishMessage(string topic, string key, string value)
        {
            using var producer = new ProducerBuilder<string, string>(new ProducerConfig
                {BootstrapServers = $"{_fixture.Hostname}:9092"}).Build();
            await producer.ProduceAsync(topic, new Message<string, string> {Key = key, Value = value});
            producer.Flush();
        }

        private KafkaConsumer<T> GetConsumer<T>(string topic)
        {
            var fakeLoggerMock = new Mock<ILogger<KafkaConsumer<T>>>();
            var configMock = new Mock<IOptions<KafkaConsumerConfig<T>>>();
            configMock.Setup(x => x.Value).Returns(GetConsumerConfig<T>(topic));
            return new KafkaConsumer<T>(fakeLoggerMock.Object, configMock.Object, null, GetApplicationNameService());
        }

        public IApplicationNameService GetApplicationNameService(string source = "test://non")
        {
            var mock = new Mock<IApplicationNameService>();
            mock.Setup(t => t.GetSource()).Returns(new Uri(source));
            return mock.Object;
        }

        private KafkaConsumerConfig<T> GetConsumerConfig<T>(string topic, string groupId = "group_id")
        {
            return new KafkaConsumerConfig<T>
            {
                Topic = topic,
                GroupId = groupId,
                BootstrapServers = $"{_fixture.Hostname}:9092",
                EnableAutoCommit = false,
                StatisticsIntervalMs = 5000,
                SessionTimeoutMs = 6000,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };
        }
    }
}
