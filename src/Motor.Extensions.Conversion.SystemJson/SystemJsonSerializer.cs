using System;
using System.Text.Json;
using Motor.Extensions.Conversion.Abstractions;

namespace Motor.Extensions.Conversion.SystemJson;

public class SystemJsonSerializer<T> : IMessageSerializer<T> where T : notnull
{
    public byte[] Serialize(T message)
    {
        if (Equals(message, default(T)))
        {
            throw new ArgumentNullException(nameof(message));
        }

        return JsonSerializer.SerializeToUtf8Bytes(message);
    }
}
