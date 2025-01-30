using Docker.DotNet.Models;
using DotNet.Testcontainers;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;

namespace Motor.Extensions.Hosting.NATS_IntegrationTest;

public sealed class NATSBuilder(NATSConfiguration resourceConfiguration)
    : ContainerBuilder<NATSBuilder, NATSContainer, NATSConfiguration>(resourceConfiguration)
{
    public const string DefaultImage = "nats:2.9.11";
    public const int DefaultPort = 4222;

    protected override NATSConfiguration DockerResourceConfiguration { get; } = resourceConfiguration;

    public NATSBuilder() : this(new NATSConfiguration())
    {
        DockerResourceConfiguration = Init().DockerResourceConfiguration;
    }

    public override NATSContainer Build()
    {
        Validate();
        return new NATSContainer(DockerResourceConfiguration);
    }

    protected override NATSBuilder Init()
    {
        return base.Init()
            .WithImage(DefaultImage)
            .WithLogger(ConsoleLogger.Instance)
            .WithPortBinding(DefaultPort, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Server is ready"));
    }

    protected override NATSBuilder Clone(IResourceConfiguration<CreateContainerParameters> resourceConfiguration)
    {
        return Merge(DockerResourceConfiguration, new NATSConfiguration(resourceConfiguration));
    }

    protected override NATSBuilder Merge(NATSConfiguration oldValue, NATSConfiguration newValue)
    {
        return new NATSBuilder(new NATSConfiguration(oldValue, newValue));
    }

    protected override NATSBuilder Clone(IContainerConfiguration resourceConfiguration)
    {
        return Merge(DockerResourceConfiguration, new NATSConfiguration(resourceConfiguration));
    }
}
