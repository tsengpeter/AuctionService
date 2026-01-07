using BiddingService.Core.Entities;
using BiddingService.Core.Interfaces;
using BiddingService.Core.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BiddingService.Infrastructure.Data.Configurations;

public class BidConfiguration : IEntityTypeConfiguration<Bid>
{
    private readonly IEncryptionService _encryptionService;

    public BidConfiguration(IEncryptionService encryptionService)
    {
        _encryptionService = encryptionService;
    }

    public void Configure(EntityTypeBuilder<Bid> builder)
    {
        builder.HasKey(b => b.BidId);

        builder.Property(b => b.BidId)
            .ValueGeneratedNever();

        builder.Property(b => b.AuctionId)
            .IsRequired();

        builder.Property(b => b.BidderId)
            .HasMaxLength(255) // Increased length for encrypted value
            .IsRequired()
            .HasConversion(
                v => _encryptionService.Encrypt(v),
                v => _encryptionService.Decrypt(v));

        builder.Property(b => b.BidderIdHash)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(b => b.Amount)
            .HasConversion(
                amount => amount.Value,
                value => new BidAmount(value))
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(b => b.BidAt)
            .IsRequired();

        builder.Property(b => b.SyncedFromRedis)
            .IsRequired();

        builder.HasIndex(b => b.AuctionId);
        // Restored index on Amount for performant sorting and highest bid queries
        builder.HasIndex(b => new { b.AuctionId, b.Amount });
        
        // Spec says: "使用者出價記錄查詢優化（使用 Hash，因 BidderId 加密）".
        builder.HasIndex(b => b.BidderIdHash);
        builder.HasIndex(b => new { b.BidderIdHash, b.BidAt });

        builder.HasIndex(b => b.BidAt);
    }
}
