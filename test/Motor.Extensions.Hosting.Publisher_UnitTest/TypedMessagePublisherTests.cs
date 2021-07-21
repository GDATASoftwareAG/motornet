using System.Threading;
using System.Threading.Tasks;
using Moq;
using Motor.Extensions.ContentEncoding.Abstractions;
using Motor.Extensions.Conversion.Abstractions;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
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
            var encoder = new Mock<IMessageEncoder>();
            var serializedBytes = new byte[] { 1, 2, 3, 4 };
            var encodedBytes = new byte[] { 4, 3, 2, 1 };
            serializer.Setup(t => t.Serialize(It.IsAny<string>())).Returns(serializedBytes);
            encoder.Setup(t => t.EncodeAsync(serializedBytes, It.IsAny<CancellationToken>()))
                .ReturnsAsync(encodedBytes);
            encoder.SetupGet(c => c.Encoding).Returns("someEncoding");
            var typedMessagePublisher = CreateTypedMessagePublisher(publisher.Object, serializer.Object, encoder.Object);
            var motorEvent = MotorCloudEvent.CreateTestCloudEvent("test");

            await typedMessagePublisher.PublishMessageAsync(motorEvent);

            publisher.Verify(t => t.PublishMessageAsync(
                It.Is<MotorCloudEvent<byte[]>>(it => it.Data == encodedBytes),
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
            IMessageEncoder? encoder = null)
        {
            publisher ??= Mock.Of<ITypedMessagePublisher<byte[]>>();
            serializer ??= Mock.Of<IMessageSerializer<string>>();
            if (encoder is null)
            {
                var fakeEncoder = new Mock<IMessageEncoder>();
                fakeEncoder.SetupGet(c => c.Encoding).Returns("someEncoder");
                encoder = fakeEncoder.Object;
            }
            return new TypedMessagePublisher<string, ITypedMessagePublisher<byte[]>>(null, publisher, serializer,
                encoder);
        }
    }
}
