using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging;

namespace Motor.Extensions.Hosting.NATS_IntegrationTest;

public class NATSContainer : DockerContainer
{
    public NATSContainer(IContainerConfiguration configuration, ILogger logger) : base(configuration, logger)
    {
    }
}
