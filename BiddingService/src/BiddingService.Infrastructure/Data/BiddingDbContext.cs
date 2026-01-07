using BiddingService.Core.Entities;
using BiddingService.Core.Interfaces;
using BiddingService.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace BiddingService.Infrastructure.Data;

public class BiddingDbContext : DbContext
{
    private readonly IEncryptionService _encryptionService;

    public BiddingDbContext(DbContextOptions<BiddingDbContext> options, IEncryptionService encryptionService) : base(options)
    {
        _encryptionService = encryptionService;
    }

    public DbSet<Bid> Bids { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new BidConfiguration(_encryptionService));
        base.OnModelCreating(modelBuilder);
    }
}
