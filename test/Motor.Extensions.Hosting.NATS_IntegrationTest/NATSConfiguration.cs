using Docker.DotNet.Models;
using DotNet.Testcontainers.Configurations;

namespace Motor.Extensions.Hosting.NATS_IntegrationTest;

public class NATSConfiguration : ContainerConfiguration
{
    public NATSConfiguration()
    {
    }

    public NATSConfiguration(IResourceConfiguration<CreateContainerParameters> resourceConfiguration)
        : base(resourceConfiguration)
    {
    }

    public NATSConfiguration(IContainerConfiguration resourceConfiguration)
        : base(resourceConfiguration)
    {
    }

    public NATSConfiguration(NATSConfiguration resourceConfiguration)
        : this(new NATSConfiguration(), resourceConfiguration)
    {
    }

    public NATSConfiguration(NATSConfiguration oldValue, NATSConfiguration newValue)
        : base(oldValue, newValue)
    {
    }
}
