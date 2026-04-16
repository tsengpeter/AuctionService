using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Auction.Infrastructure.Persistence;

public class AuctionDbContextFactory : IDesignTimeDbContextFactory<AuctionDbContext>
{
    public AuctionDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AuctionDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=auctiondev;Username=postgres;Password=postgres",
            o => o.MigrationsHistoryTable("__EFMigrationsHistory", "auction"));

        return new AuctionDbContext(optionsBuilder.Options);
    }
}
