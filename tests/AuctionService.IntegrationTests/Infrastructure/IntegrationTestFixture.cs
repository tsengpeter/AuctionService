using Auction.Infrastructure.Persistence;
using Bidding.Infrastructure.Persistence;
using Member.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        ConnectionString = _dbContainer.GetConnectionString();

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = ConnectionString,
                        ["JWT_SECRET"] = "test_jwt_secret_at_least_32_chars_long",
                        ["JWT_ISSUER"] = "AuctionService",
                        ["JWT_AUDIENCE"] = "AuctionService",
                    });
                });
                builder.ConfigureServices(services =>
                {
                    // Override all DbContext registrations with test container connection
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

        Client = Factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        await Factory.DisposeAsync();
        await _dbContainer.DisposeAsync();
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
