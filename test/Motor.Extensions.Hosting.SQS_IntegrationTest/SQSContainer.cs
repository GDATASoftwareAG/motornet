using System;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging;

namespace Motor.Extensions.Hosting.SQS_IntegrationTest;

public class SQSContainer(IContainerConfiguration configuration) : DockerContainer(configuration)
{
    public string BaseSQSUrl => new UriBuilder("http", Hostname, GetMappedPublicPort(SQSBuilder.DefaultPort)).ToString();
}
