using System;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging;

namespace Motor.Extensions.Hosting.SQS_IntegrationTest;

public class SQSContainer : DockerContainer
{

    public SQSContainer(IContainerConfiguration configuration, ILogger logger) : base(configuration, logger)
    {
    }

    public string BaseSQSUrl => new UriBuilder("http", Hostname, GetMappedPublicPort(SQSBuilder.DefaultPort)).ToString();
}
