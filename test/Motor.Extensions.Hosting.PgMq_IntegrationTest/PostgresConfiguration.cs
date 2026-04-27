using Docker.DotNet.Models;
using DotNet.Testcontainers.Configurations;
namespace Motor.Extensions.Hosting.PgMq_IntegrationTest;
public class PostgresConfiguration : ContainerConfiguration
{
    public PostgresConfiguration() { }
    public PostgresConfiguration(IResourceConfiguration<CreateContainerParameters> resourceConfiguration)
        : base(resourceConfiguration) { }
    public PostgresConfiguration(IContainerConfiguration resourceConfiguration)
        : base(resourceConfiguration) { }
    public PostgresConfiguration(PostgresConfiguration resourceConfiguration)
        : this(new PostgresConfiguration(), resourceConfiguration) { }
    public PostgresConfiguration(PostgresConfiguration oldValue, PostgresConfiguration newValue)
        : base(oldValue, newValue) { }
}
