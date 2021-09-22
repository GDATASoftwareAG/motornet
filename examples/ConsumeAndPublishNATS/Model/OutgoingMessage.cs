using System;
using System.Text.Json.Serialization;

namespace ConsumeAndPublishNATS.Model
{
    public class OutgoingMessage
    {
        [JsonPropertyName("SomeProperty")] public string SomeProperty { get; set; }
        [JsonPropertyName("IncomingTime")] public DateTime IncomingTime { get; set; }
        [JsonPropertyName("OutgoingTime")] public DateTime OutgoingTime { get; set; }
    }
}
