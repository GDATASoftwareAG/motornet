using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Moq;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using Motor.Extensions.Hosting.Timer;
using Quartz;
using Xunit;

namespace Motor.Extensions.Hosting.Timer_IntegrationTest
{
    public class TimerTests
    {
        [Fact]
        public async Task TimerStartAsync_EverySecond_CallbackCalled()
        {
            var config = GetTimerConfig("*", "*", "*", "*/1"); //every second
            var queue = new Mock<IBackgroundTaskQueue<MotorCloudEvent<IJobExecutionContext>>>();
            var timer = CreateTimerService(config, queue.Object);
            await timer.StartAsync(new CancellationToken());

            await Task.Delay(TimeSpan.FromSeconds(3));

            queue.Verify(messageHandler =>
                    messageHandler.QueueBackgroundWorkItem(It.IsAny<MotorCloudEvent<IJobExecutionContext>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task TimerStartAsync_EveryMinute_CallbackNotCalled()
        {
            var config = GetTimerConfig("*", "0", "*/1", "0"); //every minute
            var queue = new Mock<IBackgroundTaskQueue<MotorCloudEvent<IJobExecutionContext>>>();
            var timer = CreateTimerService(config, queue.Object);
            await timer.StartAsync(CancellationToken.None);

            await Task.Delay(TimeSpan.FromSeconds(5));

            queue.Verify(messageHandler =>
                    messageHandler.QueueBackgroundWorkItem(It.IsAny<MotorCloudEvent<IJobExecutionContext>>()),
                Times.Never);
        }

        private Timer.Timer CreateTimerService(IOptions<TimerOptions> config,
            IBackgroundTaskQueue<MotorCloudEvent<IJobExecutionContext>> queue)
        {
            var timer = new Timer.Timer(config, queue, GetApplicationNameService());
            return timer;
        }

        private IApplicationNameService GetApplicationNameService(string source = "test://non")
        {
            var mock = new Mock<IApplicationNameService>();
            mock.Setup(t => t.GetSource()).Returns(new Uri(source));
            return mock.Object;
        }

        private IOptions<TimerOptions> GetTimerConfig(string days, string hours, string minutes, string seconds)
        {
            var timerConfig = Options.Create(new TimerOptions
            {
                Days = days,
                Hours = hours,
                Minutes = minutes,
                Seconds = seconds
            });
            return timerConfig;
        }
    }
}
