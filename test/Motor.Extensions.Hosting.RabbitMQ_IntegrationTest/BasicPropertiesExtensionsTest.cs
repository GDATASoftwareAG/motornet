using System;
using System.Linq;
using Moq;
using Motor.Extensions.Compression.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using Motor.Extensions.Hosting.RabbitMQ;
using Motor.Extensions.Hosting.RabbitMQ.Options;
using Motor.Extensions.TestUtilities;
using Xunit;

namespace Motor.Extensions.Hosting.RabbitMQ_IntegrationTest
{
    public class BasicPropertiesExtensionsTest : IClassFixture<RabbitMQFixture>
    {
        private readonly RabbitMQFixture _fixture;

        public BasicPropertiesExtensionsTest(RabbitMQFixture fixture)
        {
            _fixture = fixture;
        }

        /*
         * Serialization Tests
         */

        [Fact]
        public void Update_NoExtensions_OnlyRequiredAttributesInHeader()
        {
            var channel = _fixture.Connection.CreateModel();
            var basicProperties = channel.CreateBasicProperties();
            var publisherOptions = new RabbitMQPublisherOptions<byte[]>();
            var cloudEvent = MotorCloudEvent.CreateTestCloudEvent(Array.Empty<byte>());

            basicProperties.Update(cloudEvent, publisherOptions);

            Assert.Equal(MotorCloudEventInfo.RequiredAttributes.Count(), basicProperties.Headers.Count);
        }

        [Fact]
        public void Update_RabbitMQPriorityExtension_OnlyRequiredAttributesInHeader()
        {
            var channel = _fixture.Connection.CreateModel();
            var basicProperties = channel.CreateBasicProperties();
            var publisherOptions = new RabbitMQPublisherOptions<byte[]>();
            var cloudEvent = MotorCloudEvent.CreateTestCloudEvent(Array.Empty<byte>());
            cloudEvent.SetRabbitMQPriority(123);

            basicProperties.Update(cloudEvent, publisherOptions);

            Assert.Equal(MotorCloudEventInfo.RequiredAttributes.Count(), basicProperties.Headers.Count);
        }

        [Fact]
        public void Update_CompressionTypeExtension_RequiredAttributesAndCompressionTypeAttributeInHeader()
        {
            var channel = _fixture.Connection.CreateModel();
            var basicProperties = channel.CreateBasicProperties();
            var publisherOptions = new RabbitMQPublisherOptions<byte[]>();
            var cloudEvent = MotorCloudEvent.CreateTestCloudEvent(Array.Empty<byte>());
            cloudEvent.SetCompressionType("someCompressionType");

            basicProperties.Update(cloudEvent, publisherOptions);

            Assert.Equal(MotorCloudEventInfo.RequiredAttributes.Count() + 1, basicProperties.Headers.Count);
        }

        /*
         * Round Trip Tests
         */

        [Fact]
        public void UpdateAndExtractCloudEvent_NoExtensions_CloudEventWithRequiredExtensions()
        {
            var channel = _fixture.Connection.CreateModel();
            var basicProperties = channel.CreateBasicProperties();
            var publisherOptions = new RabbitMQPublisherOptions<byte[]>();
            var content = new byte[] { 1, 2, 3 };
            var inputCloudEvent = MotorCloudEvent.CreateTestCloudEvent(content);
            var mockedApplicationNameService = Mock.Of<IApplicationNameService>();

            basicProperties.Update(inputCloudEvent, publisherOptions);
            var outputCloudEvent = basicProperties.ExtractCloudEvent(mockedApplicationNameService,
                new ReadOnlyMemory<byte>(content));

            Assert.Equal(MotorCloudEventInfo.RequiredAttributes.Count(),
                outputCloudEvent.GetPopulatedAttributes().Count());
            foreach (var requiredAttribute in MotorCloudEventInfo.RequiredAttributes)
            {
                Assert.Equal(inputCloudEvent[requiredAttribute], outputCloudEvent[requiredAttribute]);
            }
        }

