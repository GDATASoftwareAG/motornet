using System;
using System.Text.Json;
using Motor.Extensions.Conversion.Abstractions;

namespace Motor.Extensions.Conversion.SystemJson
{
    public class SystemJsonDeserializer<T> : IMessageDeserializer<T>
    {
        public T Deserialize(byte[] message)
        {
            if (message == null || message.Length == 0)
                throw new ArgumentNullException(nameof(message));
            try
            {
                return JsonSerializer.Deserialize<T>(message);
            }
            catch (JsonException e)
            {
                throw new ArgumentException("JSON invalid.", nameof(message), e);
            }
            catch (InvalidCastException e)
            {
                throw new ArgumentException("JSON contains unexpected type.", nameof(message), e);
            }
        }
    }
}
