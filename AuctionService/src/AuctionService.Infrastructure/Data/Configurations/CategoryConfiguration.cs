using AuctionService.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuctionService.Infrastructure.Data.Configurations;

/// <summary>
/// Category 實體配置
/// </summary>
public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.DisplayOrder)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(c => c.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        // 唯一約束
        builder.HasIndex(c => c.Name)
            .IsUnique()
            .HasDatabaseName("IX_Categories_Name_Unique");

        // 種子資料
        builder.HasData(
            new Category { Id = 1, Name = "電子產品", DisplayOrder = 1, IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Category { Id = 2, Name = "家具", DisplayOrder = 2, IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Category { Id = 3, Name = "收藏品", DisplayOrder = 3, IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Category { Id = 4, Name = "藝術品", DisplayOrder = 4, IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Category { Id = 5, Name = "服飾配件", DisplayOrder = 5, IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Category { Id = 6, Name = "書籍", DisplayOrder = 6, IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Category { Id = 7, Name = "運動用品", DisplayOrder = 7, IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Category { Id = 8, Name = "其他", DisplayOrder = 99, IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}