using FluentValidation;
using MediatR;
using Member.Application.Abstractions;
using Member.Infrastructure.BackgroundServices;
using Member.Infrastructure.Persistence;
using Member.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Member;

public static class MemberDependencyInjection
{
    public static IServiceCollection AddMemberModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MemberConnection")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string for Member module is not configured.");

        services.AddDbContext<MemberDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(MemberDependencyInjection).Assembly));

        services.AddValidatorsFromAssembly(typeof(MemberDependencyInjection).Assembly);

        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddHostedService<RefreshTokenCleanupService>();

        return services;
    }
}
