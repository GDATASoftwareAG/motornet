using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.SQS.Options;

namespace Motor.Extensions.Hosting.SQS
{
    public static class SQSHostBuilderExtensions
    {
        public static void AddSQSWithConfig<T>(this IConsumerBuilder<T> builder,
            IConfiguration clientConfiguration)
            where T : notnull
        {
            builder.Configure<SQSClientOptions>(clientConfiguration);
            builder.AddConsumer<SQSConsumer<T>>();
            builder.AddTransient<ISQSClientFactory, SQSClientFactory>();
        }

        public static void AddSQS<T>(this IConsumerBuilder<T> builder, string clientConfigSection = "SQSConsumer")
            where T : notnull
        {
            builder.AddSQSWithConfig(builder.Context.Configuration.GetSection(clientConfigSection));
        }
    }
}
