using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using Motor.Extensions.Hosting.RabbitMQ.Options;
using RabbitMQ.Client;

namespace Motor.Extensions.Hosting.RabbitMQ
{
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

        IConnection CurrentConnection { get; }
        IModel CurrentChannel { get; }
        IConnection CreateConnection();
        IModel CreateModel();
    }

    public sealed class RabbitMQConnectionFactory<TC> : IRabbitMQConnectionFactory<TC>
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly Lazy<IConnection> _lazyConnection;
        private readonly Lazy<IModel> _lazyChannel;

        public RabbitMQConnectionFactory(IConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
            _lazyConnection = new Lazy<IConnection>(
                () => _connectionFactory.CreateConnection(),
                LazyThreadSafetyMode.ExecutionAndPublication
            );
            _lazyChannel = new Lazy<IModel>(
                () => CurrentConnection.CreateModel(),
                LazyThreadSafetyMode.ExecutionAndPublication
            );
        }

        public static IRabbitMQConnectionFactory<TC> From<TO>(TO options) where TO : RabbitMQBaseOptions
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
                RequestedHeartbeat = baseOptions.RequestedHeartbeat
            };
        }

        public string UserName => _connectionFactory.UserName;
        public string Password => _connectionFactory.Password;
        public string VirtualHost => _connectionFactory.VirtualHost;

        public IConnection CurrentConnection => _lazyConnection.Value;

        public IModel CurrentChannel => _lazyChannel.Value;

        public IConnection CreateConnection() => _connectionFactory.CreateConnection();

        public IModel CreateModel() => CurrentConnection.CreateModel();

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
}
