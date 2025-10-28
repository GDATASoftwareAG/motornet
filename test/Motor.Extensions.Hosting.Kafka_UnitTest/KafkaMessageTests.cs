using System;
using System.Linq;
using CloudNative.CloudEvents.SystemTextJson;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Motor.Extensions.ContentEncoding.Abstractions;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using Motor.Extensions.Hosting.Kafka;
using Motor.Extensions.Hosting.Kafka.Options;
using Motor.Extensions.TestUtilities;
using Xunit;

namespace Motor.Extensions.Hosting.Kafka_UnitTest;

public class KafkaMessageTests
{
    private const string KafkaTopic = "someTopic";

    /*
     * Round Trip Tests
     */

    [Fact]
    public void UseProtocolFormat_NoExtensions_OnlyRequiredAttributesInHeader()
    {
        var publisher = GetKafkaPublisher<byte[]>();
        var consumer = GetKafkaConsumer<byte[]>();
        var inputCloudEvent = MotorCloudEvent.CreateTestCloudEvent(Array.Empty<byte>());

        var kafkaMessage = publisher.CloudEventToKafkaMessage(inputCloudEvent);
        var outputCloudEvent = consumer.KafkaMessageToCloudEvent(kafkaMessage);

        Assert.Equal(
            MotorCloudEventInfo.RequiredAttributes(CurrentMotorVersion).Count(),
            outputCloudEvent.GetPopulatedAttributes().Count()
        );
        foreach (var requiredAttribute in MotorCloudEventInfo.RequiredAttributes(CurrentMotorVersion))
        {
            Assert.Equal(inputCloudEvent[requiredAttribute], outputCloudEvent[requiredAttribute]);
        }
    }

    [Fact]
    public void UseProtocolFormat_EncodingExtension_RequiredAttributesAndEncodingAttributeInHeader()
    {
        var publisher = GetKafkaPublisher<byte[]>();
        var consumer = GetKafkaConsumer<byte[]>();
        var inputCloudEvent = MotorCloudEvent.CreateTestCloudEvent(Array.Empty<byte>());
        inputCloudEvent.SetEncoding("someEncoding");

        var kafkaMessage = publisher.CloudEventToKafkaMessage(inputCloudEvent);
        var outputCloudEvent = consumer.KafkaMessageToCloudEvent(kafkaMessage);

        Assert.Equal(
            MotorCloudEventInfo.RequiredAttributes(CurrentMotorVersion).Count() + 1,
            outputCloudEvent.GetPopulatedAttributes().Count()
        );
        Assert.Equal(
            inputCloudEvent[EncodingExtension.EncodingAttribute],
            outputCloudEvent[EncodingExtension.EncodingAttribute]
        );
        foreach (var requiredAttribute in MotorCloudEventInfo.RequiredAttributes(CurrentMotorVersion))
        {
            Assert.Equal(inputCloudEvent[requiredAttribute], outputCloudEvent[requiredAttribute]);
        }
    }

    [Fact]
    public void CloudEventToKafkaMessage_CloudEventFormatJson_VerifyIdIsCorrectlyStored()
    {
        var publisher = GetKafkaPublisher<byte[]>(CloudEventFormat.Json);
        var inputCloudEvent = MotorCloudEvent.CreateTestCloudEvent(Array.Empty<byte>());
        var cloudFormatter = new JsonEventFormatter();

        var kafkaMessage = publisher.CloudEventToKafkaMessage(inputCloudEvent);

        var cloudEvent = cloudFormatter.DecodeStructuredModeMessage(kafkaMessage.Value, null, null);
        Assert.Equal(inputCloudEvent.Id, cloudEvent.Id);
    }

    /*
     * Helper Methods
     */

    private static Version CurrentMotorVersion => typeof(KafkaMessageTests).Assembly.GetName().Version;

    private static KafkaMessagePublisher<TData> GetKafkaPublisher<TData>(
        CloudEventFormat format = CloudEventFormat.Protocol
    )
    {
        var options = new KafkaPublisherOptions<TData> { Topic = KafkaTopic };
        return new KafkaMessagePublisher<TData>(
            Options.Create(options),
            new JsonEventFormatter(),
            Options.Create(new PublisherOptions { CloudEventFormat = format })
        );
    }

    private KafkaMessageConsumer<T> GetKafkaConsumer<T>()
    {
        var options = Options.Create(GetConsumerConfig<T>(KafkaTopic));
        var fakeLoggerMock = Mock.Of<ILogger<KafkaMessageConsumer<T>>>();
        var fakeLifetimeMock = Mock.Of<IHostApplicationLifetime>();
        return new KafkaMessageConsumer<T>(
            fakeLoggerMock,
            options,
            fakeLifetimeMock,
            null,
            GetApplicationNameService(),
            new JsonEventFormatter()
        );
    }

    private static IApplicationNameService GetApplicationNameService(string source = "test://non")
    {
        var mock = new Mock<IApplicationNameService>();
        mock.Setup(t => t.GetSource()).Returns(new Uri(source));
        return mock.Object;
    }

    private KafkaConsumerOptions<T> GetConsumerConfig<T>(string topic, string groupId = "group_id")
    {
        return new()
        {
            Topic = topic,
            GroupId = groupId,
            CommitPeriod = 1,
            BootstrapServers = "willNotBeUsed:3000",
            EnableAutoCommit = false,
            StatisticsIntervalMs = 5000,
            SessionTimeoutMs = 6000,
            AutoOffsetReset = AutoOffsetReset.Earliest,
        };
    }
}
