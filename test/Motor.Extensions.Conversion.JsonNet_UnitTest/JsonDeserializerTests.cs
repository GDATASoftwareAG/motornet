using System;
using System.Linq;
using System.Text;
using Motor.Extensions.Conversion.JsonNet;
using Newtonsoft.Json;
using Xunit;

namespace Motor.Extensions.Conversion.JsonNet_UnitTest
{
    public class JsonDeserializerTests
    {
        [Fact]
        public void Deserialize_ValidMessage_DeserializedMessage()
        {
            var serializer = CreateDeserializer();
            var bytes = Serialize(ValidMessage);

            var message = serializer.Deserialize(bytes);

            var expectedMessage = new InputMessage {Firstname = "Foo", Lastname = "Bar", Age = 42};;
            Assert.Equal(expectedMessage, message);
        }
        
        private InputMessage ValidMessage => new InputMessage {Firstname = "Foo", Lastname = "Bar", Age = 42};
        
        private byte[] Serialize(InputMessage message)
        {
            var serializedHostname = JsonConvert.SerializeObject(message);
            return Encoding.UTF8.GetBytes(serializedHostname);
        }

        [Fact]
        public void Deserialize_InvalidMessage_Throw()
        {
            var serializer = CreateDeserializer();
            var bytes = Serialize(ValidMessage).Take(10).ToArray();

            Assert.Throws<ArgumentException>(() => serializer.Deserialize(bytes));
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

        private JsonNetDeserializer<InputMessage> CreateDeserializer()
        {
            return new JsonNetDeserializer<InputMessage>();
        }
    }
}
