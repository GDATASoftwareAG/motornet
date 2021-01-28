using Amazon.SQS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.SQS.Options;

namespace Motor.Extensions.Hosting.SQS
{
    public static class SQSHostBuilderExtensions
    {
        public static void AddSQSWithConfig<T>(this IConsumerBuilder<T> builder, IConfiguration config)
            where T : notnull
        {
            builder.Configure<SQSConsumerOptions<T>>(config);
            builder.AddConsumer<SQSConsumer<T>>();
        }

        public static void AddSQS<T>(this IConsumerBuilder<T> builder, string configSection = "SQSConsumer")
            where T : notnull
        {
            builder.AddSQSWithConfig(builder.Context.Configuration.GetSection(configSection));
        }
    }
}