        [Fact]
        public void UpdateAndExtractCloudEvent_RabbitMQPriorityExtension_CloudEventWithRequiredExtensions()
        {
            var channel = _fixture.Connection.CreateModel();
            var basicProperties = channel.CreateBasicProperties();
            var publisherOptions = new RabbitMQPublisherOptions<byte[]>();
            var content = new byte[] { 1, 2, 3 };
            var inputCloudEvent = MotorCloudEvent.CreateTestCloudEvent(content);
            inputCloudEvent.SetRabbitMQPriority(123);
            var mockedApplicationNameService = Mock.Of<IApplicationNameService>();

            basicProperties.Update(inputCloudEvent, publisherOptions);
            var outputCloudEvent = basicProperties.ExtractCloudEvent(mockedApplicationNameService,
                new ReadOnlyMemory<byte>(content));

            Assert.Equal(MotorCloudEventInfo.RequiredAttributes.Count(),
                outputCloudEvent.GetPopulatedAttributes().Count());
            foreach (var requiredAttribute in MotorCloudEventInfo.RequiredAttributes)
            {
                Assert.Equal(inputCloudEvent[requiredAttribute], outputCloudEvent[requiredAttribute]);
            }
        }

        [Fact]
        public void UpdateAndExtractCloudEvent_CompressionTypeExtension_CloudEventWithRequiredExtensionsAndCompressionTypeExtension()
        {
            var channel = _fixture.Connection.CreateModel();
            var basicProperties = channel.CreateBasicProperties();
            var publisherOptions = new RabbitMQPublisherOptions<byte[]>();
            var content = new byte[] { 1, 2, 3 };
            var inputCloudEvent = MotorCloudEvent.CreateTestCloudEvent(content);
            inputCloudEvent.SetCompressionType("someCompressionType");
            var mockedApplicationNameService = Mock.Of<IApplicationNameService>();

            basicProperties.Update(inputCloudEvent, publisherOptions);
            var outputCloudEvent = basicProperties.ExtractCloudEvent(mockedApplicationNameService,
                new ReadOnlyMemory<byte>(content));

            Assert.Equal(MotorCloudEventInfo.RequiredAttributes.Count() + 1,
                outputCloudEvent.GetPopulatedAttributes().Count());
            Assert.Equal(inputCloudEvent[CompressionTypeExtension.CompressionTypeAttribute],
                outputCloudEvent[CompressionTypeExtension.CompressionTypeAttribute]);
            foreach (var requiredAttribute in MotorCloudEventInfo.RequiredAttributes)
            {
                Assert.Equal(inputCloudEvent[requiredAttribute], outputCloudEvent[requiredAttribute]);
            }
        }

        [Fact]
        public void UpdateAndExtractCloudEvent_LegacyCompressionTypeExtension_CloudEventWithRequiredExtensionsAndCompressionTypeExtension()
        {
            var channel = _fixture.Connection.CreateModel();
            var basicProperties = channel.CreateBasicProperties();
            var publisherOptions = new RabbitMQPublisherOptions<byte[]>();
            var content = new byte[] { 1, 2, 3 };
            var inputCloudEvent = MotorCloudEvent.CreateTestCloudEvent(content);
            inputCloudEvent.SetCompressionType("someCompressionType");
            var mockedApplicationNameService = Mock.Of<IApplicationNameService>();

            basicProperties.Update(inputCloudEvent, publisherOptions);
            basicProperties.Headers["cloudEvents:compressionType"] =
                basicProperties.Headers["cloudEvents:compressiontype"];
            basicProperties.Headers.Remove("cloudEvents:compressiontype");
            var outputCloudEvent = basicProperties.ExtractCloudEvent(mockedApplicationNameService,
                new ReadOnlyMemory<byte>(content));

            Assert.Equal(MotorCloudEventInfo.RequiredAttributes.Count() + 1,
                outputCloudEvent.GetPopulatedAttributes().Count());
            Assert.Equal(inputCloudEvent[CompressionTypeExtension.CompressionTypeAttribute],
                outputCloudEvent[CompressionTypeExtension.CompressionTypeAttribute]);
            foreach (var requiredAttribute in MotorCloudEventInfo.RequiredAttributes)
            {
                Assert.Equal(inputCloudEvent[requiredAttribute], outputCloudEvent[requiredAttribute]);
            }
        }
    }
}
