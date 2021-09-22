using System;
using System.Text.Json.Serialization;

namespace ConsumeAndPublishNATS.Model
{
    public class IncomingMessage
    {
        [JsonPropertyName("SomeProperty")] public string SomeProperty { get; set; }
        [JsonPropertyName("IncomingTime")] public DateTime IncomingTime { get; set; }
    }
}
