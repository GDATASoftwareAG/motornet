using System.Linq;
using Moq;
using Motor.Extensions.Diagnostics.Metrics;
using Motor.Extensions.Diagnostics.Metrics.Abstractions;
using Prometheus.Client;
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
        public void CreateCounter1_WithNameSpace_EnsureNameAsExpected()
        {
            var factory = GetFactory();

            factory.CreateCounter("some_counter_name", "some_help", false);

            Assert.Equal("motor_extensions_diagnostics_metrics_unittest_some_counter_name",
                factory.Names.FirstOrDefault());
        }

        [Fact]
        public void CreateCounter2_WithNameSpace_EnsureNameAsExpected()
        {
            var factory = GetFactory();

            factory.CreateCounter("some_counter_name", "some_help", "and_some_label", false);

            Assert.Equal("motor_extensions_diagnostics_metrics_unittest_some_counter_name",
                factory.Names.FirstOrDefault());
        }

        [Fact]
        public void CreateCounter3_WithNameSpace_EnsureNameAsExpected()
        {
            var factory = GetFactory();

            factory.CreateCounter("some_counter_name", "some_help", false, "and_some_label");

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
        public void CreateGauge1_WithNameSpace_EnsureNameAsExpected()
        {
            var factory = GetFactory();

            factory.CreateGauge("some_counter_name", "some_help", false);

            Assert.Equal("motor_extensions_diagnostics_metrics_unittest_some_counter_name",
                factory.Names.FirstOrDefault());
        }

        [Fact]
        public void CreateGauge2_WithNameSpace_EnsureNameAsExpected()
        {
            var factory = GetFactory();

            factory.CreateGauge("some_counter_name", "some_help", "and_some_label", false);

            Assert.Equal("motor_extensions_diagnostics_metrics_unittest_some_counter_name",
                factory.Names.FirstOrDefault());
        }

        [Fact]
        public void CreateGauge3_WithNameSpace_EnsureNameAsExpected()
        {
            var factory = GetFactory();

            factory.CreateGauge("some_counter_name", "some_help", false, "and_some_label");

            Assert.Equal("motor_extensions_diagnostics_metrics_unittest_some_counter_name",
                factory.Names.FirstOrDefault());
        }

        [Fact]
        public void CreateHistogram_WithNameSpace_EnsureNameAsExpected()
        {
            var factory = GetFactory();

            factory.CreateHistogram("histo_name", "help_me_please", "label");

            Assert.Equal("motor_extensions_diagnostics_metrics_unittest_histo_name", factory.Names.FirstOrDefault());
        }

        [Fact]
        public void CreateHistogram1_WithNameSpace_EnsureNameAsExpected()
        {
            var factory = GetFactory();

            factory.CreateHistogram("some_counter_name", "some_help", false);

            Assert.Equal("motor_extensions_diagnostics_metrics_unittest_some_counter_name",
                factory.Names.FirstOrDefault());
        }

        [Fact]
        public void CreateHistogram2_WithNameSpace_EnsureNameAsExpected()
        {
            var factory = GetFactory();

            factory.CreateHistogram("some_counter_name", "some_help", "and_some_label", false);

            Assert.Equal("motor_extensions_diagnostics_metrics_unittest_some_counter_name",
                factory.Names.FirstOrDefault());
        }

        [Fact]
        public void CreateHistogram3_WithNameSpace_EnsureNameAsExpected()
        {
            var factory = GetFactory();

            factory.CreateHistogram("some_counter_name", "some_help", false, "and_some_label");

            Assert.Equal("motor_extensions_diagnostics_metrics_unittest_some_counter_name",
                factory.Names.FirstOrDefault());
        }

        [Fact]
        public void CreateSummary_WithNameSpace_EnsureNameAsExpected()
        {
            var factory = GetFactory();

            factory.CreateSummary("some_name", "some_help", "and_some_label");

            Assert.Equal("motor_extensions_diagnostics_metrics_unittest_some_name", factory.Names.FirstOrDefault());
        }

        [Fact]
        public void CreateSummary1_WithNameSpace_EnsureNameAsExpected()
        {
            var factory = GetFactory();

            factory.CreateSummary("some_counter_name", "some_help", false);

            Assert.Equal("motor_extensions_diagnostics_metrics_unittest_some_counter_name",
                factory.Names.FirstOrDefault());
        }

        [Fact]
        public void CreateSummary2_WithNameSpace_EnsureNameAsExpected()
        {
            var factory = GetFactory();

            factory.CreateSummary("some_counter_name", "some_help", "and_some_label", false);

            Assert.Equal("motor_extensions_diagnostics_metrics_unittest_some_counter_name",
                factory.Names.FirstOrDefault());
        }

        [Fact]
        public void CreateSummary3_WithNameSpace_EnsureNameAsExpected()
        {
            var factory = GetFactory();

            factory.CreateSummary("some_counter_name", "some_help", false, "and_some_label");

            Assert.Equal("motor_extensions_diagnostics_metrics_unittest_some_counter_name",
                factory.Names.FirstOrDefault());
        }

        private IMetricsFactory<MetricsFactoryTests> GetFactory()
        {
            var mock = new Mock<IMotorMetricsFactory>();
            mock.SetupGet(t => t.Factory).Returns(new MetricFactory(new CollectorRegistry()));
            return new MetricsFactory<MetricsFactoryTests>(mock.Object);
        }
    }
}
