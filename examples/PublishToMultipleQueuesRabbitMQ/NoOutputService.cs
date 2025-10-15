using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using PublishToMultipleQueuesRabbitMQ.Model;

namespace PublishToMultipleQueuesRabbitMQ;

public class NoOutputService(
    ITypedMessagePublisher<LeftMessage> leftPublisher,
    ITypedMessagePublisher<RightMessage> rightPublisher)
    : INoOutputService<InputMessage>
{
    // Handle incoming messages
    private static int _messageCount = 0;

    public Task<ProcessedMessageStatus> HandleMessageAsync(MotorCloudEvent<InputMessage> inputEvent, CancellationToken token = default)
    {
        // Get the input message from the cloud event
        var input = inputEvent.TypedData;

        // Sometimes we dont have anything to publish.
        if (string.IsNullOrEmpty(input.FancyText))
        {
            // Might need to be logged, or published to some additional queue, or can simply be ignored. 
            return Task.FromResult(ProcessedMessageStatus.Success);
        }

        _messageCount++;

        // In all other cases we publish to the queue dictated by our business logic.
        if (_messageCount % 2 == 0)
        {
            var left = CreateLeftMessage(input);
            leftPublisher.PublishMessageAsync(inputEvent.CreateNew(left), token);
        }
        else
        {
            var right = CreateRightMessage(input);
            rightPublisher.PublishMessageAsync(inputEvent.CreateNew(right), token);
        }

        return Task.FromResult(ProcessedMessageStatus.Success);
    }

    private static LeftMessage CreateLeftMessage(InputMessage input)
    {
        var output = new LeftMessage
        {
            NotSoFancyText = input.FancyText.Reverse().ToString(),
            NotSoFancyNumber = input.FancyNumber * -1,
        };
        return output;
    }

    private static RightMessage CreateRightMessage(InputMessage input)
    {
        var output = new RightMessage
        {
            NotSoFancyText = input.FancyText + " " + DateTime.Now,
            NotSoFancyNumber = input.FancyNumber * 3,
        };

        return output;
    }
}
