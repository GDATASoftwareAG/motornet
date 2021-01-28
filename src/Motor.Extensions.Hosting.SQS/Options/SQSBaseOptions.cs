namespace Motor.Extensions.Hosting.SQS.Options
{
    public class SQSBaseOptions
    {
        public string AwsAccessKeyId {get; set; } 
        public string AwsSecretAccessKey {get; set; }
        public string Region { get; set; }
    }
}
