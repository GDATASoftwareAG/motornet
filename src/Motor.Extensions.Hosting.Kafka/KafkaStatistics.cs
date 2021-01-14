using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Motor.Extensions.Hosting.Kafka
{
    public record KafkaStatistics
    {
        [JsonPropertyName("topics")] public Dictionary<string, KafkaStatisticsTopic>? Topics { get; set; }
    }

    public record KafkaStatisticsTopic
    {
        [JsonPropertyName("partitions")] public Dictionary<string, KafkaStatisticsPartition>? Partitions { get; set; }
    }

    public record KafkaStatisticsPartition
    {
        [JsonPropertyName("consumer_lag")] public int ConsumerLag { get; set; }
    }
}
