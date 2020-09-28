using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Motor.Extensions.Hosting;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.Internal;
using Motor.Extensions.TestUtilities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Motor.Extensions.Hosting_UnitTest
{
    public class QueuedGenericServiceTests
    {
        [Fact]
        public async Task ExecuteAsync_CancellationTokenIsCanceled_StopProcessing()
        {
            var queuedGenericService = CreateQueuedGenericService();
            
            await queuedGenericService.StartAsync(CancellationToken.None);

            await queuedGenericService.StopAsync(CancellationToken.None);
        }

        [Fact]
        public async Task ExecuteAsync_Messages_HandleMessageAsyncIsCalled()
        {
            var messageHandler = new Mock<IMessageHandler<string>>();
            var queuedGenericService = CreateQueuedGenericService(messageHandler.Object);
            
            await queuedGenericService.StartAsync(CancellationToken.None);
            await Task.Delay(100);
            await queuedGenericService.StopAsync(CancellationToken.None);
            
            messageHandler.Verify(t => t.HandleMessageAsync(It.IsAny<MotorCloudEvent<string>>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(4)]
        [InlineData(8)]
        [InlineData(null)]
        public async void ExecuteAsync_MultipleMessage_ParallelProcessingBasedOnConfig(int? parallelProcessesOrProcessorCount)
        {
            parallelProcessesOrProcessorCount ??= Environment.ProcessorCount;
            var waitTimeInMs = 100;
            var taskCompletionSources = new List<TaskCompletionSource<ProcessedMessageStatus>>();
            var queue = new Mock<IBackgroundTaskQueue<MotorCloudEvent<string>>>();
            var setupSequentialResult = queue.SetupSequence(t => t.DequeueAsync(It.IsAny<CancellationToken>()));
            for (var i = 0; i < parallelProcessesOrProcessorCount * 2; i++)
            {
                var source = new TaskCompletionSource<ProcessedMessageStatus>();
                setupSequentialResult = setupSequentialResult.ReturnsAsync(() =>
                    new QueueItem<MotorCloudEvent<string>>(
                        MotorCloudEvent.CreateTestCloudEvent(string.Empty, new Uri("test://non")),
                        source));
                taskCompletionSources.Add(source);
            }
            
            var messageHandler = new Mock<IMessageHandler<string>>();
            messageHandler.Setup(t =>
                    t.HandleMessageAsync(It.IsAny<MotorCloudEvent<string>>(), It.IsAny<CancellationToken>()))
                .Returns(async () =>
                {
                    await Task.Delay(waitTimeInMs);
                    return ProcessedMessageStatus.Success;
                });
            var queuedGenericService = CreateQueuedGenericService(messageHandler.Object, queue.Object, config: new QueuedGenericServiceConfig
            {
                ParallelProcesses = parallelProcessesOrProcessorCount
            });
            
            await queuedGenericService.StartAsync(CancellationToken.None);
            await Task.Delay(Convert.ToInt32(Math.Floor(waitTimeInMs * 0.5)));
            await queuedGenericService.StopAsync(CancellationToken.None);
            
            messageHandler.Verify(t => t
                .HandleMessageAsync(It.IsAny<MotorCloudEvent<string>>(), It.IsAny<CancellationToken>()),
                Times.Exactly(parallelProcessesOrProcessorCount.Value));
            var done = taskCompletionSources
                .Count(completionSource => completionSource.Task.Status == TaskStatus.RanToCompletion);
            Assert.Equal(parallelProcessesOrProcessorCount, done);
        }

        [Theory]
        [InlineData(ProcessedMessageStatus.Success)]
        [InlineData(ProcessedMessageStatus.InvalidInput)]
        [InlineData(ProcessedMessageStatus.TemporaryFailure)]
        [InlineData(ProcessedMessageStatus.CriticalFailure)]
        public async Task ExecuteAsync_MessagesProcessingStatus_TaskCompletionSourceIsCompleted(ProcessedMessageStatus expectedStatus)
        {
            var taskCompletionSource = new TaskCompletionSource<ProcessedMessageStatus>();
            var queue = CreateQueue(status: taskCompletionSource);
            
            var messageHandler = new Mock<IMessageHandler<string>>();
            messageHandler
                .Setup(t => t.HandleMessageAsync(It.IsAny<MotorCloudEvent<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedStatus);
            
            var queuedGenericService = CreateQueuedGenericService(messageHandler.Object, backgroundTaskQueue: queue);
            
            await queuedGenericService.StartAsync(CancellationToken.None);
            await Task.Delay(100);
            
            var actualStatus = await taskCompletionSource.Task;
            Assert.Equal(expectedStatus, actualStatus);
            
            await queuedGenericService.StopAsync(CancellationToken.None);
        }

        [Fact]
        public async Task ExecuteAsync_MessagesProcessStatusSuccess_NeverCallStopApplication()
        {
            var messageHandler = new Mock<IMessageHandler<string>>();
            messageHandler
                .Setup(t => t.HandleMessageAsync(It.IsAny<MotorCloudEvent<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ProcessedMessageStatus.Success);
            var hostApplicationLifetime = new Mock<IHostApplicationLifetime>();

            var queuedGenericService = CreateQueuedGenericService(messageHandler.Object, hostApplicationLifetime: hostApplicationLifetime.Object);
            
            await queuedGenericService.StartAsync(CancellationToken.None);
            await Task.Delay(100);
            await queuedGenericService.StopAsync(CancellationToken.None);
            
            hostApplicationLifetime.Verify(t=> t.StopApplication(), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_MessagesProcessStatusCritical_StopApplication()
        {
            var messageHandler = new Mock<IMessageHandler<string>>();
            messageHandler
                .Setup(t => t.HandleMessageAsync(It.IsAny<MotorCloudEvent<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ProcessedMessageStatus.CriticalFailure);
            var hostApplicationLifetime = new Mock<IHostApplicationLifetime>();

            var queuedGenericService = CreateQueuedGenericService(messageHandler.Object, hostApplicationLifetime: hostApplicationLifetime.Object);
            
            await queuedGenericService.StartAsync(CancellationToken.None);
            await Task.Delay(100);
            await queuedGenericService.StopAsync(CancellationToken.None);
            
            hostApplicationLifetime.Verify(t=> t.StopApplication(), Times.AtLeastOnce);
        }
        
        private static IBackgroundTaskQueue<MotorCloudEvent<string>> CreateQueue(MotorCloudEvent<string> dataCloudEvent = null, TaskCompletionSource<ProcessedMessageStatus> status = null)
        {
            var queue = new Mock<IBackgroundTaskQueue<MotorCloudEvent<string>>>();
            queue.Setup(t => t.DequeueAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                    new QueueItem<MotorCloudEvent<string>>(dataCloudEvent ?? MotorCloudEvent.CreateTestCloudEvent(string.Empty, new Uri("test://non")),
                        status ?? new TaskCompletionSource<ProcessedMessageStatus>()));
            return queue.Object;
        }

        private static QueuedGenericService<string> CreateQueuedGenericService(
            IMessageHandler<string> messageHandler = null, 
            IBackgroundTaskQueue<MotorCloudEvent<string>> backgroundTaskQueue = null, 
            IHostApplicationLifetime hostApplicationLifetime = null,
            QueuedGenericServiceConfig config = null)
        {
            var logger = new Mock<ILogger<QueuedGenericService<string>>>();
            hostApplicationLifetime ??= new Mock<IHostApplicationLifetime>().Object;
            backgroundTaskQueue ??= CreateQueue();
            messageHandler ??= new Mock<IMessageHandler<string>>().Object;
            var options = new OptionsWrapper<QueuedGenericServiceConfig>(config ?? new QueuedGenericServiceConfig());
            var baseDelegatingMessageHandler = CreateBaseDelegatingMessageHandler(messageHandler);
            
            return new QueuedGenericService<string>(
                logger.Object, 
                options,
                hostApplicationLifetime,
                backgroundTaskQueue, 
                baseDelegatingMessageHandler);
        }

        private static BaseDelegatingMessageHandler<string> CreateBaseDelegatingMessageHandler(IMessageHandler<string> messageHandler = null)
        {
            var loggerPrepare = new Mock<ILogger<PrepareDelegatingMessageHandler<string>>>();
            messageHandler ??= new Mock<IMessageHandler<string>>().Object;
            return new BaseDelegatingMessageHandler<string>(
                new PrepareDelegatingMessageHandler<string>(loggerPrepare.Object),
                messageHandler,
                new List<DelegatingMessageHandler<string>>());
        }
    }
}
