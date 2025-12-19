using BiddingService.Core.Entities;
using BiddingService.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace BiddingService.Infrastructure.Data;

public class BiddingDbContext : DbContext
{
    public BiddingDbContext(DbContextOptions<BiddingDbContext> options) : base(options)
    {
    }

    public DbSet<Bid> Bids { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new BidConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}
