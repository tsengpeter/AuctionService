using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notification.Infrastructure.Persistence;

namespace Notification;

public static class NotificationDependencyInjection
{
    public static IServiceCollection AddNotificationModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("NotificationConnection")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string for Notification module is not configured.");

        services.AddDbContext<NotificationDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(NotificationDependencyInjection).Assembly));

        return services;
    }
}
