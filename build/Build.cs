using System.Linq;
using System.Threading.Tasks;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Build;
using Cake.Common.Tools.DotNet.Test;
using Cake.Core.IO;
using Cake.Frosting;

namespace build;

[TaskName("Restore")]
public sealed class RestoreTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.DotNetRestore(context.SolutionFilePath, new());
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

        var allBinAndObjDirs = context
            .GetDirectories("./src/**/bin/")
            .Union(context.GetDirectories("./src/**/obj/"))
            .Union(context.GetDirectories("./test/**/bin/"))
            .Union(context.GetDirectories("./test/**/obj/"));

        context.DeleteDirectories(allBinAndObjDirs, new DeleteDirectorySettings { Recursive = true });
    }
}

[TaskName("Build")]
[IsDependentOn(typeof(CleanTask))]
[IsDependentOn(typeof(RestoreTask))]
[IsDependentOn(typeof(CheckFormatTask))]
public sealed class BuildTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        foreach (var dotNetVersion in context.DotNetVersions)
        {
            context.Information("Building for .NET {0}", dotNetVersion.DotnetVersionName());
            Build(context, dotNetVersion);
        }
    }

    private void Build(BuildContext context, DotNetVersion dotNetVersion) =>
        context.DotNetBuild(
            context.SolutionFilePath,
            new DotNetBuildSettings
            {
                Configuration = context.BuildConfiguration,
                NoRestore = true,
                Framework = dotNetVersion.DotnetVersionName(),
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

    private void Test(BuildContext context, DotNetVersion dotNetVersion)
    {
        Parallel.ForEach(
            context.GetFiles($"{context.TestDirectory}/**/*UnitTest.csproj"),
            new() { MaxDegreeOfParallelism = context.TestExecutionParallelism },
            testProject =>
            {
                context.Information(
                    "Running tests for project {0} on .NET {1}",
                    testProject.GetFilename(),
                    dotNetVersion
                );
                context.DotNetTest(
                    testProject.FullPath,
                    new DotNetTestSettings
                    {
                        Configuration = context.BuildConfiguration,
                        NoRestore = true,
                        NoBuild = true,
                        Framework = dotNetVersion.DotnetVersionName(),
                        ResultsDirectory = context.ArtifactsDirectory,
                    }
                );
            }
        );

        foreach (var testProject in context.GetFiles($"{context.TestDirectory}/**/*IntegrationTest.csproj"))
        {
            context.Information("Running tests for project {0} on .NET {1}", testProject.GetFilename(), dotNetVersion);
            context.DotNetTest(
                testProject.FullPath,
                new DotNetTestSettings
                {
                    Configuration = context.BuildConfiguration,
                    NoRestore = true,
                    NoBuild = true,
                    Framework = dotNetVersion.DotnetVersionName(),
                    ResultsDirectory = context.ArtifactsDirectory,
                }
            );
        }
    }
}
