using AuctionService.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuctionService.Infrastructure.Data.Configurations;

/// <summary>
/// Auction 實體配置
/// </summary>
public class AuctionConfiguration : IEntityTypeConfiguration<Auction>
{
    public void Configure(EntityTypeBuilder<Auction> builder)
    {
        builder.ToTable("Auctions");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(a => a.StartingPrice)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(a => a.StartTime)
            .IsRequired();

        builder.Property(a => a.EndTime)
            .IsRequired();

        builder.Property(a => a.UserId)
            .IsRequired();

        builder.Property(a => a.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(a => a.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        // 關聯關係
        builder.HasOne(a => a.Category)
            .WithMany(c => c.Auctions)
            .HasForeignKey(a => a.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // 索引
        builder.HasIndex(a => a.EndTime)
            .HasDatabaseName("IX_Auctions_EndTime");

        builder.HasIndex(a => a.CategoryId)
            .HasDatabaseName("IX_Auctions_CategoryId");

        builder.HasIndex(a => new { a.UserId, a.CreatedAt })
            .HasDatabaseName("IX_Auctions_UserId_CreatedAt");
    }
}