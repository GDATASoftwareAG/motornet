using System;
using System.Linq;
using System.Text;
using CloudNative.CloudEvents;
using Moq;
using Motor.Extensions.ContentEncoding.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using Motor.Extensions.Hosting.RabbitMQ;
using Motor.Extensions.Hosting.RabbitMQ.Options;
using Motor.Extensions.TestUtilities;
using RabbitMQ.Client;
using Xunit;

namespace Motor.Extensions.Hosting.RabbitMQ_UnitTest;

public class BasicPropertiesExtensionsTest
{
    /*
     * Serialization Tests
     */

    [Fact]
    public void Update_NoExtensions_OnlyRequiredAttributesInHeader()
    {
        var basicProperties = new BasicProperties();
        var publisherOptions = new RabbitMQPublisherOptions<byte[]>();
        var cloudEvent = MotorCloudEvent.CreateTestCloudEvent(Array.Empty<byte>());

        basicProperties.SetPriority(cloudEvent, publisherOptions);
        basicProperties.WriteCloudEventIntoHeader(cloudEvent);

        VerifyPresenceOfRequiredAttributes(basicProperties, cloudEvent);
    }

    [Fact]
    public void Update_RabbitMQPriorityExtension_OnlyRequiredAttributesInHeader()
    {
        var basicProperties = new BasicProperties();
        var publisherOptions = new RabbitMQPublisherOptions<byte[]>();
        var cloudEvent = MotorCloudEvent.CreateTestCloudEvent(Array.Empty<byte>());
        cloudEvent.SetRabbitMQPriority(123);

        basicProperties.SetPriority(cloudEvent, publisherOptions);
        basicProperties.WriteCloudEventIntoHeader(cloudEvent);

        VerifyPresenceOfRequiredAttributes(basicProperties, cloudEvent);
    }

    [Fact]
    public void Update_EncodingExtension_EncodingNotInHeaderInProperties()
    {
        var basicProperties = new BasicProperties();
        var publisherOptions = new RabbitMQPublisherOptions<byte[]>();
        var cloudEvent = MotorCloudEvent.CreateTestCloudEvent(Array.Empty<byte>());
        const string encoding = "someEncoding";
        cloudEvent.SetEncoding(encoding);

        basicProperties.SetPriority(cloudEvent, publisherOptions);
        basicProperties.WriteCloudEventIntoHeader(cloudEvent);

        Assert.Equal(encoding, basicProperties.ContentEncoding);
        VerifyPresenceOfRequiredAttributes(basicProperties, cloudEvent);
    }

    /*
     * Round Trip Tests
     */

    [Fact]
    public void UpdateAndExtractCloudEvent_NoExtensions_CloudEventWithRequiredExtensions()
    {
        var basicProperties = new BasicProperties();
        var publisherOptions = new RabbitMQPublisherOptions<byte[]>();
        var content = new byte[] { 1, 2, 3 };
        var inputCloudEvent = MotorCloudEvent.CreateTestCloudEvent(content);
        var mockedApplicationNameService = Mock.Of<IApplicationNameService>();

        basicProperties.SetPriority(inputCloudEvent, publisherOptions);
        basicProperties.WriteCloudEventIntoHeader(inputCloudEvent);
        var outputCloudEvent = basicProperties.ExtractCloudEvent(mockedApplicationNameService,
            new ReadOnlyMemory<byte>(content));

        foreach (var requiredAttribute in MotorCloudEventInfo.RequiredAttributes(CurrentMotorVersion))
        {
            Assert.Equal(inputCloudEvent[requiredAttribute], outputCloudEvent[requiredAttribute]);
        }
    }

