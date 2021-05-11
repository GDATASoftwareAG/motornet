using System.Threading;
using System.Threading.Tasks;
using Moq;
using Motor.Extensions.Compression.Abstractions;
using Motor.Extensions.Conversion.Abstractions;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.Publisher;
using Motor.Extensions.TestUtilities;
using Xunit;

namespace Motor.Extensions.Hosting.Publisher_UnitTest
{
    public class TypedMessagePublisherTests
    {
        [Fact]
        public async Task PublishMessageAsync_MessageToSerialize_SerializedMessageIsPublished()
        {
            var publisher = new Mock<ITypedMessagePublisher<byte[]>>();
            var serializer = new Mock<IMessageSerializer<string>>();
            var compressor = new Mock<IMessageCompressor>();
            var serializedBytes = new byte[] { 1, 2, 3, 4 };
            var compressedBytes = new byte[] { 4, 3, 2, 1 };
            serializer.Setup(t => t.Serialize(It.IsAny<string>())).Returns(serializedBytes);
            compressor.Setup(t => t.CompressAsync(serializedBytes, It.IsAny<CancellationToken>()))
                .ReturnsAsync(compressedBytes);
            compressor.SetupGet(c => c.CompressionType).Returns("someCompressionType");
            var typedMessagePublisher = CreateTypedMessagePublisher(publisher.Object, serializer.Object, compressor.Object);
            var motorEvent = MotorCloudEvent.CreateTestCloudEvent("test");

            await typedMessagePublisher.PublishMessageAsync(motorEvent);

            publisher.Verify(t => t.PublishMessageAsync(
                It.Is<MotorCloudEvent<byte[]>>(it => it.Data == compressedBytes),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task PublishMessageAsync_ContextToPassed_ContextPassed()
        {
            var publisher = new Mock<ITypedMessagePublisher<byte[]>>();
            var typedMessagePublisher = CreateTypedMessagePublisher(publisher.Object);
            var motorEvent = MotorCloudEvent.CreateTestCloudEvent("test");

            await typedMessagePublisher.PublishMessageAsync(motorEvent);

            publisher.Verify(t => t.PublishMessageAsync(
                It.Is<MotorCloudEvent<byte[]>>(it => it.Id == motorEvent.Id),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        private static TypedMessagePublisher<string, ITypedMessagePublisher<byte[]>> CreateTypedMessagePublisher(
            ITypedMessagePublisher<byte[]>? publisher = null, IMessageSerializer<string>? serializer = null,
            IMessageCompressor? compressor = null)
        {
            publisher ??= Mock.Of<ITypedMessagePublisher<byte[]>>();
            serializer ??= Mock.Of<IMessageSerializer<string>>();
            if (compressor is null)
            {
                var fakeCompressor = new Mock<IMessageCompressor>();
                fakeCompressor.SetupGet(c => c.CompressionType).Returns("someCompressionType");
                compressor = fakeCompressor.Object;
            }
            return new TypedMessagePublisher<string, ITypedMessagePublisher<byte[]>>(null, publisher, serializer,
                compressor);
        }
    }
}
