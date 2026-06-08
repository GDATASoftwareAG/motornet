using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.SQS.Options;

namespace Motor.Extensions.Hosting.SQS;

public static class SQSHostBuilderExtensions
{
    extension<T>(IConsumerBuilder<T> builder)
        where T : notnull
    {
        public IConsumerBuilder<T> AddSQSWithConfig(IConfiguration clientConfiguration)
        {
            builder.Configure<SQSClientOptions>(clientConfiguration);
            builder.AddConsumer<SQSConsumer<T>>();
            builder.AddTransient<ISQSClientFactory, SQSClientFactory>();

            return builder;
        }

        public IConsumerBuilder<T> AddSQS(string clientConfigSection = "SQSConsumer") =>
            builder.AddSQSWithConfig(builder.Configuration.GetSection(clientConfigSection));
    }
}
