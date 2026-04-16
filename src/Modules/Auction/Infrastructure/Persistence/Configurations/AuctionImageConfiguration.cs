using Auction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auction.Infrastructure.Persistence.Configurations;

public class AuctionImageConfiguration : IEntityTypeConfiguration<AuctionImage>
{
    public void Configure(EntityTypeBuilder<AuctionImage> builder)
    {
        // Owned entity — primary configuration handled in AuctionConfiguration.OwnsMany
        // This class is reserved for any future standalone configuration overrides
    }
}
