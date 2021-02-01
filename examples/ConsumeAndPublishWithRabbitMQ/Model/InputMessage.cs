namespace ConsumeAndPublishWithRabbitMQ.Model
{
    public record InputMessage(string FancyText = "FooBar", int FancyNumber = 42);
}
