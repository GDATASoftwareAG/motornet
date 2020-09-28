using System.Linq;
using Microsoft.Extensions.Options;
using Moq;
using Motor.Extensions.Diagnostics.Metrics;
using Motor.Extensions.Diagnostics.Metrics.Abstractions;
using Motor.Extensions.Hosting.Abstractions;
using Prometheus.Client.AspNetCore;
using Prometheus.Client.Collectors;
using Xunit;

namespace Motor.Extensions.Diagnostics.Metrics_UnitTest
{
    public class MetricsFactoryTests
    {
        [Fact]
        public void CreateCounter_WithNameSpace_EnsureNameAsExpected()
        {
            var factory = GetFactory();

            factory.CreateCounter("some_counter_name", "some_help", "and_some_label");

            Assert.Equal("motor_extensions_diagnostics_metrics_unittest_some_counter_name",
                factory.Names.FirstOrDefault());
        }

        [Fact]
        public void CreateGauge_WithNameSpace_EnsureNameAsExpected()
        {
            var factory = GetFactory();

            factory.CreateGauge("gauge_name", "some_help", "and_some_label");

            Assert.Equal("motor_extensions_diagnostics_metrics_unittest_gauge_name", factory.Names.FirstOrDefault());
        }

        [Fact]
        public void CreateHistogram_WithNameSpace_EnsureNameAsExpected()
        {
            var factory = GetFactory();

            factory.CreateHistogram("histo_name", "help_me_please", "label");

            Assert.Equal("motor_extensions_diagnostics_metrics_unittest_histo_name", factory.Names.FirstOrDefault());
        }

        [Fact]
        public void CreateSummary_WithNameSpace_EnsureNameAsExpected()
        {
            var factory = GetFactory();

            factory.CreateSummary("some_name", "some_help", "and_some_label");

            Assert.Equal("motor_extensions_diagnostics_metrics_unittest_some_name", factory.Names.FirstOrDefault());
        }

        private IMetricsFactory<MetricsFactoryTests> GetFactory()
        {
            var promOptions = new PrometheusOptions
            {
                CollectorRegistryInstance = new CollectorRegistry()
            };
            var optionsMock = new OptionsWrapper<PrometheusOptions>(promOptions);
            var mock = new Mock<IApplicationNameService>();
            mock.Setup(t => t.GetVersion()).Returns("test");
            mock.Setup(t => t.GetLibVersion()).Returns("test");
            return new MetricsFactory<MetricsFactoryTests>(optionsMock, mock.Object);
        }
    }
}
