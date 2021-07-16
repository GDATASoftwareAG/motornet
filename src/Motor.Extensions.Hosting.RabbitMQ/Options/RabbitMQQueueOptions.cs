using System.Collections.Generic;
using Motor.Extensions.Hosting.RabbitMQ.Validation;

namespace Motor.Extensions.Hosting.RabbitMQ.Options
{
    public record RabbitMQQueueOptions
    {
        [NotWhitespaceOrEmpty]
        public string Name { get; set; } = string.Empty;

        [RequireValid]
        public RabbitMQBindingOptions[] Bindings { get; set; } = new RabbitMQBindingOptions[0];

        public bool Durable { get; set; } = true;
        public bool AutoDelete { get; set; } = false;
        public int? MaxLength { get; set; } = 1_000_000;
        public long? MaxLengthBytes { get; set; } = 200 * 1024 * 1024;
        public int? MaxPriority { get; set; } = 255;
        public int? MessageTtl { get; set; } = 86_400_000;
        public QueueMode Mode { get; set; } = QueueMode.Default;
        public IDictionary<string, object> Arguments { get; set; } = new Dictionary<string, object>();
    }
}
