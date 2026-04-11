using Member.Domain;
using Member.Domain.Entities;
using Member.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Member.Infrastructure.Persistence;

public class MemberDbContext : DbContext
{
    public MemberDbContext(DbContextOptions<MemberDbContext> options) : base(options) { }

    public DbSet<MemberUser> Users => Set<MemberUser>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PhoneCountryCode> PhoneCountryCodes => Set<PhoneCountryCode>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new PhoneCountryCodeConfiguration());
        modelBuilder.ApplyConfiguration(new MemberUserConfiguration());
        modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());
    }
}
