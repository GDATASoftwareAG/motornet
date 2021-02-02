using System;
using System.Text;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.SQS;
using Motor.Extensions.Hosting.SQS.Options;
using RandomDataGenerator.FieldOptions;
using RandomDataGenerator.Randomizers;
using Xunit;

namespace Motor.Extensions.Hosting.SQS_IntegrationTest
{
    [Collection("SQSMessage")]
    public class SQSMessageConsumerTests : IClassFixture<SQSFixture>
    {
        private readonly IRandomizerString _randomizerString;
        private readonly string _baseSQSUrl;

        public SQSMessageConsumerTests(SQSFixture fixture)
        {
            _randomizerString = RandomizerFactory.GetRandomizer(new FieldOptionsTextRegex {Pattern = @"^[A-Z]{10}"});
            _baseSQSUrl = $"http://{fixture.Hostname}:{fixture.Port}";
        }

        [Fact(Timeout = 50000)]
        public async Task Consume_RawPublishIntoSQSAndConsumeCreateCloudEvent_ConsumedEqualsPublished()
        {
            const string expectedMessage = "testMessage";
            var queueName = _randomizerString.Generate();
            var clientOptions = GetSqsClientOptions(queueName);
            
            var sqs = new SQSClientFactory().From(clientOptions);
            
            await sqs.CreateQueueAsync(queueName);
            await PublishMessage(sqs, queueName, expectedMessage);

            var consumer = GetConsumer<string>(new OptionsWrapper<SQSClientOptions>(clientOptions), $"{_baseSQSUrl}/queue/{queueName}");
            var rawConsumedSQSMessage = await RawConsumedSqsMessage(consumer);
            Assert.Equal(expectedMessage, Encoding.UTF8.GetString(rawConsumedSQSMessage));
        }
        
        private static async Task<byte[]> RawConsumedSqsMessage(SQSConsumer<string> consumer)
        {
            var rawConsumedSQSMessage = (byte[]) null;
            var taskCompletionSource = new TaskCompletionSource();
            consumer.ConsumeCallbackAsync = async (dataEvent, _) =>
            {
                rawConsumedSQSMessage = dataEvent.TypedData;
                taskCompletionSource.TrySetResult();
                return await Task.FromResult(ProcessedMessageStatus.Success);
            };

            await consumer.StartAsync();
            var consumerStartTask = consumer.ExecuteAsync();

            await Task.WhenAny(consumerStartTask, taskCompletionSource.Task);
            return rawConsumedSQSMessage;
        }

        private SQSClientOptions GetSqsClientOptions(string queueName)
        {
            var clientOptions = new SQSClientOptions
            {
                ServiceUrl = $"{_baseSQSUrl}",
                QueueUrl = $"{_baseSQSUrl}/queue/{queueName}"
            };
            return clientOptions;
        }

        private async Task PublishMessage(IAmazonSQS sqsClient, string queue, string message)
        {
            await sqsClient.SendMessageAsync(new SendMessageRequest
            {
                QueueUrl = $"{_baseSQSUrl}/queue/{queue}",
                MessageBody = message
            });
        }

        private SQSConsumer<T> GetConsumer<T>(IOptions<SQSClientOptions> clientOptions, string queueName)
        {
            var fakeLoggerMock = Mock.Of<ILogger<SQSConsumer<T>>>();
            return new SQSConsumer<T>(clientOptions, fakeLoggerMock, GetApplicationNameService(),
                new SQSClientFactory());
        }
        
        private IApplicationNameService GetApplicationNameService(string source = "test://non")
        {
            var mock = new Mock<IApplicationNameService>();
            mock.Setup(t => t.GetSource()).Returns(new Uri(source));
            return mock.Object;
        }
    }
}
