using System;
using System.Text;
using Motor.Extensions.Conversion.Abstractions;
using Newtonsoft.Json;

namespace Motor.Extensions.Conversion.JsonNet
{
    public class JsonNetDeserializer<T> : IMessageDeserializer<T> where T : notnull
    {
        public T Deserialize(byte[] message)
        {
            if (message == null || message.Length == 0)
                throw new ArgumentNullException(nameof(message));

            var json = Encoding.UTF8.GetString(message);
            try
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (JsonReaderException e)
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
