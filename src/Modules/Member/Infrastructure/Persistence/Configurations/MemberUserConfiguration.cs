using Member.Domain;
using Member.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Member.Infrastructure.Persistence.Configurations;

public class MemberUserConfiguration : IEntityTypeConfiguration<MemberUser>
{
    public void Configure(EntityTypeBuilder<MemberUser> builder)
    {
        builder.ToTable("users", "member");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.Username).IsRequired().HasMaxLength(30);
        builder.Property(u => u.UsernameNormalized).IsRequired().HasMaxLength(30);
        builder.HasIndex(u => u.UsernameNormalized).IsUnique();
        builder.Property(u => u.PasswordHash).IsRequired();
        builder.Property(u => u.DisplayName).HasMaxLength(50);
        builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
        builder.Property(u => u.AddressCountry).HasMaxLength(100);
        builder.Property(u => u.AddressCity).HasMaxLength(100);
        builder.Property(u => u.AddressPostalCode).HasMaxLength(20);
        builder.Property(u => u.AddressLine).HasMaxLength(200);
        builder.Property(u => u.PhoneCountryCodeId).IsRequired();
        builder.HasOne(u => u.CountryCode)
               .WithMany()
               .HasForeignKey(u => u.PhoneCountryCodeId)
               .OnDelete(DeleteBehavior.Restrict);
        builder.Property(u => u.PhoneNumber).IsRequired().HasMaxLength(30);
    }
}
