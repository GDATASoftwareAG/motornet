using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;

namespace Motor.Extensions.Hosting.PgMq_IntegrationTest;

public class PostgresContainer(IContainerConfiguration configuration) : DockerContainer(configuration);
