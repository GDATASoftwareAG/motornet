using System;
using System.Text.Json.Serialization;

namespace ConsumeAndPublishNATS.Model;

public class OutgoingMessage
{
    public string SomeProperty { get; set; }
    public DateTimeOffset IncomingTime { get; set; }
    public DateTimeOffset OutgoingTime { get; set; }
}
