using System.Threading.Tasks;
using Xunit;

namespace Motor.Extensions.Hosting.PgMq_IntegrationTest;

[CollectionDefinition("PgMqMessage")]
public class PgMqMessageCollection : ICollectionFixture<PostgresFixture> { }

public class PostgresFixture : IAsyncLifetime
{
    private readonly PostgresContainer _container = new PostgresBuilder().Build();
    public string ConnectionString =>
        $"Host={_container.Hostname};Port={_container.GetMappedPublicPort(PostgresBuilder.DefaultPort)};Username=postgres;Password=postgres;Database=postgres";

    public Task InitializeAsync()
    {
        return _container.StartAsync();
    }

    public Task DisposeAsync()
    {
        return _container.DisposeAsync().AsTask();
    }
}
