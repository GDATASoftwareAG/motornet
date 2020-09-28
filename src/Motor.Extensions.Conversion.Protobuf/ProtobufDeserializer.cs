using System;
using Google.Protobuf;
using Motor.Extensions.Conversion.Abstractions;

namespace Motor.Extensions.Conversion.Protobuf
{
    public class ProtobufDeserializer<T> : IMessageDeserializer<T> where T : IMessage<T>, new()
    {
        private readonly MessageParser<T> _messageParser = new MessageParser<T>(() => new T());

        public T Deserialize(byte[] inMessage)
        {
            if (inMessage == null || inMessage.Length == 0) throw new ArgumentNullException(nameof(inMessage));

            T deserializedMsg;
            try
            {
                deserializedMsg = _messageParser.ParseFrom(inMessage);
            }
            catch (Exception e)
            {
                throw new ArgumentException("Proto parser failed.", e);
            }

            return deserializedMsg;
        }
    }
}
