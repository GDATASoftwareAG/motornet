using System.Collections.Generic;

namespace Motor.Extensions.Hosting.RabbitMQ.Options
{
    public record RabbitMQBindingOptions
    {
        public string RoutingKey { get; set; } = string.Empty;
        public string Exchange { get; set; } = string.Empty;
        public IDictionary<string, object> Arguments { get; set; } = new Dictionary<string, object>();
    }
}
