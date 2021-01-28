using Amazon.SQS;
using Motor.Extensions.Hosting.SQS.Options;

namespace Motor.Extensions.Hosting.SQS
{
    public interface ISQSClientFactory
    {
        IAmazonSQS From<T>(SQSConsumerOptions<T> options);
    }

    public class SQSClientFactory : ISQSClientFactory
    {
        public IAmazonSQS From<T>(SQSConsumerOptions<T> options)
        {
            return new AmazonSQSClient(options.AwsAccessKeyId, options.AwsSecretAccessKey,
                options.Region);
        }
    }
}
