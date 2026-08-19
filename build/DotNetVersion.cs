using System.ComponentModel;

namespace build;

public enum DotNetVersion
{
    [Description("net8.0")]
    Net8,

    [Description("net9.0")]
    Net9,

    [Description("net10.0")]
    Net10,
}

public static class DotNetVersionExtensions
{
    public static string DotnetVersionName(this DotNetVersion version) => $"{version.ToString().ToLower()}.0";
}
