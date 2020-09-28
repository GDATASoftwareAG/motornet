using System;
using RabbitMQ.Client;

namespace Motor.Extensions.Hosting.RabbitMQ
{
    public abstract class RabbitMQConnectionHandler : IDisposable
    {
        protected IConnectionFactory? ConnectionFactory { get; set; }
        private IConnection? Connection { get; set; }
        protected IModel? Channel { get; private set; }
        private bool alreadyDisposed;

        protected void EstablishConnection()
        {
            if (ConnectionFactory == null)
            {
                throw new InvalidOperationException("ConnectionFactory is not set.");
            }
            Connection = ConnectionFactory.CreateConnection();
        }

        protected void EstablishChannel()
        { 
            if (Connection == null)
            {
                throw new InvalidOperationException("Connection is not established.");
            }
            Channel = Connection.CreateModel();
        }
        
        public void Dispose()
        {
            Dispose(!alreadyDisposed);
            alreadyDisposed = true;
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;
            Channel?.Dispose();
            Connection?.Dispose();
        }
    }
}
