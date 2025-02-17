using System;
using System.Globalization;
using System.Text;
using CloudNative.CloudEvents.SystemTextJson;
using Confluent.Kafka;
using Moq;
using Motor.Extensions.Hosting.CloudEvents;
using Motor.Extensions.Hosting.Kafka;
using Xunit;

namespace Motor.Extensions.Hosting.Kafka_UnitTest;

public class KafkaClientExtensionsRegressionTests
{
    [Fact]
    public void ToMotorCloudEvent_ContentTypeIsCloudEventWithJson_DecodedDataRemainsByteArray()
    {
        var applicationNameServiceMock = Mock.Of<IApplicationNameService>();
        var actualContent = "{\"key\":\"value\"}";
        var message = new Message<string, byte[]>
        {
            Headers = new Headers
            {
                { "content-type", "application/cloudevents+json"u8.ToArray() }
            },
            Value = Encoding.UTF8.GetBytes($@"
            {{
                ""specversion"": ""1.0"",
                ""type"": ""imaginary.type"",
                ""source"": ""/imaginary-source"",
                ""id"": ""{Guid.NewGuid()}"",
                ""time"": ""{DateTimeOffset.UnixEpoch.ToString("o", CultureInfo.InvariantCulture)}"",
                ""datacontenttype"": ""application/json"",
                ""data"": {actualContent}
            }}")
        };
        var cloudEvent = message.ToMotorCloudEvent(applicationNameServiceMock, new JsonEventFormatter());
        Assert.Equal(Encoding.UTF8.GetBytes(actualContent), cloudEvent.Data);
    }
}
