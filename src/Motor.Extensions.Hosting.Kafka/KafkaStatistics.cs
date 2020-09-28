using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Motor.Extensions.Hosting.Kafka
{
    public class KafkaStatistics
    {
        [JsonPropertyName("topics")]
        public Dictionary<string, KafkaStatisticsTopic>? Topics { get; set; }
    }

    public class KafkaStatisticsTopic
    {
        [JsonPropertyName("partitions")]
        public Dictionary<string, KafkaStatisticsPartition>? Partitions { get; set; }
    }

    public class KafkaStatisticsPartition
    {
        [JsonPropertyName("consumer_lag")]
        public int ConsumerLag { get; set; }
    }
}
