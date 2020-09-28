using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Motor.Extensions.Diagnostics.Metrics.Abstractions;
using Motor.Extensions.Hosting;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.TestUtilities;
using Xunit;

namespace Motor.Extensions.Hosting_UnitTest
{
    public class MultiResultMessageHandlerTests
    {
        private static Mock<ILogger<MessageHandler<string, string>>> FakeLogger =>
            new Mock<ILogger<MessageHandler<string, string>>>();

        private static Mock<IMultiResultMessageConverter<string, string>> FakeConverter =>
            new Mock<IMultiResultMessageConverter<string, string>>();

        private static Mock<IMetricsFactory<MessageHandler<string, string>>> FakeMetrics =>
            new Mock<IMetricsFactory<MessageHandler<string, string>>>();

        private static Mock<ITypedMessagePublisher<string>> FakePublisher => new Mock<ITypedMessagePublisher<string>>();


        [Fact]
        public void Ctor_WithMetricsFactory_SummaryIsCreated()
        {
            var metricsFactoryMock = FakeMetrics;

            GetMessageHandler(metrics: metricsFactoryMock.Object);

            metricsFactoryMock.Verify(x =>
                x.CreateSummary("message_processing", "Message processing duration in ms"));
        }

        [Fact]
        public async Task HandleMessageAsync_WithContextAndInput_HasContext()
        {
            var converterMock = FakeConverter;
            var context = CreateMotorEvent("message");
            var messageHandler = GetMessageHandler(converter: converterMock.Object);

            await messageHandler.HandleMessageAsync(context);

            converterMock.Verify(x => x.ConvertMessageAsync(context, It.IsAny<CancellationToken>()), Times.Exactly(1));
        }

        [Fact]
        public async Task HandleMessageAsync_ConverterThrowsArgumentException_ThrowsArgumentException()
        {
            var converterMock = FakeConverter;
            converterMock.Setup(x =>
                    x.ConvertMessageAsync(It.IsAny<MotorCloudEvent<string>>(), It.IsAny<CancellationToken>()))
                .Throws(new ArgumentException("argException"));
            var messageHandler = GetMessageHandler(converter: converterMock.Object);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                messageHandler.HandleMessageAsync(CreateMotorEvent("message_1")));
        }

