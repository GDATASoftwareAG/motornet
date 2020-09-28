using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTracing.Propagation;

namespace Motor.Extensions.Hosting.RabbitMQ
{
    public class RabbitMQHeadersMap : ITextMap
    {
        private readonly IDictionary<string, object> _basicPropertiesHeaders;

        public RabbitMQHeadersMap(IDictionary<string, object> basicPropertiesHeaders)
        {
            _basicPropertiesHeaders = basicPropertiesHeaders;
        }

        public static string Prefix { get; } = "x-open-tracing";

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return _basicPropertiesHeaders
                .Where(t => t.Key.StartsWith(Prefix))
                .Select(t =>
                {
                    var valueFromHeader = (byte[]) t.Value;
                    var message = Encoding.UTF8.GetString(valueFromHeader, 0, valueFromHeader.Length);
                    return new KeyValuePair<string, string>(t.Key.Substring($"{Prefix}-".Length), message);
                }).GetEnumerator();
        }

        public void Set(string key, string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            _basicPropertiesHeaders.Add($"{Prefix}-{key}", bytes);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
