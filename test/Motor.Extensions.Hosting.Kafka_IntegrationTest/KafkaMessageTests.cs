using System;
using System.Linq;
using CloudNative.CloudEvents.SystemTextJson;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Motor.Extensions.ContentEncoding.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using Motor.Extensions.Hosting.Kafka;
using Motor.Extensions.Hosting.Kafka.Options;
using Motor.Extensions.TestUtilities;
using Xunit;

namespace Motor.Extensions.Hosting.Kafka_IntegrationTest;

public class KafkaMessageTests : IClassFixture<KafkaFixture>
{
    private readonly KafkaFixture _fixture;
    private const string KafkaTopic = "someTopic";

    public KafkaMessageTests(KafkaFixture fixture)
    {
        _fixture = fixture;
    }

    /*
     * Round Trip Tests
     */

    [Fact]
    public void Update_NoExtensions_OnlyRequiredAttributesInHeader()
    {
        var publisher = GetKafkaPublisher<byte[]>();
        var consumer = GetKafkaConsumer<byte[]>();
        var inputCloudEvent = MotorCloudEvent.CreateTestCloudEvent(Array.Empty<byte>());

        var kafkaMessage = publisher.CloudEventToKafkaMessage(inputCloudEvent);
        var outputCloudEvent = consumer.KafkaMessageToCloudEvent(kafkaMessage);

        Assert.Equal(MotorCloudEventInfo.RequiredAttributes(CurrentMotorVersion).Count(),
            outputCloudEvent.GetPopulatedAttributes().Count());
        foreach (var requiredAttribute in MotorCloudEventInfo.RequiredAttributes(CurrentMotorVersion))
        {
            Assert.Equal(inputCloudEvent[requiredAttribute], outputCloudEvent[requiredAttribute]);
        }
    }

    [Fact]
    public void Update_EncodingExtension_RequiredAttributesAndEncodingAttributeInHeader()
    {
        var publisher = GetKafkaPublisher<byte[]>();
        var consumer = GetKafkaConsumer<byte[]>();
        var inputCloudEvent = MotorCloudEvent.CreateTestCloudEvent(Array.Empty<byte>());
        inputCloudEvent.SetEncoding("someEncoding");

        var kafkaMessage = publisher.CloudEventToKafkaMessage(inputCloudEvent);
        var outputCloudEvent = consumer.KafkaMessageToCloudEvent(kafkaMessage);

        Assert.Equal(MotorCloudEventInfo.RequiredAttributes(CurrentMotorVersion).Count() + 1,
            outputCloudEvent.GetPopulatedAttributes().Count());
        Assert.Equal(inputCloudEvent[EncodingExtension.EncodingAttribute],
            outputCloudEvent[EncodingExtension.EncodingAttribute]);
        foreach (var requiredAttribute in MotorCloudEventInfo.RequiredAttributes(CurrentMotorVersion))
        {
            Assert.Equal(inputCloudEvent[requiredAttribute], outputCloudEvent[requiredAttribute]);
        }
    }

    /*
     * Helper Methods
     */

    private static Version CurrentMotorVersion => typeof(KafkaMessageTests).Assembly.GetName().Version;

    private static KafkaMessagePublisher<TData> GetKafkaPublisher<TData>()
    {
        var options = new KafkaPublisherOptions<TData>
        {
            Topic = KafkaTopic
        };
        return new KafkaMessagePublisher<TData>(Options.Create(options), new JsonEventFormatter());
    }


    private KafkaMessageConsumer<T> GetKafkaConsumer<T>()
    {
        var options = new OptionsWrapper<KafkaConsumerOptions<T>>(GetConsumerConfig<T>(KafkaTopic));
        var fakeLoggerMock = Mock.Of<ILogger<KafkaMessageConsumer<T>>>();
        return new KafkaMessageConsumer<T>(fakeLoggerMock, options, null, GetApplicationNameService(),
            new JsonEventFormatter());
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
            BootstrapServers = $"{_fixture.Hostname}:{_fixture.Port}",
            EnableAutoCommit = false,
            StatisticsIntervalMs = 5000,
            SessionTimeoutMs = 6000,
            AutoOffsetReset = AutoOffsetReset.Earliest
        };
    }
}
