using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ConsumeAndMultiOutputPublisherWithRabbitMQ.Model;
using Motor.Extensions.Hosting.Abstractions;

namespace ConsumeAndMultiOutputPublisherWithRabbitMQ
{
    public class MultiOutputService : IMultiOutputService<InputMessage, OutputMessage>
    {
        // Handle incoming messages
        public async IAsyncEnumerable<MotorCloudEvent<OutputMessage>> ConvertMessageAsync(
            MotorCloudEvent<InputMessage> inputEvent, [EnumeratorCancellation] CancellationToken token = default)
        {
            // Get the input message from the cloud event
            var input = inputEvent.TypedData;

            // Do your magic here .....
            var output = MagicFuncAsync(input);

            // Create a new cloud event from your output message which is automatically published and return a new task.
            await foreach (var outputMessage in output.WithCancellation(token))
            {
                yield return inputEvent.CreateNew(outputMessage);
            }
        }

        private static async IAsyncEnumerable<OutputMessage> MagicFuncAsync(InputMessage input)
        {
            if (string.IsNullOrEmpty(input.FancyText))
            {
                // Reject message in RabbitMQ queue (Any ArgumentException can be used to reject to messages.).
                throw new ArgumentNullException("FancyText is empty");
            }

            // Magic async function.
            await Task.Delay(10).ConfigureAwait(false);

            yield return new()
            {
                NotSoFancyText = input.FancyText.Reverse().ToString(),
                NotSoFancyNumber = input.FancyNumber * -1,
            };
            yield return new()
            {
                NotSoFancyText = input.FancyText,
                NotSoFancyNumber = input.FancyNumber * -2,
            };
        }
    }
}
