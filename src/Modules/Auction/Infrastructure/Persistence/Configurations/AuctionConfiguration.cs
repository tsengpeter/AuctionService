using Auction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auction.Infrastructure.Persistence.Configurations;

public class AuctionConfiguration : IEntityTypeConfiguration<Auction.Domain.Entities.Auction>
{
    public void Configure(EntityTypeBuilder<Auction.Domain.Entities.Auction> builder)
    {
        builder.ToTable("auctions", "auction");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OwnerId).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasColumnType("text");
        builder.Property(x => x.StartingPrice).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.StartTime).IsRequired();
        builder.Property(x => x.EndTime).IsRequired();
        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(x => x.WinnerId);
        builder.Property(x => x.SoldAmount).HasPrecision(18, 2);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.OwnsMany(x => x.Images, img =>
        {
            img.ToTable("auction_images", "auction");
            img.WithOwner().HasForeignKey("AuctionId");
            img.HasKey(x => x.Id);
            img.Property(x => x.Url).HasMaxLength(500).IsRequired();
            img.Property(x => x.DisplayOrder).IsRequired();
        });

        builder.HasIndex(x => new { x.Status, x.EndTime });
        builder.HasIndex(x => x.StartTime);
        builder.HasIndex(x => x.OwnerId);
    }
}
