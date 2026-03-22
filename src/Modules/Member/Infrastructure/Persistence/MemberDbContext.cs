using Member.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Member.Infrastructure.Persistence;

public class MemberDbContext : DbContext
{
    public MemberDbContext(DbContextOptions<MemberDbContext> options) : base(options) { }

    public DbSet<MemberUser> Users => Set<MemberUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("member");

        modelBuilder.Entity<MemberUser>(builder =>
        {
            builder.ToTable("users");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Email).HasMaxLength(256).IsRequired();
            builder.Property(x => x.Username).HasMaxLength(50).IsRequired();
            builder.Property(x => x.PasswordHash).IsRequired();
            builder.HasIndex(x => x.Email).IsUnique();
        });
    }
}
