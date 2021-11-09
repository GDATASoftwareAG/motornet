using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Moq;
using Motor.Extensions.ContentEncoding.Abstractions;
using Motor.Extensions.Conversion.Abstractions;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using Motor.Extensions.Hosting.Publisher;
using Motor.Extensions.TestUtilities;
using Xunit;

namespace Motor.Extensions.Hosting.Publisher_UnitTest;

public class TypedMessagePublisherTests
{
    [Fact]
    public async Task PublishMessageAsync_MessageToSerialize_SerializedMessageIsPublished()
    {
        var publisher = new Mock<IRawMessagePublisher<string>>();
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
        var publisher = new Mock<IRawMessagePublisher<string>>();
        var typedMessagePublisher = CreateTypedMessagePublisher(publisher.Object);
        var motorEvent = MotorCloudEvent.CreateTestCloudEvent("test");

        await typedMessagePublisher.PublishMessageAsync(motorEvent);

        publisher.Verify(t => t.PublishMessageAsync(
            It.Is<MotorCloudEvent<byte[]>>(it => it.Id == motorEvent.Id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishMessageAsync_CloudEventOfTypeString_PublishedCloudEventHasTypeString()
    {
        var bytesPublisher = new Mock<IRawMessagePublisher<string>>();
        var typedMessagePublisher = CreateTypedMessagePublisher(bytesPublisher.Object);
        var motorEvent = MotorCloudEvent.CreateTestCloudEvent("test");

        await typedMessagePublisher.PublishMessageAsync(motorEvent);

        bytesPublisher.Verify(t => t.PublishMessageAsync(
            It.Is<MotorCloudEvent<byte[]>>(it => it.Type == motorEvent.Type),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishMessageAsync_CloudEventWithContentEncoding_PublishedCloudEventHasDefaultEncoding()
    {
        var bytesPublisher = new Mock<IRawMessagePublisher<string>>();
        var typedMessagePublisher = CreateTypedMessagePublisher(bytesPublisher.Object, encoder: new NoOpMessageEncoder());
        var motorEvent = MotorCloudEvent.CreateTestCloudEvent("test");
        motorEvent.SetEncoding("some-encoding");

        await typedMessagePublisher.PublishMessageAsync(motorEvent);

        bytesPublisher.Verify(t => t.PublishMessageAsync(
            It.Is<MotorCloudEvent<byte[]>>(it => it.GetEncoding() == NoOpMessageEncoder.NoEncoding),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishMessageAsync_CloudEventWithContentEncodingIgnored_PublishedCloudEventHasSameEncoding()
    {
        var bytesPublisher = new Mock<IRawMessagePublisher<string>>();
        var typedMessagePublisher = CreateTypedMessagePublisher(bytesPublisher.Object,
            encoder: new NoOpMessageEncoder(), ignoreEncoding: true);
        var motorEvent = MotorCloudEvent.CreateTestCloudEvent("test");
        motorEvent.SetEncoding("some-encoding");

        await typedMessagePublisher.PublishMessageAsync(motorEvent);

        bytesPublisher.Verify(t => t.PublishMessageAsync(
            It.Is<MotorCloudEvent<byte[]>>(it => it.GetEncoding() == motorEvent.GetEncoding()),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static TypedMessagePublisher<string, IRawMessagePublisher<string>> CreateTypedMessagePublisher(
        IRawMessagePublisher<string>? publisher = null, IMessageSerializer<string>? serializer = null,
        IMessageEncoder? encoder = null, bool ignoreEncoding = false)
    {
        publisher ??= Mock.Of<IRawMessagePublisher<string>>();
        serializer ??= Mock.Of<IMessageSerializer<string>>();
        if (encoder is null)
        {
            var fakeEncoder = new Mock<IMessageEncoder>();
            fakeEncoder.SetupGet(c => c.Encoding).Returns("someEncoder");
            encoder = fakeEncoder.Object;
        }

        var encodingOptions = new ContentEncodingOptions { IgnoreEncoding = ignoreEncoding };
        return new TypedMessagePublisher<string, IRawMessagePublisher<string>>(null, publisher, serializer,
            Options.Create(encodingOptions), encoder);
    }
}
