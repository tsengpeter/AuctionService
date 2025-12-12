using AuctionService.Core.DTOs.Common;
using AuctionService.Core.DTOs.Requests;
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
        // 檢查是否使用 In-Memory 資料庫 (開發測試用)
        var useInMemory = configuration.GetValue<bool>("UseInMemoryDatabase");

        if (useInMemory)
        {
            // 使用 In-Memory 資料庫進行開發測試
            services.AddDbContext<AuctionDbContext>(options =>
                options.UseInMemoryDatabase("AuctionDb"));
        }
        else
        {
            // 使用 PostgreSQL 生產資料庫
            services.AddDbContext<AuctionDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        }

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
        services.AddScoped<IFollowService, Core.Services.FollowService>();
        services.AddScoped<IResponseCodeService, ResponseCodeService>();

        // 註冊驗證器
        services.AddScoped<IValidator<AuctionQueryParameters>, AuctionQueryParametersValidator>();
        services.AddScoped<IValidator<CreateAuctionRequest>, CreateAuctionRequestValidator>();
        services.AddScoped<IValidator<UpdateAuctionRequest>, UpdateAuctionRequestValidator>();
        services.AddScoped<IValidator<FollowAuctionRequest>, FollowAuctionRequestValidator>();

        // 配置 BiddingService HttpClient
        services.AddHttpClient<IBiddingServiceClient, BiddingServiceClient>(client =>
        {
            var baseAddress = configuration["BiddingService:BaseUrl"] ?? "http://localhost:5002";
            client.BaseAddress = new Uri(baseAddress);
        })
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());

        // 自訂工廠來注入 BiddingServiceClient 與其依賴
        services.AddScoped<IBiddingServiceClient>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(nameof(IBiddingServiceClient));
            var responseCodeService = sp.GetRequiredService<IResponseCodeService>();
            return new BiddingServiceClient(httpClient, responseCodeService);
        });

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