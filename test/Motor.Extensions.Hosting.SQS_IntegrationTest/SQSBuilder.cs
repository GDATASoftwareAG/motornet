using System.Collections.Generic;
using Docker.DotNet.Models;
using DotNet.Testcontainers;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;

namespace Motor.Extensions.Hosting.SQS_IntegrationTest;

public sealed class SQSBuilder(SQSConfiguration resourceConfiguration)
    : ContainerBuilder<SQSBuilder, SQSContainer, SQSConfiguration>(resourceConfiguration)
{
    public const string DefaultImage = "roribio16/alpine-sqs:1.2.0";
    public const int DefaultPort = 9324;

    protected override SQSConfiguration DockerResourceConfiguration { get; } = resourceConfiguration;

    public SQSBuilder() : this(new SQSConfiguration())
    {
        DockerResourceConfiguration = Init().DockerResourceConfiguration;
    }

    public override SQSContainer Build()
    {
        Validate();
        return new SQSContainer(DockerResourceConfiguration);
    }

    protected override SQSBuilder Init()
    {
        var ulimit = new Ulimit { Name = "nofile", Soft = 1024, Hard = 1024 };
        return base.Init()
            .WithImage(DefaultImage)
            .WithLogger(ConsoleLogger.Instance)
            .WithPortBinding(DefaultPort, true)
            .WithCreateParameterModifier(g => g.HostConfig.Ulimits = new List<Ulimit> { ulimit })
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("listening on port"));
    }

    protected override SQSBuilder Clone(IResourceConfiguration<CreateContainerParameters> resourceConfiguration)
    {
        return Merge(DockerResourceConfiguration, new SQSConfiguration(resourceConfiguration));
    }

    protected override SQSBuilder Merge(SQSConfiguration oldValue, SQSConfiguration newValue)
    {
        return new SQSBuilder(new SQSConfiguration(oldValue, newValue));
    }

    protected override SQSBuilder Clone(IContainerConfiguration resourceConfiguration)
    {
        return Merge(DockerResourceConfiguration, new SQSConfiguration(resourceConfiguration));
    }
}
