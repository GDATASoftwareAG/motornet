using System.IO;
using System.Linq;
using Cake.Common.Diagnostics;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Pack;
using Cake.Common.Tools.DotNet.Publish;
using Cake.Frosting;

namespace build;

[TaskName("Pack")]
[IsDependentOn(typeof(BuildTask))]
public sealed class PackTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context) =>
        context.DotNetPack(
            context.SolutionFilePath,
            new DotNetPackSettings
            {
                Configuration = context.BuildConfiguration,
                OutputDirectory = context.ArtifactsDirectory,
                NoBuild = true,
                IncludeSource = true,
                IncludeSymbols = true,
                Verbosity = DotNetVerbosity.Minimal,
            }
        );
}

[TaskName("Publish")]
[IsDependentOn(typeof(BuildTask))]
public sealed class PublishTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var newestVersion = context.DotNetVersions.Max();
        context.Information($"Publishing bridge for {newestVersion}");
        context.DotNetPublish(
            Path.Combine(
                context.SolutionDirectory,
                "src",
                "Motor.Extensions.Hosting.Bridge",
                "Motor.Extensions.Hosting.Bridge.csproj"
            ),
            new DotNetPublishSettings
            {
                Configuration = context.BuildConfiguration,
                Framework = newestVersion,
                OutputDirectory = context.BridgeArtifactsDirectory,
                NoBuild = true,
                Verbosity = DotNetVerbosity.Minimal,
            }
        );
    }
}

[TaskName("Artifacts")]
[IsDependentOn(typeof(PackTask))]
[IsDependentOn(typeof(PublishTask))]
public sealed class ArtifactsTask : FrostingTask<BuildContext> { }
