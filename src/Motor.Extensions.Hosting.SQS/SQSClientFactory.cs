using Amazon;
using Amazon.SQS;
using Motor.Extensions.Hosting.SQS.Options;

namespace Motor.Extensions.Hosting.SQS;

public interface ISQSClientFactory
{
    IAmazonSQS From(SQSClientOptions clientOptions);
}

public class SQSClientFactory : ISQSClientFactory
{
    public IAmazonSQS From(SQSClientOptions clientOptions)
    {
        var amazonSqsConfig = new AmazonSQSConfig();
        if (!string.IsNullOrWhiteSpace(clientOptions.ServiceUrl))
        {
            amazonSqsConfig.ServiceURL = clientOptions.ServiceUrl;
        }

        if (!string.IsNullOrWhiteSpace(clientOptions.Region))
        {
            amazonSqsConfig.RegionEndpoint = RegionEndpoint.GetBySystemName(clientOptions.Region);
        }

        return new AmazonSQSClient(clientOptions.AwsAccessKeyId, clientOptions.AwsSecretAccessKey, amazonSqsConfig);
    }
}
