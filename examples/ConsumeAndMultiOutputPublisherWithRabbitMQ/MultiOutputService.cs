using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConsumeAndMultiOutputPublisherWithRabbitMQ.Model;
using Motor.Extensions.Hosting.Abstractions;

namespace ConsumeAndMultiOutputPublisherWithRabbitMQ
{
    public class MultiOutputService : IMultiOutputService<InputMessage, OutputMessage>
    {
        // Handle incoming messages
        public Task<IEnumerable<MotorCloudEvent<OutputMessage>>> ConvertMessageAsync(
            MotorCloudEvent<InputMessage> inputEvent,
            CancellationToken token = default)
        {
            // Get the input message from the cloud event
            var input = inputEvent.TypedData;

            // Do your magic here .....
            var output = MagicFunc(input);

            // Create a new cloud event from your output message which is automatically published and return a new task.
            var outputEvent = output.Select(singleEvent => inputEvent.CreateNew(singleEvent));
            return Task.FromResult(outputEvent);
        }

        private static IEnumerable<OutputMessage> MagicFunc(InputMessage input)
        {
            if (string.IsNullOrEmpty(input.FancyText))
            {
                // Reject message in RabbitMQ queue (Any ArgumentException can be used to reject to messages.).
                throw new ArgumentNullException("FancyText is empty");
            }

            return new List<OutputMessage>
            {
                new()
                {
                    NotSoFancyText = input.FancyText.Reverse().ToString(),
                    NotSoFancyNumber = input.FancyNumber * -1,
                },
                new()
                {
                    NotSoFancyText = input.FancyText,
                    NotSoFancyNumber = input.FancyNumber * -2,
                },
            };
        }
    }
}
