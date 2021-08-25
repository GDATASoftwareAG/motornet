using System;
using System.Text.Json.Serialization;

namespace ConsumeNATS.Model
{
    public class NatsMessage
    {
        [JsonPropertyName("SomeProperty")] public string SomeProperty { get; set; }
        [JsonPropertyName("Time")] public DateTime Time { get; set; }
    }
}
