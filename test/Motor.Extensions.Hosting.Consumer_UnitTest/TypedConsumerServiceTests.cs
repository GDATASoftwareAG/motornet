using Motor.Extensions.Conversion.Abstractions;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.Consumer;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Motor.Extensions.Hosting.Consumer_UnitTest
{
    public class TypedConsumerServiceTests
    {
        [Fact]
        public void SingleMessageConsumeAsync_()
        {
            var logger = new Mock<ILogger<TypedConsumerService<string>>>();
            var queue = new Mock<IBackgroundTaskQueue<MotorCloudEvent<string>>>();
            var deserializer = new Mock<IMessageDeserializer<string>>();
            var consumer = new Mock<IMessageConsumer<string>>();
            var consumerService = new TypedConsumerService<string>(logger.Object, queue.Object, deserializer.Object, consumer.Object);
        }
    }
}