        [Fact]
        public async Task HandleMessageAsync_ConverterThrowsSomeException_TemporaryFailureResult()
        {
            var converterMock = FakeConverter;
            converterMock.Setup(x =>
                    x.ConvertMessageAsync(It.IsAny<MotorCloudEvent<string>>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception("someException"));
            var messageHandler = GetMessageHandler(converter: converterMock.Object);

            var actual = await messageHandler.HandleMessageAsync(CreateMotorEvent("message_3"));

            Assert.Equal(ProcessedMessageStatus.TemporaryFailure, actual);
        }

        [Fact]
        public async Task HandleMessageAsync_ConverterReturnsNull_ReturnWithSuccess()
        {
            var converterMock = FakeConverter;
            converterMock.Setup(x =>
                    x.ConvertMessageAsync(It.IsAny<MotorCloudEvent<string>>(), It.IsAny<CancellationToken>()))
                .Returns(CreateReturnValues((string) null));
            var publisherMock = FakePublisher;
            var messageHandler = GetMessageHandler(converter: converterMock.Object,
                publisher: publisherMock.Object);

            var actual = await messageHandler.HandleMessageAsync(CreateMotorEvent("message_5"));

            Assert.Equal(ProcessedMessageStatus.Success, actual);
            publisherMock.Verify(
                x => x.PublishMessageAsync(It.IsAny<MotorCloudEvent<string>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task HandleMessageAsync_ConverterReturnIsEmpty_ReturnWithSuccess()
        {
            var converterMock = FakeConverter;
            converterMock.Setup(x =>
                    x.ConvertMessageAsync(It.IsAny<MotorCloudEvent<string>>(), It.IsAny<CancellationToken>()))
                .Returns(CreateReturnValues());
            var publisherMock = FakePublisher;
            var messageHandler = GetMessageHandler(converter: converterMock.Object, publisher: publisherMock.Object);

            var actual = await messageHandler.HandleMessageAsync(CreateMotorEvent("message_5"));

            Assert.Equal(ProcessedMessageStatus.Success, actual);
            publisherMock.Verify(
                x => x.PublishMessageAsync(It.IsAny<MotorCloudEvent<string>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task HandleMessageAsync_ConverterReturnsSomeResult_ReturnWithSuccess()
        {
            var converterMock = FakeConverter;
            converterMock.Setup(x =>
                    x.ConvertMessageAsync(It.IsAny<MotorCloudEvent<string>>(), It.IsAny<CancellationToken>()))
                .Returns(CreateReturnValues("someResult"));
            var publisherMock = FakePublisher;
            var messageHandler = GetMessageHandler(converter: converterMock.Object, publisher: publisherMock.Object);

            var actual = await messageHandler.HandleMessageAsync(CreateMotorEvent("message_6"));

            Assert.Equal(ProcessedMessageStatus.Success, actual);
        }


        [Fact]
        public async Task HandleMessageAsync_ConverterReturnsMultipleResults_PublisherIsCalledWithEachResult()
        {
            const string converterResult1 = "someOtherResult1";
            const string converterResult2 = "someOtherResult2";
            const string converterResult3 = "someOtherResult3";
            var converterMock = FakeConverter;
            converterMock.Setup(x =>
                    x.ConvertMessageAsync(It.IsAny<MotorCloudEvent<string>>(), It.IsAny<CancellationToken>()))
                .Returns(CreateReturnValues(converterResult1, converterResult2, converterResult3));
            var publisherMock = FakePublisher;
            var messageHandler = GetMessageHandler(converter: converterMock.Object, publisher: publisherMock.Object);

            await messageHandler.HandleMessageAsync(CreateMotorEvent("message_6"));

            publisherMock.Verify(
                x => x.PublishMessageAsync(It.IsAny<MotorCloudEvent<string>>(), It.IsAny<CancellationToken>()),
                Times.Exactly(3));
            publisherMock.Verify(
                x => x.PublishMessageAsync(It.Is<MotorCloudEvent<string>>(t => t.TypedData == converterResult1),
                    It.IsAny<CancellationToken>()), Times.Once);
            publisherMock.Verify(
                x => x.PublishMessageAsync(It.Is<MotorCloudEvent<string>>(t => t.TypedData == converterResult2),
                    It.IsAny<CancellationToken>()), Times.Once);
            publisherMock.Verify(
                x => x.PublishMessageAsync(It.Is<MotorCloudEvent<string>>(t => t.TypedData == converterResult3),
                    It.IsAny<CancellationToken>()), Times.Once);
            ;
        }

        private Task<IEnumerable<MotorCloudEvent<string>>> CreateReturnValues(params string[] data)
        {
            return Task.FromResult(data.Select(t => MotorCloudEvent.CreateTestCloudEvent(t, new Uri("test://non"))));
        }

        private MultiResultMessageHandler<string, string> GetMessageHandler(
            ILogger<MessageHandler<string, string>> logger = null,
            IMetricsFactory<MessageHandler<string, string>> metrics = null,
            IMultiResultMessageConverter<string, string> converter = null,
            ITypedMessagePublisher<string> publisher = null)
        {
            logger ??= FakeLogger.Object;
            converter ??= FakeConverter.Object;
            publisher ??= FakePublisher.Object;

            return new MultiResultMessageHandler<string, string>(logger, metrics, converter, publisher);
        }

        private static MotorCloudEvent<string> CreateMotorEvent(string data = null)
        {
            return MotorCloudEvent.CreateTestCloudEvent(data, new Uri("test://non"));
        }
    }
}
