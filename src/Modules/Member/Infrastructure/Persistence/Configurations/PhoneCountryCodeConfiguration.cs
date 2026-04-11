using Member.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Member.Infrastructure.Persistence.Configurations;

public class PhoneCountryCodeConfiguration : IEntityTypeConfiguration<PhoneCountryCode>
{
    public void Configure(EntityTypeBuilder<PhoneCountryCode> builder)
    {
        builder.ToTable("phone_country_codes", "member");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedOnAdd();
        builder.Property(p => p.DialCode).IsRequired().HasMaxLength(10);
        builder.HasIndex(p => p.DialCode).IsUnique();
        builder.Property(p => p.CountryName).IsRequired().HasMaxLength(100);
        builder.Property(p => p.CountryIso).IsRequired().HasMaxLength(2);

        builder.HasData(
            new { Id = 1, DialCode = "886", CountryName = "Taiwan", CountryIso = "TW" },
            new { Id = 2, DialCode = "1", CountryName = "United States", CountryIso = "US" },
            new { Id = 3, DialCode = "81", CountryName = "Japan", CountryIso = "JP" },
            new { Id = 4, DialCode = "44", CountryName = "United Kingdom", CountryIso = "GB" },
            new { Id = 5, DialCode = "61", CountryName = "Australia", CountryIso = "AU" },
            new { Id = 6, DialCode = "49", CountryName = "Germany", CountryIso = "DE" }
        );
    }
}
