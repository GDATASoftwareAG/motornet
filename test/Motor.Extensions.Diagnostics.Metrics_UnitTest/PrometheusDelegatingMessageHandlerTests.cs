using Moq;
using Motor.Extensions.Diagnostics.Metrics;
using Motor.Extensions.Diagnostics.Metrics.Abstractions;
using Xunit;

namespace Motor.Extensions.Diagnostics.Metrics_UnitTest;

public class PrometheusDelegatingMessageHandlerTests
{
    [Fact]
    public void Ctor_WithMetricsFactory_SummaryIsCreated()
    {
        var metricsFactoryMock = new Mock<IMetricsFactory<PrometheusDelegatingMessageHandler<string>>>();

        var _ = new PrometheusDelegatingMessageHandler<string>(metricsFactoryMock.Object);

        metricsFactoryMock.Verify(x =>
            x.CreateSummary("message_processing", "Message processing duration in ms",
                false, "status")
    );
    }
}
