using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using Motor.Extensions.Hosting.RabbitMQ.Options;
using RabbitMQ.Client;

namespace Motor.Extensions.Hosting.RabbitMQ;

public interface IRabbitMQConnectionFactory<T> : IDisposable
{
    /// <summary>
    /// Username to use when authenticating to the server.
    /// </summary>
    string UserName { get; }

    /// <summary>
    /// Password to use when authenticating to the server.
    /// </summary>
    string Password { get; }

    /// <summary>
    /// Virtual host to access during this connection.
    /// </summary>
    string VirtualHost { get; }

    Task<IConnection> CurrentConnectionAsync();
    Task<IChannel> CurrentChannelAsync();
    Task<IConnection> CreateConnectionAsync(CancellationToken ct);
    Task<IChannel> CreateChannelAsync(CancellationToken ct);
}

public sealed class RabbitMQConnectionFactory<TC> : IRabbitMQConnectionFactory<TC>
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly Lazy<Task<IConnection>> _lazyConnection;
    private readonly Lazy<Task<IChannel>> _lazyChannel;

    public RabbitMQConnectionFactory(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
        _lazyConnection = new Lazy<Task<IConnection>>(
            async () => await _connectionFactory.CreateConnectionAsync(),
            LazyThreadSafetyMode.ExecutionAndPublication
        );
        _lazyChannel = new Lazy<Task<IChannel>>(
            async () => await (await CurrentConnectionAsync()).CreateChannelAsync(),
            LazyThreadSafetyMode.ExecutionAndPublication
        );
    }

    public static IRabbitMQConnectionFactory<TC> From<TOptions>(TOptions options)
        where TOptions : RabbitMQBaseOptions
    {
        Validator.ValidateObject(options, new ValidationContext(options), true);
        return new RabbitMQConnectionFactory<TC>(FromConfig(options));
    }

    private static IConnectionFactory FromConfig(RabbitMQBaseOptions baseOptions)
    {
        return new ConnectionFactory
        {
            HostName = baseOptions.Host,
            Port = baseOptions.Port,
            VirtualHost = baseOptions.VirtualHost,
            UserName = baseOptions.User,
            Password = baseOptions.Password,
            RequestedHeartbeat = baseOptions.RequestedHeartbeat,
            Ssl = new SslOption
            {
                Enabled = baseOptions.Tls.Enabled,
                ServerName = baseOptions.Host,
                Version = baseOptions.Tls.Enabled ? baseOptions.Tls.Protocols : SslProtocols.None,
                AcceptablePolicyErrors = baseOptions.Tls.AcceptablePolicyErrors,
                CertificateValidationCallback = baseOptions.Tls.CertificateValidationCallback,
            },
        };
    }

    public string UserName => _connectionFactory.UserName;
    public string Password => _connectionFactory.Password;
    public string VirtualHost => _connectionFactory.VirtualHost;

    public async Task<IConnection> CurrentConnectionAsync() => await _lazyConnection.Value;

    public async Task<IChannel> CurrentChannelAsync() => await _lazyChannel.Value;

    public async Task<IConnection> CreateConnectionAsync(CancellationToken ct) =>
        await _connectionFactory.CreateConnectionAsync(ct);

    public async Task<IChannel> CreateChannelAsync(CancellationToken ct) =>
        await (await CurrentConnectionAsync()).CreateChannelAsync(cancellationToken: ct);

    public void Dispose()
    {
        if (_lazyChannel.IsValueCreated)
        {
            _lazyChannel.Value.Dispose();
        }

        if (_lazyConnection.IsValueCreated)
        {
            _lazyConnection.Value.Dispose();
        }
    }
}
