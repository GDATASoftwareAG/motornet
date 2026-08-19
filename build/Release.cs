using System;
using System.Linq;
using Cake.Common.Build.GitHubActions;
using Cake.Common.Build.GitHubActions.Data;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.NuGet.Push;
using Cake.Docker;
using Cake.Frosting;

namespace build;

[TaskName("NugetPush")]
[IsDependentOn(typeof(PackTask))]
public sealed class NugetPushTask : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context) =>
        context.GitHubContext.IsRunningOnGitHubActions
        && context.GitHubContext.Environment.Workflow.EventName == "release";

    public override void Run(BuildContext context)
    {
        ArgumentException.ThrowIfNullOrEmpty(context.NuGetApiKey);

        context.DotNetNuGetPush(
            $"{context.ArtifactsDirectory}/*.nupkg",
            new DotNetNuGetPushSettings
            {
                SkipDuplicate = true,
                Source = context.NuGetFeed,
                ApiKey = context.NuGetApiKey,
            }
        );
    }
}

[TaskName("BridgeContainerImage")]
[IsDependentOn(typeof(PublishTask))]
public sealed class BridgeContainerImageTask : FrostingTask<BuildContext>
{
    private const string ContainerImageName = "ghcr.io/gdatasoftwareag/motornet/bridge";

    public override void Run(BuildContext context)
    {
        var isRunningOnGitHubActions = context.GitHubContext.IsRunningOnGitHubActions;
        var imageTags = isRunningOnGitHubActions
            ? new[] { "", context.GitHubContext.Environment.Workflow.Sha }
                .Select(suffix =>
                    $"{ContainerImageName}:{DetermineImageTag(context.GitHubContext)}-{suffix}".TrimEnd('-')
                )
                .ToArray()
            : [$"{ContainerImageName}:dev"];

        var revision = isRunningOnGitHubActions ? context.GitHubContext.Environment.Workflow.Sha : "dev";

        context.DockerBuildXBuild(
            new DockerBuildXBuildSettings
            {
                Tag = imageTags,
                Label =
                [
                    $"org.opencontainers.image.revision={revision}",
                    $"org.opencontainers.image.created={DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}",
                ],
                Platform = ["linux/amd64"],
                Push = isRunningOnGitHubActions && context.GitHubContext.Environment.Workflow.EventName == "release",
            },
            context.BridgeArtifactsDirectory
        );
    }

    private static string DetermineImageTag(IGitHubActionsProvider gh) =>
        (gh.Environment.Workflow.RefType, gh.Environment.PullRequest.IsPullRequest) switch
        {
            (GitHubActionsRefType.Tag, _) => gh.Environment.Workflow.RefName,
            (GitHubActionsRefType.Branch, false) => new string(
                gh.Environment.Workflow.RefName.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray()
            ),
            (GitHubActionsRefType.Branch, true) => $"pr-{Environment.GetEnvironmentVariable("GITHUB_EVENT_NUMBER")}",
            _ => "edge",
        };
}
