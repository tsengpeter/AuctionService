using Ordering.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ordering.Infrastructure.Persistence;

public class OrderingDbContext : DbContext
{
    public OrderingDbContext(DbContextOptions<OrderingDbContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("ordering");

        modelBuilder.Entity<Order>(builder =>
        {
            builder.ToTable("orders");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.AuctionId).IsRequired();
            builder.Property(x => x.BuyerId).IsRequired();
            builder.Property(x => x.Amount).HasPrecision(18, 2).IsRequired();
            builder.Property(x => x.Status).HasConversion<string>().IsRequired();
            // No foreign keys to other schemas - logical references only
            builder.HasIndex(x => x.AuctionId);
        });
    }
}
