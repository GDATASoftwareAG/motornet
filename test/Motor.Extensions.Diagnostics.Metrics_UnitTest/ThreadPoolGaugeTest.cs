using Moq;
using Motor.Extensions.Diagnostics.Metrics;
using Prometheus.Client.MetricsWriter;
using Xunit;

namespace Motor.Extensions.Diagnostics.Metrics_UnitTest
{
    public class ThreadPoolGaugeTest
    {
        private static readonly string[] ExpectedLabelValues = {
            "MinWorkerThreads",
            "MinCompletionThreads",
            "MaxWorkerThreads",
            "MaxCompletionThreads",
            "ThreadCount",
            "AvailableWorkerThreads",
            "AvailableCompletionThreads"
        };

        [Fact]
        public void TestCollect_MockedWriter_ThreadPoolMetrics()
        {
            var threadPoolGauge = new ThreadPoolGauge();
            var writerMock = new Mock<IMetricsWriter>();
            var sampleWriterMock = new Mock<ISampleWriter>();
            var labelWriterMock = new Mock<ILabelWriter>();

            writerMock
                .Setup(w => w.StartSample(string.Empty))
                .Returns(sampleWriterMock.Object);
            sampleWriterMock
                .Setup(w => w.StartLabels())
                .Returns(labelWriterMock.Object);
            sampleWriterMock
                .Setup(w => w.WriteValue(It.Is<double>(d => d > 0)));
            sampleWriterMock
                .Setup(w => w.EndSample());
            labelWriterMock
                .Setup(w => w.WriteLabel("ThreadPoolState", It.IsIn(ExpectedLabelValues)));

            threadPoolGauge.Collect(writerMock.Object);

            writerMock.Verify(w => w.StartSample(string.Empty), Times.Exactly(ExpectedLabelValues.Length));
            sampleWriterMock.Verify(w => w.StartLabels(), Times.Exactly(ExpectedLabelValues.Length));
            sampleWriterMock.Verify(w => w.WriteValue(It.IsAny<double>()), Times.Exactly(ExpectedLabelValues.Length));
            sampleWriterMock.Verify(w => w.EndSample(), Times.Exactly(ExpectedLabelValues.Length));
            foreach (var expectedLabelValue in ExpectedLabelValues)
            {
                labelWriterMock.Verify(w => w.WriteLabel("ThreadPoolOption", expectedLabelValue), Times.Once);
            }
        }
    }
}
