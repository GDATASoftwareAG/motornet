using System;
using Motor.Extensions.Conversion.Protobuf;
using Xunit;

namespace Motor.Extensions.Conversion.Protobuf_UnitTest
{
    public class ProtobufSerializerTests
    {
        private InputMsg ValidMessage => new InputMsg {Forename = "Foo", Surename = "Bar", Age = 42};

        private byte[] ValidSerializedMessage => new byte[]
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

        [Fact]
        public void Serialize_ValidMessage_SerializedMessage()
        {
            var serializer = CreateSerializer();

            var message = serializer.Serialize(ValidMessage);

            Assert.Equal(ValidSerializedMessage, message);
        }

        [Fact]
        public void Serialize_NullMessage_Throw()
        {
            var serializer = CreateSerializer();

            Assert.Throws<ArgumentNullException>(() => serializer.Serialize(null));
        }

        private ProtobufSerializer<InputMsg> CreateSerializer()
        {
            return new ProtobufSerializer<InputMsg>();
        }
    }
}
