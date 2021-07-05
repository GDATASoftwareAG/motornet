using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Motor.Extensions.Compression.Abstractions;
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
            var fakeDecompressor = new Mock<IMessageDecompressor>();
            var decompressedMessage = new byte[] { 4, 5, 6 };
            fakeDecompressor.Setup(f => f.DecompressAsync(inputMessage, It.IsAny<CancellationToken>()))
                .ReturnsAsync(decompressedMessage);
            fakeDecompressor.Setup(m => m.CompressionType).Returns(NoOpMessageCompressor.NoOpCompressionType);
            var preprocessedMessage = "test";
            var fakeDeserializer = new Mock<IMessageDeserializer<string>>();
            fakeDeserializer.Setup(f => f.Deserialize(decompressedMessage)).Returns(preprocessedMessage);
            var mockQueue = new Mock<IBackgroundTaskQueue<MotorCloudEvent<string>>>();
            var fakeMessageConsumer = new Mock<IMessageConsumer<string>>();
            fakeMessageConsumer.SetupProperty(p => p.ConsumeCallbackAsync);
            CreateConsumerService(mockQueue.Object, fakeDeserializer.Object, fakeDecompressor.Object,
                fakeMessageConsumer.Object);

            await fakeMessageConsumer.Object.ConsumeCallbackAsync?.Invoke(inputEvent, CancellationToken.None)!;

            mockQueue.Verify(m =>
                m.QueueBackgroundWorkItem(
                    It.Is<MotorCloudEvent<string>>(cloudEvent => cloudEvent.TypedData == preprocessedMessage)));
        }

        [Fact]
        public async Task SingleMessageConsumeAsync_UnknownCompressionTypeInInput_InvalidInput()
        {
            var inputEvent = CreateMotorCloudEventWithCompressionType("unknown-compression-type");
            var fakeDecompressor = new Mock<IMessageDecompressor>();
            fakeDecompressor.Setup(m => m.CompressionType).Returns("other-compression-type");
            var fakeMessageConsumer = new Mock<IMessageConsumer<string>>();
            fakeMessageConsumer.SetupProperty(p => p.ConsumeCallbackAsync);
            CreateConsumerService(decompressor: fakeDecompressor.Object, consumer: fakeMessageConsumer.Object);

            var actual = await fakeMessageConsumer.Object.ConsumeCallbackAsync?.Invoke(inputEvent, CancellationToken.None)!;

            Assert.Equal(ProcessedMessageStatus.InvalidInput, actual);
        }

        [Fact]
        public async Task SingleMessageConsumeAsync_MultipleDecompressors_Success()
        {
            const string usedCompressionType = "used-compression";
            var inputEvent = CreateMotorCloudEventWithCompressionType(usedCompressionType);
            var unusedDecompressor = CreateDecompressor("unused-compression");
            var usedDecompressor = CreateDecompressor(usedCompressionType);
            var fakeMessageConsumer = new Mock<IMessageConsumer<string>>();
            fakeMessageConsumer.SetupProperty(p => p.ConsumeCallbackAsync);
            CreateConsumerService(
                decompressors: new List<IMessageDecompressor> { unusedDecompressor.Object, usedDecompressor.Object },
                consumer: fakeMessageConsumer.Object);

            var actual = await fakeMessageConsumer.Object.ConsumeCallbackAsync?.Invoke(inputEvent, CancellationToken.None)!;

            Assert.Equal(ProcessedMessageStatus.Success, actual);
            usedDecompressor.Verify(m => m.DecompressAsync(inputEvent.TypedData, It.IsAny<CancellationToken>()));
            unusedDecompressor.Verify(m => m.DecompressAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private static MotorCloudEvent<byte[]> CreateMotorCloudEventWithCompressionType(string compressionType,
        byte[]? inputMessage = null)
        {
            var cloudEvent = MotorCloudEvent.CreateTestCloudEvent(inputMessage ?? Array.Empty<byte>());
            cloudEvent.SetCompressionType(compressionType);
            return cloudEvent;
        }

        private static Mock<IMessageDecompressor> CreateDecompressor(string compressionType, byte[]? expectedInput = null,
            byte[]? decompressedOutput = null)
        {
            var fakeDecompressor = new Mock<IMessageDecompressor>();
            fakeDecompressor.Setup(f =>
                    f.DecompressAsync(expectedInput ?? It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(decompressedOutput ?? Array.Empty<byte>());
            fakeDecompressor.Setup(m => m.CompressionType).Returns(compressionType);
            return fakeDecompressor;
        }

        private static void CreateConsumerService(IBackgroundTaskQueue<MotorCloudEvent<string>>? queue = null,
            IMessageDeserializer<string>? deserializer = null,
            IMessageDecompressor? decompressor = null,
            IMessageConsumer<string>? consumer = null)
        {
            CreateConsumerService(queue, deserializer,
                decompressor is null ? null : new List<IMessageDecompressor> { decompressor }, consumer);
        }

        private static void CreateConsumerService(IBackgroundTaskQueue<MotorCloudEvent<string>>? queue = null,
            IMessageDeserializer<string>? deserializer = null,
            IEnumerable<IMessageDecompressor>? decompressors = null,
            IMessageConsumer<string>? consumer = null)
        {
            queue ??= Mock.Of<IBackgroundTaskQueue<MotorCloudEvent<string>>>();
            deserializer ??= Mock.Of<IMessageDeserializer<string>>();
            decompressors ??= new List<IMessageDecompressor> { Mock.Of<IMessageDecompressor>() };
            consumer ??= Mock.Of<IMessageConsumer<string>>();
            var _ = new TypedConsumerService<string>(Mock.Of<ILogger<TypedConsumerService<string>>>(), null, queue,
                deserializer, decompressors, consumer);
        }
    }
}
