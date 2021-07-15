using System;
using System.Threading;
using System.Threading.Tasks;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.Kafka;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using Motor.Extensions.Hosting.Kafka.Options;

namespace Motor.Extensions.Hosting.Kafka
{
    public class KafkaMessagePublisher<T> : ITypedMessagePublisher<byte[]>, IDisposable
    {
        private readonly CloudEventFormatter _cloudEventFormatter;
        private readonly IProducer<string?, byte[]> _producer;
        private readonly KafkaPublisherOptions<T> _options;

        public KafkaMessagePublisher(IOptions<KafkaPublisherOptions<T>> options,
            CloudEventFormatter cloudEventFormatter)
        {
            _cloudEventFormatter = cloudEventFormatter;
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
            _producer = new ProducerBuilder<string?, byte[]>(_options).Build();
        }

        public async Task PublishMessageAsync(MotorCloudEvent<byte[]> motorCloudEvent, CancellationToken token = default)
        {
            var topic = motorCloudEvent.GetKafkaTopic() ?? _options.Topic;
            await _producer.ProduceAsync(topic, CloudEventToKafkaMessage(motorCloudEvent), token);
        }

        public Message<string?, byte[]> CloudEventToKafkaMessage<TData>(MotorCloudEvent<TData> motorCloudEvent) where TData : class =>
            motorCloudEvent.ConvertToCloudEvent().ToKafkaMessage(ContentMode.Binary, _cloudEventFormatter);

        public void Dispose()
        {
            _producer.Dispose();
        }
    }
}
