using System;
using System.Text;
using Motor.Extensions.Conversion.Abstractions;
using Newtonsoft.Json;

namespace Motor.Extensions.Conversion.JsonNet
{
    public class JsonNetSerializer<T> : IMessageSerializer<T>
    {
        public byte[] Serialize(T message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
        }
    }
}
