using MemberService.Domain.Entities;
using MemberService.Domain.Interfaces;
using MemberService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace MemberService.IntegrationTests.TestHelpers;

public class TestDatabaseHelper
{
    private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private static PostgreSqlContainer? _sharedContainer;
    private static bool _containerStarted = false;
    private static readonly object _lock = new object();

    public static async Task EnsureDatabaseStartedAsync()
    {
        if (_containerStarted)
            return;

        await _semaphore.WaitAsync();
        try
        {
            if (!_containerStarted)
            {
                _sharedContainer = new PostgreSqlBuilder()
                    .WithImage("postgres:16")
                    .WithDatabase("testdb")
                    .WithUsername("postgres")
                    .WithPassword("password")
                    .Build();

                await _sharedContainer.StartAsync();
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
        if (_sharedContainer == null)
        {
            throw new InvalidOperationException("Database container not initialized. Call EnsureDatabaseStartedAsync first.");
        }
        
        var connString = _sharedContainer.GetConnectionString();
        // Replace the database name in the connection string
        return connString.Replace("Database=testdb", $"Database={databaseName}");
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
            if (_sharedContainer != null)
            {
                await _sharedContainer.DisposeAsync();
                _sharedContainer = null;
                _containerStarted = false;
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }
}