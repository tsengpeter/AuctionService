using Bidding.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bidding;

public static class BiddingDependencyInjection
{
    public static IServiceCollection AddBiddingModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("BiddingConnection")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string for Bidding module is not configured.");

        services.AddDbContext<BiddingDbContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }
}
