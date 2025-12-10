using AuctionService.Core.Interfaces;
using AuctionService.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace AuctionService.Api.Controllers;

/// <summary>
/// 健康檢查控制器
/// </summary>
[Route("api/health")]
[ApiController]
public class HealthController : ControllerBase
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 初始化健康檢查控制器
    /// </summary>
    public HealthController(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// 健康檢查端點 - 檢查資料庫連線和 BiddingService 可用性
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var healthStatus = new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "AuctionService",
            checks = new List<object>()
        };

        // 檢查資料庫連線
        var dbCheck = await CheckDatabaseConnection();
        healthStatus.checks.Add(dbCheck);

        // 檢查 BiddingService 可用性
        var biddingServiceCheck = await CheckBiddingServiceAvailability();
        healthStatus.checks.Add(biddingServiceCheck);

        // 如果任何檢查失敗，返回服務不可用
        var failedChecks = healthStatus.checks
            .Where(c => ((dynamic)c).status != "healthy")
            .ToList();

        if (failedChecks.Any())
        {
            healthStatus = healthStatus with { status = "unhealthy" };
            return StatusCode(503, healthStatus);
        }

        return Ok(healthStatus);
    }

    private async Task<object> CheckDatabaseConnection()
    {
        try
        {
            // 嘗試執行簡單的資料庫查詢來驗證連線
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();

            // 執行簡單查詢來測試連線
            var canConnect = await dbContext.Database.CanConnectAsync();

            return new
            {
                name = "database",
                status = canConnect ? "healthy" : "unhealthy",
                timestamp = DateTime.UtcNow,
                details = canConnect ? "Database connection successful" : "Database connection failed"
            };
        }
        catch (Exception ex)
        {
            return new
            {
                name = "database",
                status = "unhealthy",
                timestamp = DateTime.UtcNow,
                details = $"Database check failed: {ex.Message}"
            };
        }
    }

    private async Task<object> CheckBiddingServiceAvailability()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var biddingServiceClient = scope.ServiceProvider.GetRequiredService<IBiddingServiceClient>();

            // 嘗試呼叫 BiddingService 的健康檢查端點或簡單查詢
            // 這裡使用 GetCurrentBidAsync 作為可用性測試，因為它是一個輕量級的呼叫
            var testAuctionId = Guid.NewGuid(); // 使用不存在的 ID 來測試服務可用性
            var result = await biddingServiceClient.GetCurrentBidAsync(testAuctionId);

            // 如果服務可用但返回預期的錯誤（拍賣不存在），則視為健康
            return new
            {
                name = "bidding-service",
                status = "healthy",
                timestamp = DateTime.UtcNow,
                details = "BiddingService is available"
            };
        }
        catch (Exception ex)
        {
            // 如果是網路錯誤或服務不可用，視為不健康
            return new
            {
                name = "bidding-service",
                status = "unhealthy",
                timestamp = DateTime.UtcNow,
                details = $"BiddingService check failed: {ex.Message}"
            };
        }
    }
}