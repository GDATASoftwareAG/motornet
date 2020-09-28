using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Motor.Extensions.Utilities_IntegrationTest
{
    [Collection("GenericHosting")]
    public class DefaultHostBuilderExtensionsTests
    {
        [Theory]
        [InlineData("local", "appsettings.json")]
        [InlineData("", "appsettings.Production.json")]
        [InlineData("Production", "appsettings.Production.json")]
        [InlineData("Development", "appsettings.Development.json")]
        public void ConfigureDefaultSettingsBehaviour_SetEnv_EnvConfigLoadedAndDefaultValueReplaced(string env, string expected)
        {
            var toOverwriteValue = "";
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", env);
            Environment.SetEnvironmentVariable("TestConfig", null);
            Host.CreateDefaultBuilder()
                .ConfigureServices((hostContext, services) =>
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
                .ConfigureServices((hostContext, services) =>
                {
                    unchangedValue = hostContext.Configuration.GetSection("Tracing").GetValue<string>("AgentHost");
                })
                .Build();

            Assert.Equal("localhost", unchangedValue);
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
                .ConfigureServices((hostContext, services) =>
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
                .ConfigureServices((hostContext, services) =>
                {
                    unchangedValue = hostContext.Configuration.GetSection("Tracing").GetValue<string>("AgentHost");
                })
                .Build();

            Assert.Equal("localhost", unchangedValue);
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
                .ConfigureServices((hostContext, services) =>
                {
                    toOverwriteValue = hostContext.Configuration.GetValue<string>("TestConfig");
                })
                .Build();

            Assert.Equal(expectedValue, toOverwriteValue);
        }
    }
}
