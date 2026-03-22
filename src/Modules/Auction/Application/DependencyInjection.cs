using Auction.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Auction;

public static class AuctionDependencyInjection
{
    public static IServiceCollection AddAuctionModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("AuctionConnection")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string for Auction module is not configured.");

        services.AddDbContext<AuctionDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(AuctionDependencyInjection).Assembly));

        return services;
    }
}
