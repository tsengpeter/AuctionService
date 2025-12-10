using AuctionService.Core.DTOs.Common;
using AuctionService.Core.Interfaces;
using AuctionService.Core.Services;
using AuctionService.Core.Validators;
using AuctionService.Infrastructure.Clients;
using AuctionService.Infrastructure.Data;
using AuctionService.Infrastructure.Repositories;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

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
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 註冊應用程式服務
        services.AddScoped<IAuctionService, Core.Services.AuctionService>();
        services.AddScoped<ICategoryService, Core.Services.CategoryService>();

        // 註冊驗證器
        services.AddScoped<IValidator<AuctionQueryParameters>, AuctionQueryParametersValidator>();

        // 配置 BiddingService HttpClient
        services.AddHttpClient<IBiddingServiceClient, BiddingServiceClient>(client =>
        {
            var baseAddress = configuration["BiddingService:BaseUrl"] ?? "http://localhost:5002";
            client.BaseAddress = new Uri(baseAddress);
        })
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());

        return services;
    }

    /// <summary>
    /// 取得重試原則
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    /// <summary>
    /// 取得斷路器原則
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
    }
}