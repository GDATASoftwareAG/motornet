namespace ConsumeAndPublishWithKafka.Model;

public record InputMessage
{
    public string FancyText { get; set; } = "FooBar";
    public int FancyNumber { get; set; } = 42;
}
