using System.Collections.Generic;

namespace Motor.Extensions.Hosting.RabbitMQ.Config
{
    public class RabbitMQQueueConfig
    {
        public string Name { get; set; } = string.Empty;
        public bool Durable { get; set; } = true;
        public bool AutoDelete { get; set; } = false;
        public int? MaxLength { get; set; } = 1_000_000;
        public long? MaxLengthBytes { get; set; } = 200 * 1024 * 1024;
        public int? MaxPriority { get; set; } = 255;
        public int? MessageTtl { get; set; } = 86_400_000;
        public QueueMode Mode { get; set; } = QueueMode.Default;
        public RabbitMQBindingConfig[] Bindings { get; set; } = new RabbitMQBindingConfig[0];
        public IDictionary<string, object> Arguments { get; set; } = new Dictionary<string, object>();
    }
}
