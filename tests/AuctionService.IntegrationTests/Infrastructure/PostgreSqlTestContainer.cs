using Testcontainers.PostgreSql;
using Xunit;

namespace AuctionService.IntegrationTests.Infrastructure;

public class PostgreSqlTestContainer : IAsyncLifetime
{
    public PostgreSqlContainer Container { get; private set; }

    public string ConnectionString => Container.GetConnectionString();

    public PostgreSqlTestContainer()
    {
        Container = new PostgreSqlBuilder()
            .WithDatabase("auctiontest")
            .WithUsername("testuser")
            .WithPassword("testpass123")
            .WithCleanUp(true)
            .Build();
    }

    public async Task InitializeAsync()
    {
        await Container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await Container.StopAsync();
    }
}