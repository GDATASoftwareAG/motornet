using System;

namespace ConsumeAndPublishNATS.Model;

public class IncomingMessage
{
    public string SomeProperty { get; set; }
    public DateTimeOffset IncomingTime { get; set; }
}
