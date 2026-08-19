using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using build;
using Cake.Common;
using Cake.Common.Build;
using Cake.Common.Build.GitHubActions;
using Cake.Core;
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
    private const string SolutionFileName = "Motor.NET.slnx";

    public IEnumerable<DotNetVersion> DotNetVersions { get; } = [DotNetVersion.Net8, DotNetVersion.Net9];
    public string SolutionDirectory => Directory.GetCurrentDirectory();
    public string SolutionFilePath => Path.Combine(SolutionDirectory, SolutionFileName);
    public string TestDirectory => Path.Combine(SolutionDirectory, "test");
    public string ArtifactsDirectory { get; }
    public string BridgeArtifactsDirectory { get; }
    public string BuildConfiguration { get; }
    public int TestExecutionParallelism { get; private set; }
    public IGitHubActionsProvider GitHubContext { get; }
    public string NuGetFeed { get; }
    public string NuGetApiKey { get; }

    public BuildContext(ICakeContext context)
        : base(context)
    {
        if (context.HasArgument("dotnet-versions"))
        {
            DotNetVersions = context
                .Arguments.GetArguments("dotnet-versions")
                .Select(raw => Enum.Parse<DotNetVersion>(raw, true));
        }

        TestExecutionParallelism = context.Argument("test-execution-parallelism", System.Environment.ProcessorCount);
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

[TaskName("Default")]
[IsDependentOn(typeof(NugetPushTask))]
[IsDependentOn(typeof(BridgeContainerImageTask))]
public class DefaultTask : FrostingTask { }
