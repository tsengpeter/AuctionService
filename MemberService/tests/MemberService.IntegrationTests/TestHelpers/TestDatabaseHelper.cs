using MemberService.Domain.Entities;
using MemberService.Domain.Interfaces;
using MemberService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace MemberService.IntegrationTests.TestHelpers;

public class TestDatabaseHelper
{
    private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private static PostgreSqlContainer? _sharedPostgresContainer;
    private static RedisContainer? _sharedRedisContainer;
    private static bool _containerStarted = false;
    private static readonly object _lock = new object();
    
    // 測試環境使用 Alpine 版本以加快測試速度
    // 生產環境：postgres:16 (一般版本), redis:7-alpine
    // 測試環境：postgres:16-alpine, redis:7-alpine
    private static readonly string PostgresImage = 
        Environment.GetEnvironmentVariable("TEST_POSTGRES_IMAGE") ?? "postgres:16-alpine";
    private static readonly string RedisImage = 
        Environment.GetEnvironmentVariable("TEST_REDIS_IMAGE") ?? "redis:7-alpine";

    public static async Task EnsureDatabaseStartedAsync()
    {
        if (_containerStarted)
            return;

        await _semaphore.WaitAsync();
        try
        {
            if (!_containerStarted)
            {
                // Start PostgreSQL container
                _sharedPostgresContainer = new PostgreSqlBuilder()
                    .WithImage(PostgresImage)
                    .WithDatabase("testdb")
                    .WithUsername("postgres")
                    .WithPassword("password")
                    .Build();

                await _sharedPostgresContainer.StartAsync();

                // Start Redis container
                _sharedRedisContainer = new RedisBuilder()
                    .WithImage(RedisImage)
                    .Build();

                await _sharedRedisContainer.StartAsync();

                _containerStarted = true;
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public static string GetConnectionString(string databaseName)
    {
        if (_sharedPostgresContainer == null)
        {
            throw new InvalidOperationException("Database container not initialized. Call EnsureDatabaseStartedAsync first.");
        }
        
        var connString = _sharedPostgresContainer.GetConnectionString();
        // Replace the database name in the connection string
        return connString.Replace("Database=testdb", $"Database={databaseName}");
    }

    public static string GetRedisConnectionString()
    {
        if (_sharedRedisContainer == null)
        {
            throw new InvalidOperationException("Redis container not initialized. Call EnsureDatabaseStartedAsync first.");
        }
        
        return _sharedRedisContainer.GetConnectionString();
    }

    public static void ConfigureTestDatabase(IServiceCollection services, string databaseName)
    {
        // Remove the existing DbContext registration
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<MemberDbContext>));
        if (descriptor != null)
        {
            services.Remove(descriptor);
        }

        // Add test database
        services.AddDbContext<MemberDbContext>(options =>
            options.UseNpgsql(GetConnectionString(databaseName)));
    }

    public static async Task ResetDatabaseAsync(IServiceProvider serviceProvider, string databaseName)
    {
        await _semaphore.WaitAsync();
        try
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MemberDbContext>();

            // Ensure database is deleted and recreated
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.EnsureCreatedAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public static async Task CleanupAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_sharedPostgresContainer != null)
            {
                await _sharedPostgresContainer.DisposeAsync();
                _sharedPostgresContainer = null;
            }
            
            if (_sharedRedisContainer != null)
            {
                await _sharedRedisContainer.DisposeAsync();
                _sharedRedisContainer = null;
            }
            
            _containerStarted = false;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}