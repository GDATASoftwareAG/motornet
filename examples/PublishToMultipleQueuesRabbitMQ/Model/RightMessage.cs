namespace PublishToMultipleQueuesRabbitMQ.Model;

// Since we send the same data to the queue, we can use a baseclass with the actual
// values, but still need to define an individual type for the queue.
public record RightMessage : OutputMessage
{
    // Or add additional data here.
}
