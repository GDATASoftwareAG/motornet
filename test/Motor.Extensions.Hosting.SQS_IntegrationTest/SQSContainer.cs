using System.Collections.Generic;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TestContainers.Container.Abstractions;
using TestContainers.Container.Abstractions.Images;

namespace Motor.Extensions.Hosting.SQS_IntegrationTest
{
    public class SQSContainer : GenericContainer
    {
        /// <summary>
        /// Default image name
        /// </summary>
        private static readonly string SQSDefaultImage = "roribio16/alpine-sqs";

        /// <summary>
        /// Default image tag
        /// </summary>
        private static readonly string SQSDefaultTag = "1.2.0";

        private static IImage CreateDefaultImage(IDockerClient dockerClient, ILoggerFactory loggerFactory)
        {
            return new GenericImage(dockerClient, loggerFactory) { ImageName = $"{SQSDefaultImage}:{SQSDefaultTag}" };
        }

        public const int SQS_PORT = 9324;
        private readonly IDockerClient _dockerClient;
        private ContainerInspectResponse _containerInfo;

        /// <inheritdoc />
        public SQSContainer(IDockerClient dockerClient, ILoggerFactory loggerFactory)
            : base($"{DefaultImage}:{DefaultTag}", dockerClient, loggerFactory)
        {
            _dockerClient = dockerClient;
        }

        /// <inheritdoc />
        public SQSContainer(string dockerImageName, IDockerClient dockerClient, ILoggerFactory loggerFactory)
            : base(dockerImageName, dockerClient, loggerFactory)
        {
            _dockerClient = dockerClient;
        }

        /// <inheritdoc />
        [ActivatorUtilitiesConstructor]
        public SQSContainer(IImage dockerImage, IDockerClient dockerClient, ILoggerFactory loggerFactory)
            : base(NullImage.IsNullImage(dockerImage) ? CreateDefaultImage(dockerClient, loggerFactory) : dockerImage,
                dockerClient, loggerFactory)
        {
            _dockerClient = dockerClient;
        }

        protected override async Task ConfigureAsync()
        {
            await base.ConfigureAsync();
            ExposedPorts.Add(SQS_PORT);
        }

        protected override async Task ContainerStarted()
        {
            await base.ContainerStarting();
            _containerInfo = await _dockerClient.Containers.InspectContainerAsync(ContainerId);
        }
    }
}
