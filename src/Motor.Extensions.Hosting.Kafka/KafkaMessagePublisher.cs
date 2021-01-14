using System;
using System.Threading;
using System.Threading.Tasks;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.Kafka;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.Kafka.Options;

namespace Motor.Extensions.Hosting.Kafka
{
    public class KafkaMessagePublisher<T> : ITypedMessagePublisher<byte[]>, IDisposable
    {
        private readonly ICloudEventFormatter _cloudEventFormatter;
        private readonly IProducer<string, byte[]> producer;
        private readonly KafkaPublisherOptions<T> _options;

        public KafkaMessagePublisher(IOptions<KafkaPublisherOptions<T>> options,
            ICloudEventFormatter cloudEventFormatter)
        {
            _cloudEventFormatter = cloudEventFormatter;
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
            producer = new ProducerBuilder<string, byte[]>(_options).Build();
        }

        public async Task PublishMessageAsync(MotorCloudEvent<byte[]> cloudEvent, CancellationToken token = default)
        {
            var topic = cloudEvent.Extension<KafkaTopicExtension>()?.Topic ?? _options.Topic;
            await producer.ProduceAsync(topic,
                new KafkaCloudEventMessage(cloudEvent, ContentMode.Binary, _cloudEventFormatter), token);
        }

        public void Dispose()
        {
            producer.Dispose();
        }
    }
}
