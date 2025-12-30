using AuctionService.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuctionService.Infrastructure.Data.Configurations;

/// <summary>
/// Follow 實體配置
/// </summary>
public class FollowConfiguration : IEntityTypeConfiguration<Follow>
{
    public void Configure(EntityTypeBuilder<Follow> builder)
    {
        builder.ToTable("Follows");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.UserId)
            .IsRequired();

        builder.Property(f => f.AuctionId)
            .IsRequired();

        builder.Property(f => f.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        // 關聯關係
        builder.HasOne(f => f.Auction)
            .WithMany(a => a.Follows)
            .HasForeignKey(f => f.AuctionId)
            .OnDelete(DeleteBehavior.Cascade);

        // 複合唯一索引 (防止重複追蹤)
        builder.HasIndex(f => new { f.UserId, f.AuctionId })
            .IsUnique()
            .HasDatabaseName("IX_Follows_UserId_AuctionId_Unique");

        // 使用者查詢索引
        builder.HasIndex(f => new { f.UserId, f.CreatedAt })
            .HasDatabaseName("IX_Follows_UserId_CreatedAt");

        // 外鍵索引
        builder.HasIndex(f => f.AuctionId)
            .HasDatabaseName("IX_Follows_AuctionId");
    }
}