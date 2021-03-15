using System;
using Motor.Extensions.Conversion.Abstractions;

namespace Motor.Extensions.Hosting.Shifter.Internals
{
    public class ByteDataConversions : IMessageDeserializer<ByteData>, IMessageSerializer<ByteData>
    {
        public ByteData Deserialize(byte[] message)
        {
            if (message == null || message.Length == 0)
                throw new ArgumentNullException(nameof(message));
            return new ByteData(message);
        }

        public byte[] Serialize(ByteData message)
        {
            return message.data;
        }
    }
}
