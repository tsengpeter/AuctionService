using MemberService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MemberService.Infrastructure.Persistence;

/// <summary>
/// Member service database context
/// </summary>
public class MemberDbContext : DbContext
{
    public MemberDbContext(DbContextOptions<MemberDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MemberDbContext).Assembly);
    }
}