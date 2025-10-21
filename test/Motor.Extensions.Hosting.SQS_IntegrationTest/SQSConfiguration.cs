using Docker.DotNet.Models;
using DotNet.Testcontainers.Configurations;

namespace Motor.Extensions.Hosting.SQS_IntegrationTest;

public class SQSConfiguration : ContainerConfiguration
{
    public SQSConfiguration() { }

    public SQSConfiguration(IResourceConfiguration<CreateContainerParameters> resourceConfiguration)
        : base(resourceConfiguration) { }

    public SQSConfiguration(IContainerConfiguration resourceConfiguration)
        : base(resourceConfiguration) { }

    public SQSConfiguration(SQSConfiguration resourceConfiguration)
        : this(new SQSConfiguration(), resourceConfiguration) { }

    public SQSConfiguration(SQSConfiguration oldValue, SQSConfiguration newValue)
        : base(oldValue, newValue) { }
}
