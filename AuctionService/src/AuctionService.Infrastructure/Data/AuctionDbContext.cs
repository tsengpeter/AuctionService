using AuctionService.Core.Entities;
using AuctionService.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Infrastructure.Data;

/// <summary>
/// 拍賣服務資料庫上下文
/// </summary>
public class AuctionDbContext : DbContext
{
    /// <summary>
    /// 拍賣商品資料集
    /// </summary>
    public DbSet<Auction> Auctions => Set<Auction>();

    /// <summary>
    /// 商品分類資料集
    /// </summary>
    public DbSet<Category> Categories => Set<Category>();

    /// <summary>
    /// 商品追蹤資料集
    /// </summary>
    public DbSet<Follow> Follows => Set<Follow>();

    /// <summary>
    /// API 回應代碼資料集
    /// </summary>
    public DbSet<ResponseCode> ResponseCodes => Set<ResponseCode>();

    public AuctionDbContext(DbContextOptions<AuctionDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 套用實體配置
        modelBuilder.ApplyConfiguration(new AuctionConfiguration());
        modelBuilder.ApplyConfiguration(new CategoryConfiguration());
        modelBuilder.ApplyConfiguration(new FollowConfiguration());
        modelBuilder.ApplyConfiguration(new ResponseCodeConfiguration());
    }
}