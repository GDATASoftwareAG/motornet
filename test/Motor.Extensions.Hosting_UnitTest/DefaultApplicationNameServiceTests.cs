using System.Reflection;
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
            var application =
                new DefaultApplicationNameService(Assembly.GetAssembly(typeof(DefaultApplicationNameService)));

            var extractedServiceName = application.ExtractServiceName(product, assembly);

            Assert.Equal(expected, extractedServiceName);
        }
    }
}
