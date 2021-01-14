using System;
using RabbitMQ.Client;

namespace Motor.Extensions.Hosting.RabbitMQ
{
    public abstract class RabbitMQConnectionHandler : IDisposable
    {
        private bool alreadyDisposed;
        protected IConnectionFactory? ConnectionFactory { get; set; }
        private IConnection? Connection { get; set; }
        protected IModel? Channel { get; private set; }

        public void Dispose()
        {
            Dispose(!alreadyDisposed);
            alreadyDisposed = true;
            GC.SuppressFinalize(this);
        }

        protected void EstablishConnection()
        {
            if (ConnectionFactory is null) throw new InvalidOperationException("ConnectionFactory is not set.");
            Connection = ConnectionFactory.CreateConnection();
        }

        protected void EstablishChannel()
        {
            if (Connection is null) throw new InvalidOperationException("Connection is not established.");
            Channel = Connection.CreateModel();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;
            Channel?.Dispose();
            Connection?.Dispose();
        }
    }
}
