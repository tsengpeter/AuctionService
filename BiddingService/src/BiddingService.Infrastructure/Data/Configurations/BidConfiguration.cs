using BiddingService.Core.Entities;
using BiddingService.Core.ValueObjects;
using BiddingService.Infrastructure.Encryption;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BiddingService.Infrastructure.Data.Configurations;

public class BidConfiguration : IEntityTypeConfiguration<Bid>
{
    public void Configure(EntityTypeBuilder<Bid> builder)
    {
        builder.HasKey(b => b.BidId);

        builder.Property(b => b.BidId)
            .ValueGeneratedNever();

        builder.Property(b => b.AuctionId)
            .IsRequired();

        builder.Property(b => b.BidderId)
            .HasMaxLength(100)
            .IsRequired();

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
        builder.HasIndex(b => new { b.AuctionId, b.Amount });
        builder.HasIndex(b => b.BidderId);
        builder.HasIndex(b => b.BidAt);
    }
}
