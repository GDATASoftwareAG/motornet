namespace Motor.Extensions.Hosting.SQS.Options
{
    public class SQSConsumerOptions<T>: SQSBaseOptions
    {
        public string SqsUrl { get; set; }
    }
}
