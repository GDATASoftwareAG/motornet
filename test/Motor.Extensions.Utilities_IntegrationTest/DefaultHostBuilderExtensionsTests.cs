using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Motor.Extensions.Utilities_IntegrationTest;

[Collection("GenericHosting")]
public class DefaultHostBuilderExtensionsTests
{
    [Theory]
    [InlineData("local", "appsettings.json")]
#if NET9_0_OR_GREATER
    [InlineData("", "appsettings.json")]
#else
    [InlineData("", "appsettings.Production.json")]
#endif
    [InlineData("Production", "appsettings.Production.json")]
    [InlineData("Development", "appsettings.Development.json")]
    public void ConfigureDefaultSettingsBehaviour_SetEnv_EnvConfigLoadedAndDefaultValueReplaced(string env,
        string expected)
    {
        var toOverwriteValue = "";
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", env);
        Environment.SetEnvironmentVariable("TestConfig", null);
        Host.CreateDefaultBuilder()
            .ConfigureServices((hostContext, _) =>
            {
                toOverwriteValue = hostContext.Configuration.GetValue<string>("TestConfig");
            })
            .Build();

        Assert.Equal(expected, toOverwriteValue);
    }

    [Theory]
    [InlineData("local")]
    [InlineData("")]
    [InlineData("Production")]
    [InlineData("Development")]
    public void ConfigureDefaultSettingsBehaviour_SetEnv_EnvConfigLoadedAndDefaultUnchanged(string env)
    {
        var unchangedValue = "";
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", env);
        Environment.SetEnvironmentVariable("TestConfig", null);
        Host.CreateDefaultBuilder()
            .ConfigureServices((hostContext, _) =>
            {
                unchangedValue = hostContext.Configuration.GetSection("OltpExporter").GetValue<string>("Endpoint");
            })
            .Build();

        Assert.Equal("http://localhost:4317", unchangedValue);
    }

    [Theory]
    [InlineData("local")]
    [InlineData("")]
    [InlineData("Production")]
    [InlineData("Development")]
    public void ConfigureDefaultSettingsBehaviour_SetEnvAndOverwriteWithEnvVar_EnvVarIsFinalValue(string env)
    {
        var toOverwriteValue = "";
        const string expectedValue = "environment";
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", env);
        Environment.SetEnvironmentVariable("TestConfig", expectedValue);
        Host.CreateDefaultBuilder()
            .ConfigureServices((hostContext, _) =>
            {
                toOverwriteValue = hostContext.Configuration.GetValue<string>("TestConfig");
            })
            .Build();

        Assert.Equal(expectedValue, toOverwriteValue);
    }

    [Theory]
    [InlineData("local")]
    [InlineData("")]
    [InlineData("Production")]
    [InlineData("Development")]
    public void ConfigureDefaultSettingsBehaviour_SetASPEnv_EnvConfigLoadedAndDefaultUnchanged(string env)
    {
        var unchangedValue = "";
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", null);
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", env);
        Environment.SetEnvironmentVariable("TestConfig", null);
        Host.CreateDefaultBuilder()
            .ConfigureServices((hostContext, _) =>
            {
                unchangedValue = hostContext.Configuration.GetSection("OltpExporter").GetValue<string>("Endpoint");
            })
            .Build();

        Assert.Equal("http://localhost:4317", unchangedValue);
    }

    [Theory]
    [InlineData("local")]
    [InlineData("")]
    [InlineData("Production")]
    [InlineData("Development")]
    public void ConfigureDefaultSettingsBehaviour_SetASPEnvAndOverwriteWithEnvVar_EnvVarIsFinalValue(string env)
    {
        var toOverwriteValue = "";
        const string expectedValue = "environment";
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", null);
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", env);
        Environment.SetEnvironmentVariable("TestConfig", expectedValue);
        Host.CreateDefaultBuilder()
            .ConfigureServices((hostContext, _) =>
            {
                toOverwriteValue = hostContext.Configuration.GetValue<string>("TestConfig");
            })
            .Build();

        Assert.Equal(expectedValue, toOverwriteValue);
    }
}