    [Fact]
    public void UpdateAndExtractCloudEvent_NoExtensions_CloudEventWithoutSpecificExtensions()
    {
        var basicProperties = new BasicProperties();
        var publisherOptions = new RabbitMQPublisherOptions<byte[]>();
        var content = new byte[] { 1, 2, 3 };
        var inputCloudEvent = MotorCloudEvent.CreateTestCloudEvent(content);
        var mockedApplicationNameService = Mock.Of<IApplicationNameService>();

        basicProperties.SetPriority(inputCloudEvent, publisherOptions);
        basicProperties.WriteCloudEventIntoHeader(inputCloudEvent);
        var outputCloudEvent = basicProperties.ExtractCloudEvent(mockedApplicationNameService,
            new ReadOnlyMemory<byte>(content));

        var rabbitSpecificAttributes =
            RabbitMQBindingExtension.AllAttributes.Concat(RabbitMQPriorityExtension.AllAttributes);
        foreach (var rabbitSpecificAttribute in rabbitSpecificAttributes)
        {
            Assert.DoesNotContain(rabbitSpecificAttribute, outputCloudEvent.GetPopulatedAttributes().Select(a => a.Key));
        }
    }

    [Fact]
    public void UpdateAndExtractCloudEvent_RabbitMQPriorityExtension_CloudEventWithRequiredExtensions()
    {
        var basicProperties = new BasicProperties();
        var publisherOptions = new RabbitMQPublisherOptions<byte[]>();
        var content = new byte[] { 1, 2, 3 };
        var inputCloudEvent = MotorCloudEvent.CreateTestCloudEvent(content);
        const byte priority = 123;
        inputCloudEvent.SetRabbitMQPriority(priority);
        var mockedApplicationNameService = Mock.Of<IApplicationNameService>();

        basicProperties.SetPriority(inputCloudEvent, publisherOptions);
        basicProperties.WriteCloudEventIntoHeader(inputCloudEvent);
        var outputCloudEvent = basicProperties.ExtractCloudEvent(mockedApplicationNameService,
            new ReadOnlyMemory<byte>(content));

        Assert.Equal(priority, outputCloudEvent.GetRabbitMQPriority());
        foreach (var requiredAttribute in MotorCloudEventInfo.RequiredAttributes(CurrentMotorVersion))
        {
            Assert.Equal(inputCloudEvent[requiredAttribute], outputCloudEvent[requiredAttribute]);
        }
    }

    [Fact]
    public void UpdateAndExtractCloudEvent_EncodingProperty_CloudEventWithRequiredExtensionsAndEncodingExtension()
    {
        var basicProperties = new BasicProperties();
        var content = new byte[] { 1, 2, 3 };
        const string encoding = "someEncoding";
        basicProperties.ContentEncoding = encoding;
        var mockedApplicationNameService = Mock.Of<IApplicationNameService>();

        var outputCloudEvent = basicProperties.ExtractCloudEvent(mockedApplicationNameService,
            new ReadOnlyMemory<byte>(content));

        Assert.Equal(encoding, outputCloudEvent.GetEncoding());
    }

    /*
     * Version compatibility tests
     */

    [Fact]
    public void UpdateAndExtractCloudEvent_V0_6_0Header_ExtensionsAddedToCloudEvent()
    {
        var basicProperties = new BasicProperties();
        var publisherOptions = new RabbitMQPublisherOptions<byte[]>();
        var content = new byte[] { 1, 2, 3 };
        var inputCloudEvent = MotorCloudEvent.CreateTestCloudEvent(content);
        var mockedApplicationNameService = Mock.Of<IApplicationNameService>();

        basicProperties.SetPriority(inputCloudEvent, publisherOptions);
        basicProperties.WriteCloudEventIntoHeader(inputCloudEvent);
        // manipulate basic properties to simulate outdated version
        basicProperties.Headers!.Remove($"{BasicPropertiesExtensions.CloudEventPrefix}{MotorVersionExtension.MotorVersionAttribute.Name}");
        basicProperties.ContentEncoding = null;
        basicProperties.Headers.Add(
            $"{BasicPropertiesExtensions.CloudEventPrefix}{CloudEventsSpecVersion.V1_0.DataContentTypeAttribute.Name}",
            Encoding.UTF8.GetBytes($"{basicProperties.ContentType}"));
        foreach (var (key, value) in basicProperties.Headers)
        {
            if (value is byte[] byteValue)
            {
                basicProperties.Headers[key] = EscapeWithQuotes(byteValue);
            }
        }

        var outputCloudEvent = basicProperties.ExtractCloudEvent(mockedApplicationNameService,
            new ReadOnlyMemory<byte>(content));

        Assert.Equal(MotorCloudEventInfo.RequiredAttributes(Version.Parse("0.6.0.0")).Count(),
            outputCloudEvent.GetPopulatedAttributes().Count());
        foreach (var requiredAttribute in MotorCloudEventInfo.RequiredAttributes(Version.Parse("0.6.0.0")))
        {
            Assert.Equal(inputCloudEvent[requiredAttribute], outputCloudEvent[requiredAttribute]);
        }
    }

