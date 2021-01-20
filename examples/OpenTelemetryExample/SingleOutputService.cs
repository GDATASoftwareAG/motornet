using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CloudNative.CloudEvents.Extensions;
using Motor.Extensions.Diagnostics.Tracing;
using OpenTelemetryExample.Model;
using Motor.Extensions.Hosting.Abstractions;

namespace OpenTelemetryExample
{
    public class SingleOutputService : ISingleOutputService<InputMessage, OutputMessage>
    {
        private static readonly ActivitySource ActivitySource = new(nameof(OpenTelemetryExample));

        // Handle incoming messages
        public Task<MotorCloudEvent<OutputMessage>> ConvertMessageAsync(
            MotorCloudEvent<InputMessage> inputEvent,
            CancellationToken token = default)
        {
            // Get the input message from the cloud event
            var input = inputEvent.TypedData;

            if (string.IsNullOrEmpty(input.FancyText))
            {
                // Reject message in RabbitMQ queue (Any ArgumentException can be used to reject to messages.).
                throw new ArgumentNullException("FancyText is empty");
            }

            // Extract ActivityContext from the incoming CloudEvent
            var parentContext = inputEvent.Extension<DistributedTracingExtension>()?.GetActivityContext() ?? default;

            // Create new Activity with extracted ActivityContext as parent
            using var activity =
                ActivitySource.StartActivity(nameof(MagicFunc), ActivityKind.Consumer, parentContext);
            activity?.SetTag("fancyText", input.FancyText);

            // Add Trace for MagicFunc with a tag
            OutputMessage output;
            using (activity?.Start())
            {
                // Do your magic here .....
                output = MagicFunc(input);
            }

            // Create a new cloud event from your output message which is automatically published and return a new task.
            var outputEvent = inputEvent.CreateNew(output);
            return Task.FromResult(outputEvent);
        }

        private static OutputMessage MagicFunc(InputMessage input)
        {
            var output = new OutputMessage
            {
                NotSoFancyText = input.FancyText.Reverse().ToString(),
                NotSoFancyNumber = input.FancyNumber * -1,
            };
            return output;
        }
    }
}
