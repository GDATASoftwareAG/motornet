using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cake.Common;
using Cake.Common.Build;
using Cake.Common.Build.GitHubActions;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Build;
using Cake.Common.Tools.DotNet.Restore;
using Cake.Common.Tools.DotNet.Test;
using Cake.Core;
using Cake.Core.IO;
using Cake.DotNetLocalTools.Module;
using Cake.Frosting;
using Path = System.IO.Path;

return new CakeHost()
    .UseWorkingDirectory(Path.Combine(Directory.GetCurrentDirectory(), ".."))
    .UseModule<LocalToolsModule>()
    .InstallToolsFromManifest(Path.Combine(Directory.GetCurrentDirectory(), "..", ".config", "dotnet-tools.json"))
    .UseContext<BuildContext>()
    .Run(args);

public class BuildContext : FrostingContext
{
    public IEnumerable<string> DotNetVersions { get; } = ["net8.0", "net9.0"];
    public string SolutionFileName => "Motor.NET.slnx";
    public string SolutionDirectory => Path.Combine(Directory.GetCurrentDirectory());
    public string SolutionFilePath => Path.Combine(SolutionDirectory, SolutionFileName);
    public string ArtifactsDirectory { get; }
    public string BridgeArtifactsDirectory { get; }
    public string BuildConfiguration { get; }
    public IGitHubActionsProvider GitHubContext { get; }
    public string NuGetFeed { get; }
    public string NuGetApiKey { get; }

    public BuildContext(ICakeContext context)
        : base(context)
    {
        if (context.HasArgument("dotnet-versions"))
        {
            DotNetVersions = context.Arguments.GetArguments("dotnet-versions");
        }

        BuildConfiguration = context.Argument("build-configuration", "Release");
        ArtifactsDirectory = context.Argument(
            "artifacts-directory",
            Path.Combine(Directory.GetCurrentDirectory(), "artifacts")
        );
        BridgeArtifactsDirectory = context.Argument(
            "bridge-artifacts-directory",
            Path.Combine(Directory.GetCurrentDirectory(), "artifacts-bridge")
        );
        NuGetFeed = context.EnvironmentVariable("NUGET_FEED", "https://api.nuget.org/v3/index.json");
        NuGetApiKey = context.EnvironmentVariable("NUGET_API_KEY", "");

        GitHubContext = context.GitHubActions();
    }
}

[TaskName("Restore")]
public sealed class RestoreTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.DotNetRestore(context.SolutionFilePath, new DotNetRestoreSettings { UseLockFile = true });
    }
}

[TaskName("Format")]
public sealed class FormatTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context) =>
        context.DotNetTool(context.SolutionFilePath, "csharpier", ProcessArgumentBuilder.FromStrings(["format", "."]));
}

[TaskName("CheckFormat")]
public sealed class CheckFormatTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context) =>
        context.DotNetTool(context.SolutionFilePath, "csharpier", ProcessArgumentBuilder.FromStrings(["check", "."]));
}

[TaskName("Clean")]
public sealed class CleanTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.CleanDirectories(context.ArtifactsDirectory);

        var binDirs = context.GetDirectories("**/bin/");
        var objDirs = context.GetDirectories("**/obj/");

        context.DeleteDirectories(binDirs.Union(objDirs), new DeleteDirectorySettings { Recursive = true });
    }
}

[TaskName("Build")]
[IsDependentOn(typeof(CleanTask))]
[IsDependentOn(typeof(RestoreTask))]
public sealed class BuildTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        foreach (var dotNetVersion in context.DotNetVersions)
        {
            context.Information("Building for .NET {0}", dotNetVersion);
            Build(context, dotNetVersion);
        }
    }

    private void Build(BuildContext context, string dotNetVersion) =>
        context.DotNetBuild(
            context.SolutionFilePath,
            new DotNetBuildSettings
            {
                Configuration = context.BuildConfiguration,
                NoRestore = true,
                Framework = dotNetVersion,
            }
        );
}

[TaskName("Test")]
[IsDependentOn(typeof(BuildTask))]
public sealed class TestTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        foreach (var dotNetVersion in context.DotNetVersions)
        {
            context.Information("Running tests for .NET {0}", dotNetVersion);
            Test(context, dotNetVersion);
        }
    }

    private void Test(BuildContext context, string dotnetVersion) =>
        context.DotNetTest(
            context.SolutionFilePath,
            new DotNetTestSettings
            {
                Configuration = context.BuildConfiguration,
                NoRestore = true,
                NoBuild = true,
                Framework = dotnetVersion,
            }
        );
}

[TaskName("Default")]
[IsDependentOn(typeof(TestTask))]
public class DefaultTask : FrostingTask { }
