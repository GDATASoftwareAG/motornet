using System;
using System.Collections.Generic;

namespace Motor.Extensions.Hosting.RabbitMQ.Config
{
    public class RabbitMQBindingConfig
    {
        public string RoutingKey { get; set; } = String.Empty;
        public string Exchange { get; set; } = String.Empty;
        public IDictionary<string, object> Arguments { get; set; } = new Dictionary<string, object>();
    }
}