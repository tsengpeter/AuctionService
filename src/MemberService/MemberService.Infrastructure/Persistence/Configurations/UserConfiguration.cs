using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MemberService.Domain.Entities;

namespace MemberService.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for User entity.
/// Defines table structure, indexes, and relationships.
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        
        // Primary Key
        builder.HasKey(u => u.Id);
        
        // Properties
        builder.Property(u => u.Id)
            .ValueGeneratedNever() // ID comes from Snowflake generator
            .IsRequired();
        
        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(255);
        
        builder.Property(u => u.PasswordHash)
            .IsRequired();
        
        builder.Property(u => u.Username)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(u => u.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone");
        
        builder.Property(u => u.UpdatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone");
        
        // Unique indexes
        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("IX_Users_Email_Unique");
        
        // Relationships
        builder.HasMany(u => u.RefreshTokens)
            .WithOne(rt => rt.User)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);
        
        // Convert value objects to strings
        builder.Property(u => u.Email)
            .HasConversion(
                email => email.Value,
                value => Domain.ValueObjects.Email.Create(value)
            );
        
        builder.Property(u => u.Username)
            .HasConversion(
                username => username.Value,
                value => Domain.ValueObjects.Username.Create(value)
            );
    }
}
