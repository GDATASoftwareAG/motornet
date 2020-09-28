using System;
using Motor.Extensions.Conversion.Protobuf;
using Xunit;

namespace Motor.Extensions.Conversion.Protobuf_UnitTest
{
    public class ProtobufDeserializerTests
    {
        private byte[] ValidMessage => new byte[]
        {
            0x0a,
            0x03,
            0x46,
            0x6f,
            0x6f,
            0x12,
            0x03,
            0x42,
            0x61,
            0x72,
            0x18,
            0x2a
        };

        private byte[] InvalidMessage => new byte[]
        {
            0x5a,
            0x03,
            0x46,
            0x6f,
            0x6f,
            0x52,
            0x03,
            0x4
        };

        [Fact]
        public void Deserialize_ValidMessage_DeserializedMessage()
        {
            var serializer = CreateDeserializer();

            var message = serializer.Deserialize(ValidMessage);

            var expectedMessage = new InputMsg {Forename = "Foo", Surename = "Bar", Age = 42};
            Assert.Equal(expectedMessage, message);
        }

        [Fact]
        public void Deserialize_InvalidMessage_Throw()
        {
            var serializer = CreateDeserializer();

            Assert.Throws<ArgumentException>(() => serializer.Deserialize(InvalidMessage));
        }

        [Fact]
        public void Deserialize_NullMessage_Throw()
        {
            var serializer = CreateDeserializer();

            Assert.Throws<ArgumentNullException>(() => serializer.Deserialize(null));
        }

        [Fact]
        public void Deserialize_EmptyMessage_Throw()
        {
            var serializer = CreateDeserializer();

            Assert.Throws<ArgumentNullException>(() => serializer.Deserialize(new byte[0]));
        }

        private ProtobufDeserializer<InputMsg> CreateDeserializer()
        {
            return new ProtobufDeserializer<InputMsg>();
        }
    }
}
