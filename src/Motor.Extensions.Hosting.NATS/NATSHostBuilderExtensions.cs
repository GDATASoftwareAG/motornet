using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.NATS.Options;

namespace Motor.Extensions.Hosting.NATS;

public static class NATSHostBuilderExtensions
{
    extension<TInput>(IConsumerBuilder<TInput> builder)
        where TInput : notnull
    {
        public IConsumerBuilder<TInput> AddNATSWithConfig(IConfiguration clientConfiguration)
        {
            builder.Configure<NATSConsumerOptions>(clientConfiguration);
            builder.AddConsumer<NATSMessageConsumer<TInput>>();
            builder.AddTransient<INATSClientFactory, NATSClientFactory>();

            return builder;
        }

        public IConsumerBuilder<TInput> AddNATS(string clientConfigSection = "NATSConsumer") =>
            builder.AddNATSWithConfig(builder.Configuration.GetSection(clientConfigSection));
    }

    extension<TOutput>(IPublisherBuilder<TOutput> builder)
        where TOutput : notnull
    {
        public IPublisherBuilder<TOutput> AddNATSWithConfig(IConfiguration clientConfiguration)
        {
            builder.Configure<NATSBaseOptions>(clientConfiguration);
            builder.AddPublisher<NATSMessagePublisher<TOutput>>();
            builder.AddTransient<INATSClientFactory, NATSClientFactory>();

            return builder;
        }

        public IPublisherBuilder<TOutput> AddNATS(string clientConfigSection = "NATSPublisher") =>
            builder.AddNATSWithConfig(builder.Configuration.GetSection(clientConfigSection));
    }
}