    [Fact]
    public void UpdateAndExtractCloudEvent_V0_6_0HeaderWithIncorrectVersionField_ExtensionsAddedToCloudEvent()
    {
        var basicProperties = new BasicProperties();
        var publisherOptions = new RabbitMQPublisherOptions<byte[]>();
        var content = new byte[] { 1, 2, 3 };
        var inputCloudEvent = MotorCloudEvent.CreateTestCloudEvent(content);
        var mockedApplicationNameService = Mock.Of<IApplicationNameService>();

        basicProperties.SetPriority(inputCloudEvent, publisherOptions);
        basicProperties.WriteCloudEventIntoHeader(inputCloudEvent);
        // manipulate basic properties to simulate outdated version
        basicProperties.Headers![
                $"{BasicPropertiesExtensions.CloudEventPrefix}{MotorVersionExtension.MotorVersionAttribute.Name}"] =
            Encoding.UTF8.GetBytes("\"0.7.1.0\"");
        basicProperties.ContentEncoding = null;
        basicProperties.Headers.Add(
            $"{BasicPropertiesExtensions.CloudEventPrefix}{CloudEventsSpecVersion.V1_0.DataContentTypeAttribute.Name}",
            Encoding.UTF8.GetBytes($"{basicProperties.ContentType}"));
        foreach (var (key, value) in basicProperties.Headers)
        {
            if (value is byte[] byteValue)
            {
                basicProperties.Headers[key] = EscapeWithQuotes(byteValue);
            }
        }

        var outputCloudEvent = basicProperties.ExtractCloudEvent(mockedApplicationNameService,
            new ReadOnlyMemory<byte>(content));

        // expecting all required attributes plus the (incorrect) version attribute
        Assert.Equal(MotorCloudEventInfo.RequiredAttributes(Version.Parse("0.6.0.0")).Count() + 1,
            outputCloudEvent.GetPopulatedAttributes().Count());
        foreach (var requiredAttribute in MotorCloudEventInfo.RequiredAttributes(Version.Parse("0.6.0.0")))
        {
            Assert.Equal(inputCloudEvent[requiredAttribute], outputCloudEvent[requiredAttribute]);
        }
    }

    private static Version CurrentMotorVersion => typeof(BasicPropertiesExtensionsTest).Assembly.GetName().Version;

    private static byte[] EscapeWithQuotes(byte[] value)
    {
        var stringValue = Encoding.UTF8.GetString(value);
        return Encoding.UTF8.GetBytes($"\"{stringValue}\"");
    }

    private static void VerifyPresenceOfRequiredAttributes<TData>(IBasicProperties basicProperties,
        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        MotorCloudEvent<TData> cloudEvent) where TData : class
    {
        Assert.Equal(cloudEvent.ContentType, basicProperties.ContentType);
        // ContentType is required but not saved in the header. Instead, the native
        // AMQP ContentType is used and therefore, we expect #RequiredAttributes - 1
        var requiredAttributes = MotorCloudEventInfo.RequiredAttributes(CurrentMotorVersion);
        Assert.Equal(requiredAttributes.Count() - 1, basicProperties.Headers!.Count);
    }
}
