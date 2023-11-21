using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Motor.Extensions.Diagnostics.Metrics.Abstractions;
using Motor.Extensions.Hosting;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using Motor.Extensions.TestUtilities;
using Prometheus.Client;
using Xunit;

namespace Motor.Extensions.Hosting_UnitTest;

public class MultiOutputServiceAdapterTests
{

    private static Mock<IMultiOutputService<string, string>> FakeService =>
        new();

    private static Mock<ITypedMessagePublisher<string>> FakePublisher => new();

    [Fact]
    public async Task HandleMessageAsync_WithContextAndInput_HasContext()
    {
        var converterMock = FakeService;
        var context = CreateMotorEvent("message");
        var messageHandler = GetMessageHandler(service: converterMock.Object);

        await messageHandler.HandleMessageAsync(context).ConfigureAwait(false);

        converterMock.Verify(x => x.ConvertMessageAsync(context, It.IsAny<CancellationToken>()), Times.Exactly(1));
    }

    [Fact]
    public async Task HandleMessageAsync_ConverterThrowsArgumentException_ThrowsArgumentException()
    {
        var converterMock = FakeService;
        converterMock.Setup(x =>
                x.ConvertMessageAsync(It.IsAny<MotorCloudEvent<string>>(), It.IsAny<CancellationToken>()))
            .Throws(new ArgumentException("argException"));
        var messageHandler = GetMessageHandler(service: converterMock.Object);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            messageHandler.HandleMessageAsync(CreateMotorEvent("message_1"))).ConfigureAwait(false);
    }

    [Fact]
    public async Task HandleMessageAsync_ConverterThrowsSomeException_TemporaryFailureResult()
    {
        var converterMock = FakeService;
        converterMock.Setup(x =>
                x.ConvertMessageAsync(It.IsAny<MotorCloudEvent<string>>(), It.IsAny<CancellationToken>()))
            .Throws(new Exception("someException"));
        var messageHandler = GetMessageHandler(service: converterMock.Object);

        var actual = await messageHandler.HandleMessageAsync(CreateMotorEvent("message_3")).ConfigureAwait(false);

        Assert.Equal(ProcessedMessageStatus.TemporaryFailure, actual);
    }

    [Fact]
    public async Task HandleMessageAsync_ConverterReturnsNull_ReturnWithSuccess()
    {
        var converterMock = FakeService;
        converterMock.Setup(x =>
                x.ConvertMessageAsync(It.IsAny<MotorCloudEvent<string>>(), It.IsAny<CancellationToken>()))
            .Returns(CreateReturnValues((string)null));
        var publisherMock = FakePublisher;
        var messageHandler = GetMessageHandler(service: converterMock.Object,
            publisher: publisherMock.Object);

        var actual = await messageHandler.HandleMessageAsync(CreateMotorEvent("message_5")).ConfigureAwait(false);

        Assert.Equal(ProcessedMessageStatus.Success, actual);
        publisherMock.Verify(
            x => x.PublishMessageAsync(It.IsAny<MotorCloudEvent<string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleMessageAsync_ConverterReturnIsEmpty_ReturnWithSuccess()
    {
        var converterMock = FakeService;
        converterMock.Setup(x =>
                x.ConvertMessageAsync(It.IsAny<MotorCloudEvent<string>>(), It.IsAny<CancellationToken>()))
            .Returns(CreateReturnValues());
        var publisherMock = FakePublisher;
        var messageHandler = GetMessageHandler(service: converterMock.Object, publisher: publisherMock.Object);

        var actual = await messageHandler.HandleMessageAsync(CreateMotorEvent("message_5")).ConfigureAwait(false);

        Assert.Equal(ProcessedMessageStatus.Success, actual);
        publisherMock.Verify(
            x => x.PublishMessageAsync(It.IsAny<MotorCloudEvent<string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleMessageAsync_ConverterReturnsSomeResult_ReturnWithSuccess()
    {
        var converterMock = FakeService;
        converterMock.Setup(x =>
                x.ConvertMessageAsync(It.IsAny<MotorCloudEvent<string>>(), It.IsAny<CancellationToken>()))
            .Returns(CreateReturnValues("someResult"));
        var publisherMock = FakePublisher;
        var messageHandler = GetMessageHandler(service: converterMock.Object, publisher: publisherMock.Object);

        var actual = await messageHandler.HandleMessageAsync(CreateMotorEvent("message_6"));

        Assert.Equal(ProcessedMessageStatus.Success, actual);
    }

    [Fact]
    public async Task HandleMessageAsync_ConverterReturnsMultipleResults_PublisherIsCalledWithEachResult()
    {
        const string converterResult1 = "someOtherResult1";
        const string converterResult2 = "someOtherResult2";
        const string converterResult3 = "someOtherResult3";
        var converterMock = FakeService;
        converterMock.Setup(x =>
                x.ConvertMessageAsync(It.IsAny<MotorCloudEvent<string>>(), It.IsAny<CancellationToken>()))
            .Returns(CreateReturnValues(converterResult1, converterResult2, converterResult3));
        var publisherMock = FakePublisher;
        var messageHandler = GetMessageHandler(service: converterMock.Object, publisher: publisherMock.Object);

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
    }

    [Fact]
    public async Task HandleMessageAsync_MessageProcessingFailure_ReturnsFailure()
    {
        var converterMock = FakeService;
        converterMock.Setup(x =>
                x.ConvertMessageAsync(It.IsAny<MotorCloudEvent<string>>(), It.IsAny<CancellationToken>()))
            .Throws(new FailureException("message processing failed"));
        var messageHandler = GetMessageHandler(service: converterMock.Object);

        var actual = await messageHandler.HandleMessageAsync(CreateMotorEvent("message_7"));

        Assert.Equal(ProcessedMessageStatus.Failure, actual);
    }

    private async IAsyncEnumerable<MotorCloudEvent<string>> CreateReturnValues(params string[] data)
    {
        foreach (var t in data)
        {
            yield return await Task.FromResult(MotorCloudEvent.CreateTestCloudEvent(t, new Uri("test://non")));
        }
    }

    private MultiOutputServiceAdapter<string, string> GetMessageHandler(
        ILogger<SingleOutputServiceAdapter<string, string>>? logger = null,
        IMultiOutputService<string, string>? service = null,
        ITypedMessagePublisher<string>? publisher = null)
    {
        logger ??= Mock.Of<ILogger<SingleOutputServiceAdapter<string, string>>>();
        service ??= FakeService.Object;
        publisher ??= FakePublisher.Object;

        return new MultiOutputServiceAdapter<string, string>(logger, service, publisher);
    }

    private static MotorCloudEvent<string> CreateMotorEvent(string data = "")
    {
        return MotorCloudEvent.CreateTestCloudEvent(data, new Uri("test://non"));
    }
}
