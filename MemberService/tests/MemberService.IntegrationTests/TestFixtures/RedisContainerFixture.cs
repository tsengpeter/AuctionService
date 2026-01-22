using Testcontainers.Redis;

namespace MemberService.IntegrationTests.TestFixtures;

public class RedisContainerFixture : IDisposable
{
    private readonly RedisContainer _container;
    
    // 測試和生產環境都使用 Alpine 版本
    private static readonly string RedisImage = 
        Environment.GetEnvironmentVariable("TEST_REDIS_IMAGE") ?? "redis:7-alpine";

    public RedisContainerFixture()
    {
        _container = new RedisBuilder()
            .WithImage(RedisImage)
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
