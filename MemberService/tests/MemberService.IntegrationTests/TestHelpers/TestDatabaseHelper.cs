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
    private static readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .WithDatabase("testdb")
        .WithUsername("postgres")
        .WithPassword("password")
        .Build();

    private static bool _containerStarted = false;

    public static async Task EnsureDatabaseStartedAsync()
    {
        if (!_containerStarted)
        {
            await _postgresContainer.StartAsync();
            _containerStarted = true;
        }
    }

    public static string GetConnectionString()
    {
        return _postgresContainer.GetConnectionString();
    }

    public static void ConfigureTestDatabase(IServiceCollection services)
    {
        // Remove the existing DbContext registration
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<MemberDbContext>));
        if (descriptor != null)
        {
            services.Remove(descriptor);
        }

        // Add test database
        services.AddDbContext<MemberDbContext>(options =>
            options.UseNpgsql(GetConnectionString()));
    }

    public static async Task ResetDatabaseAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MemberDbContext>();
        
        // Ensure database is deleted and recreated
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
        
        // Also run migrations if needed
        // await dbContext.Database.MigrateAsync();
    }
}