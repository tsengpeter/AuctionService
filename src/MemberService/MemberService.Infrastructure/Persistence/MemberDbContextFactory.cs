using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MemberService.Infrastructure.Persistence;

/// <summary>
/// Factory for creating DbContext instances at design time for EF Core migrations.
/// </summary>
public class MemberDbContextFactory : IDesignTimeDbContextFactory<MemberDbContext>
{
    public MemberDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MemberDbContext>();

        // Use the development connection string
        var connectionString = "Host=localhost;Port=5432;Database=memberservice_dev;Username=memberservice;Password=Dev@Password123";

        optionsBuilder.UseNpgsql(connectionString,
            npgsqlOptions => npgsqlOptions.CommandTimeout(30));

        return new MemberDbContext(optionsBuilder.Options);
    }
}
