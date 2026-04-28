using Docker.DotNet.Models;
using DotNet.Testcontainers;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;

namespace Motor.Extensions.Hosting.PgMq_IntegrationTest;

public sealed class PostgresBuilder(PostgresConfiguration resourceConfiguration)
    : ContainerBuilder<PostgresBuilder, PostgresContainer, PostgresConfiguration>(resourceConfiguration)
{
    public const string DefaultImage = "quay.io/tembo/pg16-pgmq:latest";
    public const int DefaultPort = 5432;
    private const string DefaultUser = "postgres";
    private const string DefaultPassword = "postgres";
    private const string DefaultDatabase = "postgres";
    protected override PostgresConfiguration DockerResourceConfiguration { get; } = resourceConfiguration;

    public PostgresBuilder()
        : this(new PostgresConfiguration())
    {
        DockerResourceConfiguration = Init().DockerResourceConfiguration;
    }

    public override PostgresContainer Build()
    {
        Validate();
        return new PostgresContainer(DockerResourceConfiguration);
    }

    protected override PostgresBuilder Init()
    {
        return base.Init()
            .WithImage(DefaultImage)
            .WithLogger(ConsoleLogger.Instance)
            .WithPortBinding(DefaultPort, true)
            .WithEnvironment("POSTGRES_USER", DefaultUser)
            .WithEnvironment("POSTGRES_PASSWORD", DefaultPassword)
            .WithEnvironment("POSTGRES_DB", DefaultDatabase)
            .WithWaitStrategy(
                Wait.ForUnixContainer().UntilMessageIsLogged("database system is ready to accept connections")
            );
    }

    protected override PostgresBuilder Clone(IResourceConfiguration<CreateContainerParameters> resourceConfiguration)
    {
        return Merge(DockerResourceConfiguration, new PostgresConfiguration(resourceConfiguration));
    }

    protected override PostgresBuilder Merge(PostgresConfiguration oldValue, PostgresConfiguration newValue)
    {
        return new PostgresBuilder(new PostgresConfiguration(oldValue, newValue));
    }

    protected override PostgresBuilder Clone(IContainerConfiguration resourceConfiguration)
    {
        return Merge(DockerResourceConfiguration, new PostgresConfiguration(resourceConfiguration));
    }
}
