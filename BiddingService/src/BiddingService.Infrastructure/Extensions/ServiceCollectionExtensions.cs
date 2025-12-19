using BiddingService.Core.Interfaces;
using BiddingService.Core.Services;
using BiddingService.Infrastructure.BackgroundServices;
using BiddingService.Infrastructure.Data;
using BiddingService.Infrastructure.Encryption;
using BiddingService.Infrastructure.HttpClients;
using BiddingService.Infrastructure.IdGeneration;
using BiddingService.Infrastructure.Redis;
using BiddingService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Prometheus;

namespace BiddingService.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBiddingServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<BiddingDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("BiddingDatabase")));

        // Repositories
        services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IBidRepository, BidRepository>();
        services.AddScoped<IRedisRepository, RedisRepository>();

        // Core services
        services.AddScoped<IBiddingService, BiddingService.Core.Services.BiddingService>();

        // Infrastructure services
        services.AddScoped<IEncryptionService>(sp =>
        {
            var key = configuration.GetValue<string>("Encryption:Key");
            var iv = configuration.GetValue<string>("Encryption:IV");
            return new EncryptionService(key, iv);
        });
        services.AddScoped<ISnowflakeIdGenerator>(sp =>
        {
            var generatorId = configuration.GetValue<int>("IdGen:GeneratorId");
            var datacenterId = configuration.GetValue<int>("IdGen:DatacenterId");
            return new SnowflakeIdGenerator(generatorId, datacenterId);
        });

        // Redis
        services.AddSingleton<IRedisConnection>(sp =>
        {
            var connectionString = configuration.GetValue<string>("Redis:ConnectionString");
            return new RedisConnection(connectionString);
        });

        // HTTP Clients
        services.AddHttpClient<IAuctionServiceClient, AuctionServiceClient>((sp, client) =>
        {
            var baseUrl = configuration.GetValue<string>("AuctionService:BaseUrl");
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(configuration.GetValue<int>("AuctionService:TimeoutSeconds"));
        })
        .AddHttpMessageHandler<CorrelationIdDelegatingHandler>();

        services.AddTransient<CorrelationIdDelegatingHandler>();

        // Caching
        services.AddMemoryCache();

        // Background services
        services.AddHostedService<RedisSyncWorker>();

        return services;
    }
}