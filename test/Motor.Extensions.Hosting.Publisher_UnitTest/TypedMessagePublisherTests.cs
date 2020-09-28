using System.Threading;
using System.Threading.Tasks;
using Moq;
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
        public async Task PublishMessageAsync_MessageToSerialize_IsCalledSerializedMessage()
        {
            var publisher = new Mock<ITypedMessagePublisher<byte[]>>();
            var serializer = new Mock<IMessageSerializer<string>>();
            var typedMessagePublisher =
                new TypedMessagePublisher<string, ITypedMessagePublisher<byte[]>>(null, publisher.Object,
                    serializer.Object);
            var motorEvent = MotorCloudEvent.CreateTestCloudEvent("test");

            await typedMessagePublisher.PublishMessageAsync(motorEvent);

            serializer.Verify(t => t.Serialize("test"), Times.Once);
        }

        [Fact]
        public async Task PublishMessageAsync_MessageToSerialize_SerializedMessageIsPublished()
        {
            var publisher = new Mock<ITypedMessagePublisher<byte[]>>();
            var serializer = new Mock<IMessageSerializer<string>>();
            var bytes = new byte[] {1, 2, 3, 4};
            serializer.Setup(t => t.Serialize(It.IsAny<string>())).Returns(bytes);
            var typedMessagePublisher =
                new TypedMessagePublisher<string, ITypedMessagePublisher<byte[]>>(null, publisher.Object,
                    serializer.Object);
            var motorEvent = MotorCloudEvent.CreateTestCloudEvent("test");

            await typedMessagePublisher.PublishMessageAsync(motorEvent);

            publisher.Verify(t => t.PublishMessageAsync(
                It.Is<MotorCloudEvent<byte[]>>(it => it.Data == bytes),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task PublishMessageAsync_ContextToPassed_ContextPassed()
        {
            var publisher = new Mock<ITypedMessagePublisher<byte[]>>();
            var serializer = new Mock<IMessageSerializer<string>>();
            var typedMessagePublisher =
                new TypedMessagePublisher<string, ITypedMessagePublisher<byte[]>>(null, publisher.Object,
                    serializer.Object);
            var motorEvent = MotorCloudEvent.CreateTestCloudEvent("test");

            await typedMessagePublisher.PublishMessageAsync(motorEvent);

            publisher.Verify(t => t.PublishMessageAsync(
                It.Is<MotorCloudEvent<byte[]>>(it => it.Id == motorEvent.Id),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
