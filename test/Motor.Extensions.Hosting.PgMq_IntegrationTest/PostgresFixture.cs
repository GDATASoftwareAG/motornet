using System.Threading.Tasks;
using Testcontainers.PostgreSql;
using Xunit;

namespace Motor.Extensions.Hosting.PgMq_IntegrationTest;

[CollectionDefinition("PgMqMessage")]
public class PgMqMessageCollection : ICollectionFixture<PostgresFixture> { }

public class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("quay.io/tembo/pg16-pgmq:latest").Build();

    // Get ConnectionString from TestContainer.
    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
