using System;
using System.Text.Json;
using Motor.Extensions.Conversion.Abstractions;

namespace Motor.Extensions.Conversion.SystemJson
{
    public class SystemJsonSerializer<T> : IMessageSerializer<T>
    {
        public byte[] Serialize(T message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            return JsonSerializer.SerializeToUtf8Bytes(message);
        }
    }
}
