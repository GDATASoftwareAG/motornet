using Motor.Extensions.Hosting.Abstractions;

namespace Motor.Extensions.TestUtilities
{
    public static class InMemoryExtensions
    {
        public static void AddInMemory<T>(this IConsumerBuilder<T> builder, InMemoryConsumer<T> consumer) where T : notnull
        {
            builder.AddConsumer<IMessageConsumer<T>>(_ => consumer);
        }

        public static void AddInMemory<T>(this IPublisherBuilder<T> builder, InMemoryPublisher<T> publisher) where T : class
        {
            builder.AddPublisher<InMemoryPublisher<T>>(_ => publisher);
        }
    }
}
