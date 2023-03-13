using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;

namespace Motor.Extensions.Hosting.NATS_IntegrationTest;

public sealed class NATSBuilder : ContainerBuilder<NATSBuilder, NATSContainer, NATSConfiguration>
{
    public const string DefaultImage = "nats:2.9.11";
    public const int DefaultPort = 4222;

    protected override NATSConfiguration DockerResourceConfiguration { get; }

    public NATSBuilder() : this(new NATSConfiguration())
    {
        DockerResourceConfiguration = Init().DockerResourceConfiguration;
    }

    public NATSBuilder(NATSConfiguration resourceConfiguration) : base(resourceConfiguration)
    {
        DockerResourceConfiguration = resourceConfiguration;
    }

    public override NATSContainer Build()
    {
        Validate();
        return new NATSContainer(DockerResourceConfiguration, TestcontainersSettings.Logger);
    }

    protected override NATSBuilder Init()
    {
        return base.Init()
            .WithImage(DefaultImage)
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
