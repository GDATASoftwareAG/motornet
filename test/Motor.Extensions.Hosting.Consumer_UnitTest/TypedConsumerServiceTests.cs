using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Motor.Extensions.ContentEncoding.Abstractions;
using Motor.Extensions.Conversion.Abstractions;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using Motor.Extensions.Hosting.Consumer;
using Motor.Extensions.TestUtilities;
using Xunit;

namespace Motor.Extensions.Hosting.Consumer_UnitTest
{
    public class TypedConsumerServiceTests
    {
        [Fact]
        public async Task SingleMessageConsumeAsync_SomeEvent_PreprocessedEventAddedToQueue()
        {
            var inputMessage = new byte[] { 1, 2, 3 };
            var inputEvent = MotorCloudEvent.CreateTestCloudEvent(inputMessage);
            var fakeDecoder = new Mock<IMessageDecoder>();
            var decodedMessage = new byte[] { 4, 5, 6 };
            fakeDecoder.Setup(f => f.DecodeAsync(inputMessage, It.IsAny<CancellationToken>()))
                .ReturnsAsync(decodedMessage);
            fakeDecoder.Setup(m => m.Encoding).Returns(NoOpMessageEncoder.NoEncoding);
            var preprocessedMessage = "test";
            var fakeDeserializer = new Mock<IMessageDeserializer<string>>();
            fakeDeserializer.Setup(f => f.Deserialize(decodedMessage)).Returns(preprocessedMessage);
            var mockQueue = new Mock<IBackgroundTaskQueue<MotorCloudEvent<string>>>();
            var fakeMessageConsumer = new Mock<IMessageConsumer<string>>();
            fakeMessageConsumer.SetupProperty(p => p.ConsumeCallbackAsync);
            CreateConsumerService(mockQueue.Object, fakeDeserializer.Object, fakeDecoder.Object,
                fakeMessageConsumer.Object);

            await fakeMessageConsumer.Object.ConsumeCallbackAsync?.Invoke(inputEvent, CancellationToken.None)!;

            mockQueue.Verify(m =>
                m.QueueBackgroundWorkItem(
                    It.Is<MotorCloudEvent<string>>(cloudEvent => cloudEvent.TypedData == preprocessedMessage)));
        }

        [Fact]
        public async Task SingleMessageConsumeAsync_UnknownEncodingInInput_InvalidInput()
        {
            var inputEvent = CreateMotorCloudEventWithEncoding("unknown-encoding");
            var fakeDecoder = new Mock<IMessageDecoder>();
            fakeDecoder.Setup(m => m.Encoding).Returns("other-encoding");
            var fakeMessageConsumer = new Mock<IMessageConsumer<string>>();
            fakeMessageConsumer.SetupProperty(p => p.ConsumeCallbackAsync);
            CreateConsumerService(decoder: fakeDecoder.Object, consumer: fakeMessageConsumer.Object);

            var actual =
                await fakeMessageConsumer.Object.ConsumeCallbackAsync?.Invoke(inputEvent, CancellationToken.None)!;

            Assert.Equal(ProcessedMessageStatus.InvalidInput, actual);
        }

        [Fact]
        public async Task SingleMessageConsumeAsync_MultipleDecoders_Success()
        {
            const string usedEncoding = "used-encoding";
            var inputEvent = CreateMotorCloudEventWithEncoding(usedEncoding);
            var unusedDecoder = CreateDecoder("unused-encoding");
            var usedDecoder = CreateDecoder(usedEncoding);
            var fakeMessageConsumer = new Mock<IMessageConsumer<string>>();
            fakeMessageConsumer.SetupProperty(p => p.ConsumeCallbackAsync);
            CreateConsumerService(
                decoders: new List<IMessageDecoder> { unusedDecoder.Object, usedDecoder.Object },
                consumer: fakeMessageConsumer.Object);

            var actual =
                await fakeMessageConsumer.Object.ConsumeCallbackAsync?.Invoke(inputEvent, CancellationToken.None)!;

            Assert.Equal(ProcessedMessageStatus.Success, actual);
            usedDecoder.Verify(m => m.DecodeAsync(inputEvent.TypedData, It.IsAny<CancellationToken>()));
            unusedDecoder.Verify(m => m.DecodeAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private static MotorCloudEvent<byte[]> CreateMotorCloudEventWithEncoding(string encoding,
        byte[]? inputMessage = null)
        {
            var cloudEvent = MotorCloudEvent.CreateTestCloudEvent(inputMessage ?? Array.Empty<byte>());
            cloudEvent.SetEncoding(encoding);
            return cloudEvent;
        }

        private static Mock<IMessageDecoder> CreateDecoder(string encoding, byte[]? expectedInput = null,
            byte[]? decodedOutput = null)
        {
            var fakeDecoder = new Mock<IMessageDecoder>();
            fakeDecoder.Setup(f =>
                    f.DecodeAsync(expectedInput ?? It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(decodedOutput ?? Array.Empty<byte>());
            fakeDecoder.Setup(m => m.Encoding).Returns(encoding);
            return fakeDecoder;
        }

        private static void CreateConsumerService(IBackgroundTaskQueue<MotorCloudEvent<string>>? queue = null,
            IMessageDeserializer<string>? deserializer = null,
            IMessageDecoder? decoder = null,
            IMessageConsumer<string>? consumer = null)
        {
            CreateConsumerService(queue, deserializer,
                decoder is null ? null : new List<IMessageDecoder> { decoder }, consumer);
        }

        private static void CreateConsumerService(IBackgroundTaskQueue<MotorCloudEvent<string>>? queue = null,
            IMessageDeserializer<string>? deserializer = null,
            IEnumerable<IMessageDecoder>? decoders = null,
            IMessageConsumer<string>? consumer = null)
        {
            queue ??= Mock.Of<IBackgroundTaskQueue<MotorCloudEvent<string>>>();
            deserializer ??= Mock.Of<IMessageDeserializer<string>>();
            decoders ??= new List<IMessageDecoder> { Mock.Of<IMessageDecoder>() };
            consumer ??= Mock.Of<IMessageConsumer<string>>();
            var _ = new TypedConsumerService<string>(Mock.Of<ILogger<TypedConsumerService<string>>>(), null, queue,
                deserializer, decoders, consumer);
        }
    }
}
