using System.Collections.Generic;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TestContainers.Container.Abstractions;
using TestContainers.Container.Abstractions.Images;

namespace Motor.Extensions.Hosting.Kafka_IntegrationTest;

public class KafkaContainer : GenericContainer
{
    private static readonly string STARTER_SCRIPT = "/testcontainers_start.sh";

    /// <summary>
    /// Default image name
    /// </summary>
    private static readonly string KafkaDefaultImage = "confluentinc/cp-kafka";

    /// <summary>
    /// Default image tag
    /// </summary>
    private static readonly string KafkaDefaultTag = "5.4.3";

    private static IImage CreateDefaultImage(IDockerClient dockerClient, ILoggerFactory loggerFactory)
    {
        return new GenericImage(dockerClient, loggerFactory) { ImageName = $"{KafkaDefaultImage}:{KafkaDefaultTag}" };
    }

    public const int KAFKA_PORT = 9093;
    private const int ZOOKEEPER_PORT = 2181;
    private readonly IDockerClient _dockerClient;
    private ContainerInspectResponse _containerInfo;

    /// <inheritdoc />
    public KafkaContainer(IDockerClient dockerClient, ILoggerFactory loggerFactory)
        : base($"{DefaultImage}:{DefaultTag}", dockerClient, loggerFactory)
    {
        _dockerClient = dockerClient;
    }

    /// <inheritdoc />
    public KafkaContainer(string dockerImageName, IDockerClient dockerClient, ILoggerFactory loggerFactory)
        : base(dockerImageName, dockerClient, loggerFactory)
    {
        _dockerClient = dockerClient;
    }

    /// <inheritdoc />
    [ActivatorUtilitiesConstructor]
    public KafkaContainer(IImage dockerImage, IDockerClient dockerClient, ILoggerFactory loggerFactory)
        : base(NullImage.IsNullImage(dockerImage) ? CreateDefaultImage(dockerClient, loggerFactory) : dockerImage,
            dockerClient, loggerFactory)
    {
        _dockerClient = dockerClient;
    }

    protected override async Task ConfigureAsync()
    {
        await base.ConfigureAsync();
        ExposedPorts.Add(KAFKA_PORT);
        ExposedPorts.Add(ZOOKEEPER_PORT);

        // Use two listeners with different names, it will force Kafka to communicate with itself via internal
        // listener when KAFKA_INTER_BROKER_LISTENER_NAME is set, otherwise Kafka will try to use the advertised listener
        Env["KAFKA_LISTENERS"] = "PLAINTEXT://0.0.0.0:" + KAFKA_PORT + ",BROKER://0.0.0.0:9092";
        Env["KAFKA_LISTENER_SECURITY_PROTOCOL_MAP"] = "BROKER:PLAINTEXT,PLAINTEXT:PLAINTEXT";
        Env["KAFKA_INTER_BROKER_LISTENER_NAME"] = "BROKER";

        Env["KAFKA_BROKER_ID"] = "1";
        Env["KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR"] = "1";
        Env["KAFKA_OFFSETS_TOPIC_NUM_PARTITIONS"] = "1";
        Env["KAFKA_LOG_FLUSH_INTERVAL_MESSAGES"] = long.MaxValue + "";
        Env["KAFKA_GROUP_INITIAL_REBALANCE_DELAY_MS"] = "0";
        Command = new List<string>
                {"sh", "-c", "while [ ! -f " + STARTER_SCRIPT + " ]; do sleep 0.1; done; " + STARTER_SCRIPT};
    }

    protected override async Task ContainerStarted()
    {
        await base.ContainerStarting();
        _containerInfo = await _dockerClient.Containers.InspectContainerAsync(ContainerId);

        await ExecuteCommand("sh", "-c",
            $"echo \"{StartupScript()}\" > {STARTER_SCRIPT} && chmod +x {STARTER_SCRIPT}");
    }

    private string getBootstrapServers()
    {
        return
            $"PLAINTEXT://{GetDockerHostIpAddress()}:{GetMappedPort(KAFKA_PORT)},BROKER://{GetDockerHostIpAddress()}:9092";
    }

    private string StartupScript()
    {
        var command = "#!/bin/bash\n";
        var zookeeperConnect = $"localhost:{ZOOKEEPER_PORT}";
        command += $"echo 'clientPort={ZOOKEEPER_PORT}' > zookeeper.properties\n";
        command += "echo 'dataDir=/var/lib/zookeeper/data' >> zookeeper.properties\n";
        command += "echo 'dataLogDir=/var/lib/zookeeper/log' >> zookeeper.properties\n";
        command += "zookeeper-server-start zookeeper.properties &\n";

        command += $"export KAFKA_ZOOKEEPER_CONNECT='{zookeeperConnect}'\n";

        command += $"export KAFKA_ADVERTISED_LISTENERS='{getBootstrapServers()}'\n";

        command += ". /etc/confluent/docker/bash-config \n";
        command += "/etc/confluent/docker/configure \n";
        command += "/etc/confluent/docker/launch \n";
        return command;
    }
}
