using Auction.Domain.Entities;
using Auction.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Auction.Infrastructure.Persistence;

public class AuctionDbContext : DbContext
{
    public AuctionDbContext(DbContextOptions<AuctionDbContext> options) : base(options) { }

    public DbSet<Auction.Domain.Entities.Auction> Auctions => Set<Auction.Domain.Entities.Auction>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Watchlist> Watchlist => Set<Watchlist>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("auction");
        modelBuilder.ApplyConfiguration(new AuctionConfiguration());
        modelBuilder.ApplyConfiguration(new CategoryConfiguration());
        modelBuilder.ApplyConfiguration(new WatchlistConfiguration());
    }
}
