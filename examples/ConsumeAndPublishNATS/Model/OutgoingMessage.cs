using System;
using System.Text.Json.Serialization;

namespace ConsumeAndPublishNATS.Model
{
    public class OutgoingMessage
    {
        public string SomeProperty { get; set; }
        public DateTime IncomingTime { get; set; }
        public DateTime OutgoingTime { get; set; }
    }
}
