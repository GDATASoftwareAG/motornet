namespace Motor.Extensions.Hosting.SQS.Options
{
    public class SQSClientOptions
    {
        public string AwsAccessKeyId { get; set; } = "";
        public string AwsSecretAccessKey { get; set; } = "";
        public string Region { get; set; } = "";
        public string ServiceUrl { get; set; } = "";
        public string QueueUrl { get; set; } = "";
        public int WaitTimeSeconds { get; set; } = 10;
    }
}
