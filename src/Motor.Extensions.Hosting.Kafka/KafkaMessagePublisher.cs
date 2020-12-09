using System;
using System.Threading;
using System.Threading.Tasks;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.Kafka;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Motor.Extensions.Hosting.Abstractions;

namespace Motor.Extensions.Hosting.Kafka
{
    public class KafkaMessagePublisher<T> : ITypedMessagePublisher<byte[]>, IDisposable
    {
        private readonly ICloudEventFormatter _cloudEventFormatter;
        private readonly IProducer<string, byte[]> producer;
        private readonly KafkaPublisherConfig<T> config;

        public KafkaMessagePublisher(IOptions<KafkaPublisherConfig<T>> options,
            ICloudEventFormatter cloudEventFormatter)
        {
            _cloudEventFormatter = cloudEventFormatter;
            config = options.Value ?? throw new ArgumentNullException(nameof(options));
            producer = new ProducerBuilder<string, byte[]>(config).Build();
        }

        public async Task PublishMessageAsync(MotorCloudEvent<byte[]> cloudEvent, CancellationToken token = default)
        {
            var topic = cloudEvent.Extension<KafkaTopicExtension>()?.Topic ?? config.Topic;
            await producer.ProduceAsync(topic,
                new KafkaCloudEventMessage(cloudEvent, ContentMode.Binary, _cloudEventFormatter), token);
        }

        public void Dispose()
        {
            producer.Dispose();
        }
    }
}
