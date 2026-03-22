using Bidding.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bidding.Infrastructure.Persistence;

public class BiddingDbContext : DbContext
{
    public BiddingDbContext(DbContextOptions<BiddingDbContext> options) : base(options) { }

    public DbSet<Bid> Bids => Set<Bid>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("bidding");

        modelBuilder.Entity<Bid>(builder =>
        {
            builder.ToTable("bids");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.AuctionId).IsRequired();
            builder.Property(x => x.BidderId).IsRequired();
            builder.Property(x => x.Amount).HasPrecision(18, 2).IsRequired();
            builder.Property(x => x.PlacedAt).IsRequired();
            // No foreign keys to other schemas - logical references only
            builder.HasIndex(x => x.AuctionId);
        });
    }
}
