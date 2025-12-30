using Testcontainers.PostgreSql;

namespace MemberService.IntegrationTests.TestFixtures;

public class PostgreSqlContainerFixture : IDisposable
{
    private readonly PostgreSqlContainer _container;

    public PostgreSqlContainerFixture()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16")
            .WithDatabase("testdb")
            .WithUsername("postgres")
            .WithPassword("password")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public string GetConnectionString()
    {
        return _container.GetConnectionString();
    }

    public void Dispose()
    {
        _container.DisposeAsync().AsTask().Wait();
    }
}