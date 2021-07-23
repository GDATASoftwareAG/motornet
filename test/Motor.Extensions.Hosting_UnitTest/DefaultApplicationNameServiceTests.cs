using System.Reflection;
using Microsoft.Extensions.Options;
using Motor.Extensions.Hosting;
using Xunit;

namespace Motor.Extensions.Hosting_UnitTest
{
    public class DefaultApplicationNameServiceTests
    {
        [Theory]
        [InlineData("test", "IpFeatureExtraction.Console", "test/ip-feature-extraction")]
        [InlineData("test", "IpFeatureExtraction.Service", "test/ip-feature-extraction")]
        [InlineData("test", "IpFeatureExtractionConsole", "test/ip-feature-extraction")]
        [InlineData("test", "IpFeatureExtractionService", "test/ip-feature-extraction")]
        public void ExtractServiceName_ProductAndName_UnifiedServiceName(string product, string assembly,
            string expected)
        {
            var application = CreateDefaultApplicationNameService();

            var extractedServiceName = application.ExtractServiceName(product, assembly);

            Assert.Equal(expected, extractedServiceName);
        }

        [Fact]
        public void GetFullName_FullNameWithOverride_SameFullName()
        {
            var options = new DefaultApplicationNameOptions
            {
                FullName = "test",
            };
            var application = CreateDefaultApplicationNameService(options);

            var actualFullName = application.GetFullName();

            Assert.Equal(options.FullName, actualFullName);
        }

        [Fact]
        public void GetFullName_FullNameWithoutOverride_DifferentFullName()
        {
            var options = new DefaultApplicationNameOptions
            {
                FullName = "",
            };
            var application = CreateDefaultApplicationNameService(options);

            var actualFullName = application.GetFullName();

            Assert.NotEqual(options.FullName, actualFullName);
        }

        [Fact]
        public void GetSource_SourceWithOverride_SameSource()
        {
            var options = new DefaultApplicationNameOptions
            {
                Source = "motor://test/",
            };
            var application = CreateDefaultApplicationNameService(options);

            var actualSource = application.GetSource();

            Assert.Equal(options.Source, actualSource.ToString());
        }

        [Fact]
        public void GetSource_SourceWithoutOverride_DifferentSource()
        {
            var options = new DefaultApplicationNameOptions
            {
                Source = "",
            };
            var application = CreateDefaultApplicationNameService(options);

            var actualSource = application.GetSource();

            Assert.NotEqual(options.Source, actualSource.ToString());
        }

        private static DefaultApplicationNameService CreateDefaultApplicationNameService(DefaultApplicationNameOptions? fullName = null)
        {
            return new(Assembly.GetAssembly(typeof(DefaultApplicationNameServiceTests))!, new OptionsWrapper<DefaultApplicationNameOptions>(fullName ?? new DefaultApplicationNameOptions()));
        }
    }
}
