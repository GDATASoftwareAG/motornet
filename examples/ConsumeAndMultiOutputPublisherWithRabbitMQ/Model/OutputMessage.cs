namespace ConsumeAndMultiOutputPublisherWithRabbitMQ.Model;

public record OutputMessage
{
    public string NotSoFancyText { get; set; }
    public int NotSoFancyNumber { get; set; }
}
