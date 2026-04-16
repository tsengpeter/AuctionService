using Auction.Infrastructure.Persistence;
using Bidding.Infrastructure.Persistence;
using Member.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Notification.Infrastructure.Persistence;
using Ordering.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace AuctionService.IntegrationTests.Infrastructure;

public class IntegrationTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("auctiontest")
        .WithUsername("testuser")
        .WithPassword("testpassword")
        .Build();

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;
    public HttpClient Client { get; private set; } = null!;
    public string ConnectionString { get; private set; } = string.Empty;

    // Well-known category IDs for use in integration tests
    public static readonly Guid CategoryElectronicsId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public static readonly Guid CategoryFashionId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    public static readonly Guid CategoryHomeId = Guid.Parse("10000000-0000-0000-0000-000000000003");
    public static readonly Guid CategorySportsId = Guid.Parse("10000000-0000-0000-0000-000000000004");
    public static readonly Guid CategoryBooksId = Guid.Parse("10000000-0000-0000-0000-000000000005");

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        ConnectionString = _dbContainer.GetConnectionString();

        // Environment variables must be set BEFORE WebApplicationFactory is created.
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", ConnectionString);
        Environment.SetEnvironmentVariable("JWT_SECRET", "test_jwt_secret_at_least_32_chars_long");
        Environment.SetEnvironmentVariable("JWT_ISSUER", "AuctionService");
        Environment.SetEnvironmentVariable("JWT_AUDIENCE", "AuctionService");

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    ReplaceDbContext<MemberDbContext>(services, ConnectionString);
                    ReplaceDbContext<AuctionDbContext>(services, ConnectionString);
                    ReplaceDbContext<BiddingDbContext>(services, ConnectionString);
                    ReplaceDbContext<OrderingDbContext>(services, ConnectionString);
                    ReplaceDbContext<NotificationDbContext>(services, ConnectionString);
                });
            });

        // Apply migrations for all modules
        using var scope = Factory.Services.CreateScope();
        await ApplyMigrations<MemberDbContext>(scope);
        await ApplyMigrations<AuctionDbContext>(scope);
        await ApplyMigrations<BiddingDbContext>(scope);
        await ApplyMigrations<OrderingDbContext>(scope);
        await ApplyMigrations<NotificationDbContext>(scope);

        // Seed categories for Auction integration tests
        await SeedCategoriesAsync(scope);

        Client = Factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", null);
        Environment.SetEnvironmentVariable("JWT_SECRET", null);
        Environment.SetEnvironmentVariable("JWT_ISSUER", null);
        Environment.SetEnvironmentVariable("JWT_AUDIENCE", null);

        Client?.Dispose();
        await Factory.DisposeAsync();
        await _dbContainer.DisposeAsync();
    }

    private static async Task SeedCategoriesAsync(IServiceScope scope)
    {
        var db = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();

        if (await db.Categories.AnyAsync()) return;

        var now = DateTimeOffset.UtcNow;
        await db.Database.ExecuteSqlRawAsync($"""
            INSERT INTO auction.categories ("Id", "Name", "ParentId", "CreatedAt", "UpdatedAt") VALUES
            ('{CategoryElectronicsId}', 'Electronics', null, '{now:O}', '{now:O}'),
            ('{CategoryFashionId}', 'Fashion', null, '{now:O}', '{now:O}'),
            ('{CategoryHomeId}', 'Home & Garden', null, '{now:O}', '{now:O}'),
            ('{CategorySportsId}', 'Sports', null, '{now:O}', '{now:O}'),
            ('{CategoryBooksId}', 'Books', null, '{now:O}', '{now:O}')
            ON CONFLICT DO NOTHING
            """);
    }

    private static void ReplaceDbContext<TContext>(IServiceCollection services, string connectionString)
        where TContext : DbContext
    {
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<TContext>));
        if (descriptor != null) services.Remove(descriptor);

        services.AddDbContext<TContext>(options => options.UseNpgsql(connectionString));
    }

    private static async Task ApplyMigrations<TContext>(IServiceScope scope)
        where TContext : DbContext
    {
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        await context.Database.MigrateAsync();
    }
}

