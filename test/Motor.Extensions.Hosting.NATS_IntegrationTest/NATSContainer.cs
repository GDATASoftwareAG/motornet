using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TestContainers.Container.Abstractions;
using TestContainers.Container.Abstractions.Images;

namespace Motor.Extensions.Hosting.NATS_IntegrationTest;

public class NATSContainer : GenericContainer
{
    /// <summary>
    /// Default image name
    /// </summary>
    private static readonly string NatsDefaultImage = "nats";

    /// <summary>
    /// Default image tag
    /// </summary>
    private static readonly string NatsDefaultTag = "2.5";

    private static IImage CreateDefaultImage(IDockerClient dockerClient, ILoggerFactory loggerFactory)
    {
        return new GenericImage(dockerClient, loggerFactory) { ImageName = $"{NatsDefaultImage}:{NatsDefaultTag}" };
    }

    public const int Port = 4222;
    private readonly IDockerClient _dockerClient;
    private ContainerInspectResponse _containerInfo;

    /// <inheritdoc />
    public NATSContainer(IDockerClient dockerClient, ILoggerFactory loggerFactory)
        : base($"{NatsDefaultImage}:{NatsDefaultTag}", dockerClient, loggerFactory)
    {
        _dockerClient = dockerClient;
    }

    /// <inheritdoc />
    public NATSContainer(string dockerImageName, IDockerClient dockerClient, ILoggerFactory loggerFactory)
        : base(dockerImageName, dockerClient, loggerFactory)
    {
        _dockerClient = dockerClient;
    }

    /// <inheritdoc />
    [ActivatorUtilitiesConstructor]
    public NATSContainer(IImage dockerImage, IDockerClient dockerClient, ILoggerFactory loggerFactory)
        : base(NullImage.IsNullImage(dockerImage) ? CreateDefaultImage(dockerClient, loggerFactory) : dockerImage,
            dockerClient, loggerFactory)
    {
        _dockerClient = dockerClient;
    }

    protected override async Task ConfigureAsync()
    {
        await base.ConfigureAsync();
        ExposedPorts.Add(Port);
    }

    protected override async Task ContainerStarted()
    {
        await base.ContainerStarting();
        _containerInfo = await _dockerClient.Containers.InspectContainerAsync(ContainerId);
    }
}
