using MemberService.Domain.Entities;
using MemberService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MemberService.Infrastructure.Persistence.Configurations;

/// <summary>
/// User entity configuration
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Value converters for value objects
        var emailConverter = new ValueConverter<Email, string>(
            v => v.Value,
            v => Email.Create(v).Value);

        var usernameConverter = new ValueConverter<Username, string>(
            v => v.Value,
            v => Username.Create(v).Value);

        builder.Property(u => u.Email)
            .HasConversion(emailConverter)
            .HasMaxLength(254)
            .IsRequired();

        builder.Property(u => u.Username)
            .HasConversion(usernameConverter)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(u => u.Username)
            .IsUnique();

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.Id)
            .ValueGeneratedNever(); // Since we use Snowflake ID

        builder.Property(u => u.PasswordHash)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.Property(u => u.UpdatedAt)
            .IsRequired();

        // One-to-many relationship with RefreshToken
        builder.HasMany(u => u.RefreshTokens)
            .WithOne(rt => rt.User)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}