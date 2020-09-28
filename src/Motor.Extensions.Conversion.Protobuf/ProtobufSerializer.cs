using Google.Protobuf;
using Motor.Extensions.Conversion.Abstractions;

namespace Motor.Extensions.Conversion.Protobuf
{
    public class ProtobufSerializer<T> : IMessageSerializer<T> where T : IMessage
    {
        public byte[] Serialize(T message)
        {
            return message.ToByteArray();
        }
    }
}
