using System;
using Motor.Extensions.Hosting.RabbitMQ;
using Moq;
using RabbitMQ.Client;
using Xunit;

namespace Motor.Extensions.Hosting.RabbitMQ_UnitTest
{
    public class RabbitMQConnectionHandlerTests
    {
        [Fact]
        public void EstablishConnection_WithoutConnectionFactory_Throws()
        {
            var connectionHandler = GetConnectionHandler(null);

            Assert.Throws<InvalidOperationException>(() => connectionHandler.EstablishConnection());
        }
        
        [Fact]
        public void EstablishConnection_WithConnectionFactory_ConnectionEstablished()
        {
            var mock = new Mock<IConnectionFactory>();
            var connectionHandler = GetConnectionHandler(mock.Object);

            connectionHandler.EstablishConnection();

            mock.Verify(x => x.CreateConnection(), Times.Exactly(1));
        }

        [Fact]
        public void EstablishConnection_ConnectionFactoryThrowsSomeException_Throw()
        {
            var mock = new Mock<IConnectionFactory>();
            var expected = new Exception("someException");
            mock.Setup(x => x.CreateConnection()).Throws(expected);
            var connectionHandler = new TestConnectionHander();
            connectionHandler.SetConnectionFactory(mock.Object);

            var actual = Assert.Throws<Exception>(() => connectionHandler.EstablishConnection());

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void EstablishChannel_WithConnectionFactoryWithoutEstablishedConnection_ThrowsInavlidOperationException()
        {
            var conFactoryMock = new Mock<IConnectionFactory>();
            conFactoryMock.Setup(x => x.CreateConnection()).Returns(new Mock<IConnection>().Object);
            var connectionHandler = GetConnectionHandler(conFactoryMock.Object);

            Assert.Throws<InvalidOperationException>(() => connectionHandler.EstablishChannel());
        }
        
        [Fact]
        public void EstablishChannel_WithConnectionFactoryAndEstablishedConnection_ChannelEstablished()
        {
            var conFactoryMock = new Mock<IConnectionFactory>();
            var connectionMock = new Mock<IConnection>();
            conFactoryMock.Setup(x => x.CreateConnection()).Returns(connectionMock.Object);
            var connectionHandler = GetConnectionHandler(conFactoryMock.Object);
            connectionHandler.EstablishConnection();
            
            connectionHandler.EstablishChannel();

            connectionMock.Verify(x => x.CreateModel(), Times.Exactly(1));
        }

        [Fact]
        public void EstablishChannel_WithConnectionThrowsOnCreateModel_Throw()
        {
            var conFactoryMock = new Mock<IConnectionFactory>();
            var connectionMock = new Mock<IConnection>();
            conFactoryMock.Setup(x => x.CreateConnection()).Returns(connectionMock.Object);
            var expected = new Exception("someException");
            connectionMock.Setup(x => x.CreateModel()).Throws(expected);
            var connectionHandler = GetConnectionHandler(conFactoryMock.Object);
            connectionHandler.EstablishConnection();

            var actual = Assert.Throws<Exception>(() => connectionHandler.EstablishChannel());

            Assert.Equal(expected, actual);
        }

        private TestConnectionHander GetConnectionHandler(IConnectionFactory conFactory)
        {
            var conHandler = new TestConnectionHander();
            conHandler.SetConnectionFactory(conFactory);
            return conHandler;
        }
        
        private class TestConnectionHander : RabbitMQConnectionHandler
        {
            public new void EstablishConnection()
            {
                base.EstablishConnection();
            }

            public new void EstablishChannel()
            {
                base.EstablishChannel();
            }

            public void SetConnectionFactory(IConnectionFactory conFactory)
            {
                ConnectionFactory = conFactory;
            }
        }
    }
}