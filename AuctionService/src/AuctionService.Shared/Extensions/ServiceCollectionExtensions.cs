using AuctionService.Core.Interfaces;
using AuctionService.Infrastructure.Data;
using AuctionService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuctionService.Shared.Extensions;

/// <summary>
/// 服務註冊擴充方法
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 註冊資料庫服務
    /// </summary>
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 註冊 DbContext
        services.AddDbContext<AuctionDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // 註冊 Repositories
        services.AddScoped<IAuctionRepository, AuctionRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IFollowRepository, FollowRepository>();
        services.AddScoped<IResponseCodeRepository, ResponseCodeRepository>();

        return services;
    }

    /// <summary>
    /// 註冊應用程式服務
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // 在這裡註冊應用程式服務
        return services;
    }
}