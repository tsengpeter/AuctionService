using AuctionService.Api;
using AuctionService.Core.Entities;
using AuctionService.Infrastructure.Data;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace AuctionService.IntegrationTests.Fixtures;

/// <summary>
/// PostgreSQL 容器測試基底類別
/// </summary>
public class PostgreSqlContainerFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgreSqlContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("auctionservice_test")
        .WithUsername("testuser")
        .WithPassword("testpass123")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
        .Build();

    public async Task InitializeAsync()
    {
        await _postgreSqlContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgreSqlContainer.StopAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing"); // 設置測試環境以禁用認證

        builder.ConfigureTestServices(services =>
        {
            // 在測試環境中配置允許所有請求的認證
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
            }).AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, TestAuthHandler>("Test", null);

            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                    .RequireAssertion(_ => true) // 允許所有請求
                    .Build();
            });

            // 移除現有的 DbContext 註冊
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AuctionDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // 使用測試資料庫連接字串
            services.AddDbContext<AuctionDbContext>(options =>
            {
                options.UseNpgsql(_postgreSqlContainer.GetConnectionString());
            });

            // 確保資料庫已建立並套用遷移
            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();
            dbContext.Database.Migrate();

            // 插入測試資料
            if (!dbContext.Categories.Any())
            {
                dbContext.Categories.Add(new AuctionService.Core.Entities.Category
                {
                    Name = "Test Category",
                    DisplayOrder = 1,
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) // 使用靜態日期
                });
                dbContext.SaveChanges();
            }

            // 插入測試拍賣資料（由其他用戶創建）
            if (!dbContext.Auctions.Any())
            {
                dbContext.Auctions.AddRange(
                    new AuctionService.Core.Entities.Auction
                    {
                        Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                        Name = "Test Auction 1",
                        Description = "Test auction for following",
                        StartingPrice = 100.00m,
                        CategoryId = 1,
                        StartTime = DateTime.UtcNow.AddMinutes(1),
                        EndTime = DateTime.UtcNow.AddHours(2),
                        UserId = "other-user-1", // 不同的用戶 ID
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new AuctionService.Core.Entities.Auction
                    {
                        Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                        Name = "Test Auction 2",
                        Description = "Another test auction for following",
                        StartingPrice = 150.00m,
                        CategoryId = 1,
                        StartTime = DateTime.UtcNow.AddMinutes(1),
                        EndTime = DateTime.UtcNow.AddHours(2),
                        UserId = "other-user-2", // 不同的用戶 ID
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new AuctionService.Core.Entities.Auction
                    {
                        Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                        Name = "Test Auction 3",
                        Description = "Third test auction for following",
                        StartingPrice = 200.00m,
                        CategoryId = 1,
                        StartTime = DateTime.UtcNow.AddMinutes(1),
                        EndTime = DateTime.UtcNow.AddHours(2),
                        UserId = "other-user-1", // 相同的其他用戶
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                );
                dbContext.SaveChanges();
            }
        });
    }
}

/// <summary>
/// 測試用的認證處理器 - 允許所有請求
/// </summary>
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        Microsoft.Extensions.Options.IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // 創建測試用的 ClaimsPrincipal
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
            new Claim(ClaimTypes.Name, "testuser"),
            new Claim(ClaimTypes.Email, "test@example.com")
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}