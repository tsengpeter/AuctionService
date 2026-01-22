using Testcontainers.PostgreSql;

namespace MemberService.IntegrationTests.TestFixtures;

public class PostgreSqlContainerFixture : IDisposable
{
    private readonly PostgreSqlContainer _container;
    
    // 測試環境使用 Alpine 版本以加快測試速度
    // 生產環境：postgres:16 (一般版本)
    // 測試環境：postgres:16-alpine
    private static readonly string PostgresImage = 
        Environment.GetEnvironmentVariable("TEST_POSTGRES_IMAGE") ?? "postgres:16-alpine";

    public PostgreSqlContainerFixture()
    {
        _container = new PostgreSqlBuilder()
            .WithImage(PostgresImage)
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