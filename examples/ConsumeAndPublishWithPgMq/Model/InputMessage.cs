namespace ConsumeAndPublishWithPgMq.Model;

public record InputMessage
{
    public string FancyText { get; set; } = "FooBar";
}
