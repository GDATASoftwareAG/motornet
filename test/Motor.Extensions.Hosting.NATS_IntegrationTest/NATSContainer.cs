using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging;

namespace Motor.Extensions.Hosting.NATS_IntegrationTest;

public class NATSContainer(IContainerConfiguration configuration) : DockerContainer(configuration);
