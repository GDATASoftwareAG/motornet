using System;
using System.Threading;
using System.Threading.Tasks;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.Internal;
using Xunit;

namespace Motor.Extensions.Hosting_UnitTest
{
    public class BackgroundTaskQueueTests
    {
        [Fact]
        public async Task DequeueAsync_MessageInQueues_GetMessage()
        {
            var message = "test";
            var backgroundTaskQueue = new BackgroundTaskQueue<string>(null);

            var queueBackgroundWorkItem = backgroundTaskQueue.QueueBackgroundWorkItem(message);

            var dequeueAsync = await backgroundTaskQueue.DequeueAsync(CancellationToken.None);
            Assert.Equal(message, dequeueAsync.Item);
            Assert.Equal(TaskStatus.WaitingForActivation, queueBackgroundWorkItem.Status);
        }

        [Fact]
        public async Task QueueBackgroundWorkItem_()
        {
            var backgroundTaskQueue = new BackgroundTaskQueue<string>(null);
            var queueBackgroundWorkItem = backgroundTaskQueue.QueueBackgroundWorkItem("test");
            var dequeueAsync = await backgroundTaskQueue.DequeueAsync(CancellationToken.None);

            dequeueAsync.TaskCompletionStatus.SetResult(ProcessedMessageStatus.Success);

            var processedMessageStatus = await queueBackgroundWorkItem;
            Assert.Equal(TaskStatus.RanToCompletion, queueBackgroundWorkItem.Status);
            Assert.Equal(ProcessedMessageStatus.Success, processedMessageStatus);
        }

        [Fact]
        public async Task QueueBackgroundWorkItem_NullInput_ThrowArgument()
        {
            var backgroundTaskQueue = new BackgroundTaskQueue<string>(null);

            await Assert.ThrowsAsync<ArgumentNullException>(() => backgroundTaskQueue.QueueBackgroundWorkItem(null));
        }
    }
}
