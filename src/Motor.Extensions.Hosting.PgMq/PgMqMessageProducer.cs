using System;
using System.Threading;
using System.Threading.Tasks;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;

namespace Motor.Extensions.Hosting.PgMq;

/// <summary>
/// Message producer for pgmq (https://github.com/pgmq/pgmq)
/// </summary>
/// <remarks>
/// <list>
/// <item>Uses Npgmq (https://github.com/brianpursley/Npgmq)</item>
/// </list>
/// </remarks>
/// <typeparam name="TOutput"></typeparam>
public class PgMqMessageProducer<TOutput> : IRawMessagePublisher<TOutput>
    where TOutput : notnull
{
    /// <summary>
    /// Publishes a MotorCloudEvent to a pgmq queue.
    /// </summary>
    /// <param name="motorCloudEvent"></param>
    /// <param name="token"></param>
    /// <returns>A Task that completes when the motorCloudEvent has been added to the DB.</returns>
    /// <remarks>
    /// <list>
    /// <item>Publishes headers and data of the motorCloudEvents.</item>
    /// </list>
    /// </remarks>
    public Task PublishMessageAsync(MotorCloudEvent<byte[]> motorCloudEvent, CancellationToken token = default)
    {
        throw new System.NotImplementedException();
    }

    /// <summary>
    /// Starts the PgMqMessageProducer
    /// </summary>
    /// <param name="token">A CancellationToken</param>
    /// <returns></returns>
    /// <remarks>
    /// * Creates the NpmqClient
    /// * Ensures the pgmq extension has been created in postgres
    /// * Creates the queue
    /// </remarks>
    public Task StartAsync(CancellationToken token = default)
    {
        // var npgmq = new NpgmqClient("<YOUR CONNECTION STRING HERE>");
        //
        // await npgmq.InitAsync(); // Optional ()
        //
        // await npgmq.CreateQueueAsync("my_queue");
        throw new NotImplementedException();
    }
}
