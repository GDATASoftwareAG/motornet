using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConsumeAndPublishWithRabbitMQ.Model;
using Motor.Extensions.Hosting.Abstractions;

namespace ConsumeAndPublishWithRabbitMQ
{
    public class BasicConverter : IMessageConverter<InputMessage, OutputMessage>
    {
        // Handle incoming messages
        public Task<MotorCloudEvent<OutputMessage>> ConvertMessageAsync(
            MotorCloudEvent<InputMessage> inputEvent, 
            CancellationToken token = default)
        {
            // Get the input message from the cloud event
            var input = inputEvent.TypedData;

            // Do your magic here .....

            if(string.IsNullOrEmpty(input.FancyText))
            {
                // Reject message in RabbitMQ queue (Any ArgumentException can be used to reject to messages.).
                throw new ArgumentNullException("FancyText is empty");
            }

            var output = new OutputMessage
            {
                NotSoFancyText = input.FancyText.Reverse().ToString(),
                NotSoFancyNumber = input.FancyNumber * -1,
            };

            // Create a new cloud event from your output message which is automatically published and return a new task.
            var outputEvent = inputEvent.CreateNew(output);
            return Task.FromResult(outputEvent);
        }
    }
}
