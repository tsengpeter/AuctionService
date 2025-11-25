using MemberService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace MemberService.IntegrationTests.TestFixtures;

/// <summary>
/// Test fixture for PostgreSQL container setup and teardown for integration tests.
/// Implements IAsyncLifetime for proper async initialization and cleanup.
/// </summary>
public class PostgreSqlContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;
    private MemberDbContext? _context;

    public PostgreSqlContainerFixture()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("member_service_test")
            .WithUsername("testuser")
            .WithPassword("testpassword")
            .WithCleanUp(true)
            .Build();
    }

    /// <summary>
    /// Connection string to the PostgreSQL container
    /// </summary>
    public string ConnectionString => _container.GetConnectionString();

    /// <summary>
    /// Get a configured DbContext for the test database
    /// </summary>
    public MemberDbContext GetDbContext()
    {
        if (_context != null)
            return _context;

        var options = new DbContextOptionsBuilder<MemberDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        _context = new MemberDbContext(options);
        return _context;
    }

    /// <summary>
    /// Initialize the container and apply migrations
    /// </summary>
    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var context = GetDbContext();
        try
        {
            // Apply migrations to the test database
            await context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to apply migrations to test database", ex);
        }
    }

    /// <summary>
    /// Cleanup: dispose context and stop container
    /// </summary>
    public async Task DisposeAsync()
    {
        if (_context != null)
        {
            await _context.DisposeAsync();
        }

        await _container.StopAsync();
        await _container.DisposeAsync();
    }
}

/// <summary>
/// xUnit collection definition for sharing PostgreSQL container across multiple test classes
/// </summary>
[CollectionDefinition("PostgreSQL Container Collection")]
public class PostgreSqlContainerCollection : ICollectionFixture<PostgreSqlContainerFixture>
{
    // This class has no code, just provides collection definition
}
