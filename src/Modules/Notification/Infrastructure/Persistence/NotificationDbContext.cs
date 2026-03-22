using Notification.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Notification.Infrastructure.Persistence;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options) { }

    public DbSet<NotificationRecord> Notifications => Set<NotificationRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("notification");

        modelBuilder.Entity<NotificationRecord>(builder =>
        {
            builder.ToTable("notifications");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.RecipientId).IsRequired();
            builder.Property(x => x.Type).HasMaxLength(50).IsRequired();
            builder.Property(x => x.Payload).IsRequired();
            builder.HasIndex(x => x.RecipientId);
        });
    }
}
