# Data Model: Auction Service

**Branch**: `002-auction-service`  
**Date**: 2025-11-20  
**Purpose**: Entity design, database schema, and EF Core configuration

---

## Overview

Auction Service 的資料模型包含 4 個核心實體：Auction (拍賣商品)、Category (商品分類)、Follow (追蹤關係)、ResponseCode (API 回應碼)。本文件定義實體結構、資料庫 schema、EF Core 配置與初始遷移腳本。

---

## Entities

### 1. Auction (拍賣商品)

商品是拍賣平台的核心實體，包含商品資訊、時間設定、創建者資訊與並發控制。

#### Properties

```csharp
namespace AuctionService.Domain.Entities;

public class Auction
{
    // 主鍵 (Snowflake ID)
    public long Id { get; set; }
    
    // 商品資訊
    public string Title { get; set; } = null!;              // 3-200 字元
    public string Description { get; set; } = null!;        // 10-2000 字元
    public decimal StartingPrice { get; set; }              // 起標價，正數，最大 10,000,000
    
    // 分類 (外鍵)
    public long CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    
    // 時間設定
    public DateTime StartTime { get; set; }                 // 開始時間 (UTC)
    public DateTime EndTime { get; set; }                   // 結束時間 (UTC, 必須 >= StartTime + 1 小時)
    
    // 創建者 (外鍵指向 Member Service 的 User)
    public long CreatorId { get; set; }
    
    // 圖片 URLs (JSON 陣列，最多 5 張)
    public List<string> ImageUrls { get; set; } = new();
    
    // 全文檢索 (PostgreSQL tsvector)
    public string? SearchVector { get; set; }               // 由 trigger 自動更新
    
    // 並發控制 (樂觀鎖)
    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;
    
    // 時間戳記
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // 導航屬性
    public ICollection<Follow> Follows { get; set; } = new List<Follow>();
}
```

#### Validation Rules

- **Title**: 長度 3-200 字元，不可空白
- **Description**: 長度 10-2000 字元，不可空白
- **StartingPrice**: 正數，範圍 0.01 - 10,000,000
- **EndTime**: 必須在 `StartTime` 至少 1 小時之後
- **CategoryId**: 必須存在於 `Categories` 資料表
- **ImageUrls**: 最多 5 個元素，每個 URL 最長 2048 字元

#### Computed Status

商品狀態不儲存在資料庫，而是查詢時計算：

```csharp
public string Status 
{
    get
    {
        var now = DateTime.UtcNow;
        if (now < StartTime) return "Pending";
        if (now < EndTime) return "Active";
        return "Ended";
    }
}
```

---

### 2. Category (商品分類)

預先定義的商品分類選項。

#### Properties

```csharp
namespace AuctionService.Domain.Entities;

public class Category
{
    public long Id { get; set; }                            // 主鍵 (Snowflake ID)
    public string Name { get; set; } = null!;               // 分類名稱，唯一
    public int DisplayOrder { get; set; }                   // 顯示順序
    public bool IsEnabled { get; set; } = true;             // 是否啟用
    
    // 導航屬性
    public ICollection<Auction> Auctions { get; set; } = new List<Auction>();
}
```

#### Initial Data

```csharp
// 預載分類資料
new Category { Id = 1, Name = "電子產品", DisplayOrder = 1 },
new Category { Id = 2, Name = "家具", DisplayOrder = 2 },
new Category { Id = 3, Name = "收藏品", DisplayOrder = 3 },
new Category { Id = 4, Name = "服飾", DisplayOrder = 4 },
new Category { Id = 5, Name = "書籍", DisplayOrder = 5 },
new Category { Id = 6, Name = "其他", DisplayOrder = 99 }
```

---

### 3. Follow (追蹤關係)

使用者追蹤商品的關聯表。

#### Properties

```csharp
namespace AuctionService.Domain.Entities;

public class Follow
{
    public long Id { get; set; }                            // 主鍵 (Snowflake ID)
    
    // 使用者 ID (外鍵指向 Member Service 的 User)
    public long UserId { get; set; }
    
    // 商品 ID (外鍵)
    public long AuctionId { get; set; }
    public Auction Auction { get; set; } = null!;
    
    // 時間戳記
    public DateTime CreatedAt { get; set; }
}
```

#### Constraints

- **Unique Index**: `(UserId, AuctionId)` - 使用者不可重複追蹤同一商品
- **Business Rule**: 使用者不可追蹤自己建立的商品（程式邏輯檢查）

---

### 4. ResponseCode (API 回應碼)

API 回應的狀態代碼與訊息對照表。

#### Properties

```csharp
namespace AuctionService.Domain.Entities;

public class ResponseCode
{
    public string Code { get; set; } = null!;               // 主鍵，例如 "AUCTION_NOT_FOUND"
    public string Name { get; set; } = null!;               // 顯示名稱
    public string MessageZhTw { get; set; } = null!;        // 繁體中文訊息
    public string MessageEn { get; set; } = null!;          // 英文訊息
    public string Category { get; set; } = null!;           // 分類：Success / ClientError / ServerError
    public string Severity { get; set; } = null!;           // 嚴重性：Info / Warning / Error
}
```

#### Initial Data

```csharp
new ResponseCode 
{ 
    Code = "SUCCESS", 
    Name = "Success", 
    MessageZhTw = "操作成功", 
    MessageEn = "Operation successful", 
    Category = "Success", 
    Severity = "Info" 
},
new ResponseCode 
{ 
    Code = "AUCTION_NOT_FOUND", 
    Name = "Auction Not Found", 
    MessageZhTw = "找不到該商品", 
    MessageEn = "Auction not found", 
    Category = "ClientError", 
    Severity = "Error" 
},
new ResponseCode 
{ 
    Code = "AUCTION_CONCURRENT_EDIT", 
    Name = "Concurrent Edit Detected", 
    MessageZhTw = "商品已被其他使用者修改，請重新載入後再試", 
    MessageEn = "Auction was modified by another user. Please reload and try again", 
    Category = "ClientError", 
    Severity = "Warning" 
},
new ResponseCode 
{ 
    Code = "BIDDING_SERVICE_UNAVAILABLE", 
    Name = "Bidding Service Unavailable", 
    MessageZhTw = "出價服務暫時無法連線，顯示的出價資訊可能過時", 
    MessageEn = "Bidding service is temporarily unavailable. Bid information may be outdated", 
    Category = "ServerError", 
    Severity = "Warning" 
},
new ResponseCode 
{ 
    Code = "UNAUTHORIZED", 
    Name = "Unauthorized", 
    MessageZhTw = "需要登入才能執行此操作", 
    MessageEn = "Authentication required", 
    Category = "ClientError", 
    Severity = "Error" 
},
new ResponseCode 
{ 
    Code = "FORBIDDEN", 
    Name = "Forbidden", 
    MessageZhTw = "您沒有權限執行此操作", 
    MessageEn = "You do not have permission to perform this action", 
    Category = "ClientError", 
    Severity = "Error" 
},
new ResponseCode 
{ 
    Code = "INVALID_INPUT", 
    Name = "Invalid Input", 
    MessageZhTw = "輸入的資料不符合要求", 
    MessageEn = "Input data is invalid", 
    Category = "ClientError", 
    Severity = "Error" 
},
new ResponseCode 
{ 
    Code = "AUCTION_ALREADY_HAS_BIDS", 
    Name = "Auction Already Has Bids", 
    MessageZhTw = "已有出價的商品無法編輯或刪除", 
    MessageEn = "Cannot edit or delete auction with existing bids", 
    Category = "ClientError", 
    Severity = "Error" 
},
new ResponseCode 
{ 
    Code = "CANNOT_FOLLOW_OWN_AUCTION", 
    Name = "Cannot Follow Own Auction", 
    MessageZhTw = "無法追蹤自己建立的商品", 
    MessageEn = "Cannot follow your own auction", 
    Category = "ClientError", 
    Severity = "Error" 
},
new ResponseCode 
{ 
    Code = "ALREADY_FOLLOWING", 
    Name = "Already Following", 
    MessageZhTw = "已在追蹤清單中", 
    MessageEn = "Already following this auction", 
    Category = "ClientError", 
    Severity = "Info" 
}
```

---

## EF Core Configuration

### 1. AuctionConfiguration

```csharp
namespace AuctionService.Infrastructure.Persistence.Configurations;

public class AuctionConfiguration : IEntityTypeConfiguration<Auction>
{
    public void Configure(EntityTypeBuilder<Auction> builder)
    {
        // 主鍵
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();  // Snowflake ID 手動生成
        
        // 商品資訊
        builder.Property(a => a.Title)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(a => a.Description)
            .IsRequired()
            .HasMaxLength(2000);
        
        builder.Property(a => a.StartingPrice)
            .IsRequired()
            .HasColumnType("decimal(18,2)");
        
        // 分類外鍵
        builder.HasOne(a => a.Category)
            .WithMany(c => c.Auctions)
            .HasForeignKey(a => a.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // 時間欄位
        builder.Property(a => a.StartTime)
            .IsRequired();
        
        builder.Property(a => a.EndTime)
            .IsRequired();
        
        builder.Property(a => a.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");
        
        builder.Property(a => a.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");
        
        // 圖片 URLs (JSON 陣列)
        builder.Property(a => a.ImageUrls)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>()
            )
            .HasColumnType("jsonb");
        
        // 全文檢索欄位 (PostgreSQL tsvector)
        builder.Property(a => a.SearchVector)
            .HasColumnType("tsvector");
        
        // 並發控制
        builder.Property(a => a.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();
        
        // 索引
        builder.HasIndex(a => a.CategoryId);
        builder.HasIndex(a => a.CreatorId);
        builder.HasIndex(a => a.EndTime).HasDatabaseName("idx_auction_endtime");
        builder.HasIndex(a => a.StartTime).HasDatabaseName("idx_auction_starttime");
        builder.HasIndex(a => a.SearchVector)
            .HasMethod("GIN")
            .HasDatabaseName("idx_auction_search");
    }
}
```

---

### 2. CategoryConfiguration

```csharp
namespace AuctionService.Infrastructure.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();  // Snowflake ID 手動生成
        
        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.HasIndex(c => c.Name).IsUnique();
        builder.HasIndex(c => c.DisplayOrder);
        
        // 預載初始資料
        builder.HasData(
            new Category { Id = 1, Name = "電子產品", DisplayOrder = 1, IsEnabled = true },
            new Category { Id = 2, Name = "家具", DisplayOrder = 2, IsEnabled = true },
            new Category { Id = 3, Name = "收藏品", DisplayOrder = 3, IsEnabled = true },
            new Category { Id = 4, Name = "服飾", DisplayOrder = 4, IsEnabled = true },
            new Category { Id = 5, Name = "書籍", DisplayOrder = 5, IsEnabled = true },
            new Category { Id = 6, Name = "其他", DisplayOrder = 99, IsEnabled = true }
        );
    }
}
```

---

### 3. FollowConfiguration

```csharp
namespace AuctionService.Infrastructure.Persistence.Configurations;

public class FollowConfiguration : IEntityTypeConfiguration<Follow>
{
    public void Configure(EntityTypeBuilder<Follow> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedNever();  // Snowflake ID 手動生成
        
        // 商品外鍵
        builder.HasOne(f => f.Auction)
            .WithMany(a => a.Follows)
            .HasForeignKey(f => f.AuctionId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.Property(f => f.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");
        
        // 唯一索引 (UserId + AuctionId)
        builder.HasIndex(f => new { f.UserId, f.AuctionId })
            .IsUnique()
            .HasDatabaseName("idx_follow_user_auction");
        
        // 索引優化查詢
        builder.HasIndex(f => f.UserId).HasDatabaseName("idx_follow_userid");
        builder.HasIndex(f => f.AuctionId).HasDatabaseName("idx_follow_auctionid");
    }
}
```

---

### 4. ResponseCodeConfiguration

```csharp
namespace AuctionService.Infrastructure.Persistence.Configurations;

public class ResponseCodeConfiguration : IEntityTypeConfiguration<ResponseCode>
{
    public void Configure(EntityTypeBuilder<ResponseCode> builder)
    {
        builder.HasKey(r => r.Code);
        
        builder.Property(r => r.Code).HasMaxLength(50);
        builder.Property(r => r.Name).IsRequired().HasMaxLength(100);
        builder.Property(r => r.MessageZhTw).IsRequired();
        builder.Property(r => r.MessageEn).IsRequired();
        builder.Property(r => r.Category).IsRequired().HasMaxLength(20);
        builder.Property(r => r.Severity).IsRequired().HasMaxLength(20);
        
        // 預載初始資料 (見上方 Initial Data)
        builder.HasData(
            new ResponseCode { Code = "SUCCESS", Name = "Success", MessageZhTw = "操作成功", MessageEn = "Operation successful", Category = "Success", Severity = "Info" },
            new ResponseCode { Code = "AUCTION_NOT_FOUND", Name = "Auction Not Found", MessageZhTw = "找不到該商品", MessageEn = "Auction not found", Category = "ClientError", Severity = "Error" },
            // ... (其他回應碼)
        );
    }
}
```

---

## DbContext

```csharp
namespace AuctionService.Infrastructure.Persistence;

public class AuctionDbContext : DbContext
{
    public AuctionDbContext(DbContextOptions<AuctionDbContext> options) : base(options) { }
    
    public DbSet<Auction> Auctions => Set<Auction>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Follow> Follows => Set<Follow>();
    public DbSet<ResponseCode> ResponseCodes => Set<ResponseCode>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // 套用所有配置
        modelBuilder.ApplyConfiguration(new AuctionConfiguration());
        modelBuilder.ApplyConfiguration(new CategoryConfiguration());
        modelBuilder.ApplyConfiguration(new FollowConfiguration());
        modelBuilder.ApplyConfiguration(new ResponseCodeConfiguration());
    }
    
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // 自動更新 UpdatedAt
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is Auction && e.State == EntityState.Modified);
        
        foreach (var entry in entries)
        {
            ((Auction)entry.Entity).UpdatedAt = DateTime.UtcNow;
        }
        
        return await base.SaveChangesAsync(cancellationToken);
    }
}
```

---

## Initial Migration: `InitialCreate`

### Migration Script

```sql
-- ============================================
-- Migration: InitialCreate
-- Description: Create initial schema for Auction Service
-- ============================================

-- Categories Table
CREATE TABLE "Categories" (
    "Id" BIGINT PRIMARY KEY,
    "Name" VARCHAR(50) NOT NULL,
    "DisplayOrder" INT NOT NULL,
    "IsEnabled" BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE UNIQUE INDEX "idx_category_name" ON "Categories" ("Name");
CREATE INDEX "idx_category_displayorder" ON "Categories" ("DisplayOrder");

-- Insert initial categories
INSERT INTO "Categories" ("Id", "Name", "DisplayOrder", "IsEnabled") VALUES
(1, '電子產品', 1, TRUE),
(2, '家具', 2, TRUE),
(3, '收藏品', 3, TRUE),
(4, '服飾', 4, TRUE),
(5, '書籍', 5, TRUE),
(6, '其他', 99, TRUE);

-- Auctions Table
CREATE TABLE "Auctions" (
    "Id" BIGINT PRIMARY KEY,
    "Title" VARCHAR(200) NOT NULL,
    "Description" VARCHAR(2000) NOT NULL,
    "StartingPrice" NUMERIC(18,2) NOT NULL,
    "CategoryId" BIGINT NOT NULL,
    "StartTime" TIMESTAMPTZ NOT NULL,
    "EndTime" TIMESTAMPTZ NOT NULL,
    "CreatorId" BIGINT NOT NULL,
    "ImageUrls" JSONB NOT NULL DEFAULT '[]',
    "SearchVector" tsvector,
    "RowVersion" BYTEA NOT NULL DEFAULT '\x0000000000000000',
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT "FK_Auctions_Categories" FOREIGN KEY ("CategoryId") REFERENCES "Categories" ("Id") ON DELETE RESTRICT
);

CREATE INDEX "idx_auction_categoryid" ON "Auctions" ("CategoryId");
CREATE INDEX "idx_auction_creatorid" ON "Auctions" ("CreatorId");
CREATE INDEX "idx_auction_endtime" ON "Auctions" ("EndTime");
CREATE INDEX "idx_auction_starttime" ON "Auctions" ("StartTime");
CREATE INDEX "idx_auction_search" ON "Auctions" USING GIN ("SearchVector");

-- Trigger: Auto-update SearchVector
CREATE OR REPLACE FUNCTION update_auction_search_vector()
RETURNS TRIGGER AS $$
BEGIN
  NEW."SearchVector" := 
    setweight(to_tsvector('simple', coalesce(NEW."Title", '')), 'A') ||
    setweight(to_tsvector('simple', coalesce(NEW."Description", '')), 'B');
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_update_auction_search
BEFORE INSERT OR UPDATE OF "Title", "Description" ON "Auctions"
FOR EACH ROW EXECUTE FUNCTION update_auction_search_vector();

-- Follows Table
CREATE TABLE "Follows" (
    "Id" BIGINT PRIMARY KEY,
    "UserId" BIGINT NOT NULL,
    "AuctionId" BIGINT NOT NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT "FK_Follows_Auctions" FOREIGN KEY ("AuctionId") REFERENCES "Auctions" ("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX "idx_follow_user_auction" ON "Follows" ("UserId", "AuctionId");
CREATE INDEX "idx_follow_userid" ON "Follows" ("UserId");
CREATE INDEX "idx_follow_auctionid" ON "Follows" ("AuctionId");

-- ResponseCodes Table
CREATE TABLE "ResponseCodes" (
    "Code" VARCHAR(50) PRIMARY KEY,
    "Name" VARCHAR(100) NOT NULL,
    "MessageZhTw" TEXT NOT NULL,
    "MessageEn" TEXT NOT NULL,
    "Category" VARCHAR(20) NOT NULL,
    "Severity" VARCHAR(20) NOT NULL
);

-- Insert initial response codes
INSERT INTO "ResponseCodes" ("Code", "Name", "MessageZhTw", "MessageEn", "Category", "Severity") VALUES
('SUCCESS', 'Success', '操作成功', 'Operation successful', 'Success', 'Info'),
('AUCTION_NOT_FOUND', 'Auction Not Found', '找不到該商品', 'Auction not found', 'ClientError', 'Error'),
('AUCTION_CONCURRENT_EDIT', 'Concurrent Edit Detected', '商品已被其他使用者修改，請重新載入後再試', 'Auction was modified by another user. Please reload and try again', 'ClientError', 'Warning'),
('BIDDING_SERVICE_UNAVAILABLE', 'Bidding Service Unavailable', '出價服務暫時無法連線，顯示的出價資訊可能過時', 'Bidding service is temporarily unavailable. Bid information may be outdated', 'ServerError', 'Warning'),
('UNAUTHORIZED', 'Unauthorized', '需要登入才能執行此操作', 'Authentication required', 'ClientError', 'Error'),
('FORBIDDEN', 'Forbidden', '您沒有權限執行此操作', 'You do not have permission to perform this action', 'ClientError', 'Error'),
('INVALID_INPUT', 'Invalid Input', '輸入的資料不符合要求', 'Input data is invalid', 'ClientError', 'Error'),
('AUCTION_ALREADY_HAS_BIDS', 'Auction Already Has Bids', '已有出價的商品無法編輯或刪除', 'Cannot edit or delete auction with existing bids', 'ClientError', 'Error'),
('CANNOT_FOLLOW_OWN_AUCTION', 'Cannot Follow Own Auction', '無法追蹤自己建立的商品', 'Cannot follow your own auction', 'ClientError', 'Error'),
('ALREADY_FOLLOWING', 'Already Following', '已在追蹤清單中', 'Already following this auction', 'ClientError', 'Info');
```

---

## Repository Interfaces

### IAuctionRepository

```csharp
namespace AuctionService.Domain.Interfaces;

public interface IAuctionRepository
{
    Task<Auction?> GetByIdAsync(long id);
    Task<List<Auction>> GetAllAsync(int pageIndex, int pageSize, long? categoryId = null, string? keyword = null);
    Task<List<Auction>> GetByCreatorIdAsync(long creatorId);
    Task<Auction> AddAsync(Auction auction);
    Task UpdateAsync(Auction auction);
    Task DeleteAsync(Auction auction);
    Task<bool> HasBidsAsync(long auctionId);  // 呼叫 Bidding Service 檢查
}
```

### ICategoryRepository

```csharp
namespace AuctionService.Domain.Interfaces;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(long id);
    Task<List<Category>> GetAllAsync();
    Task<bool> ExistsAsync(long id);
}
```

### IFollowRepository

```csharp
namespace AuctionService.Domain.Interfaces;

public interface IFollowRepository
{
    Task<Follow?> GetByUserAndAuctionAsync(long userId, long auctionId);
    Task<List<Follow>> GetByUserIdAsync(long userId);
    Task<Follow> AddAsync(Follow follow);
    Task DeleteAsync(Follow follow);
    Task<bool> IsFollowingAsync(long userId, long auctionId);
}
```

---

## ER Diagram

```
┌─────────────────┐         ┌─────────────────┐
│   Categories    │         │    Auctions     │
├─────────────────┤         ├─────────────────┤
│ Id (PK)         │<────────│ Id (PK)         │
│ Name (UK)       │ 1     * │ Title           │
│ DisplayOrder    │         │ Description     │
│ IsEnabled       │         │ StartingPrice   │
└─────────────────┘         │ CategoryId (FK) │
                            │ StartTime       │
                            │ EndTime         │
                            │ CreatorId       │
                            │ ImageUrls       │
                            │ SearchVector    │
                            │ RowVersion      │
                            │ CreatedAt       │
                            │ UpdatedAt       │
                            └─────────────────┘
                                     △
                                     │
                                     │ *
                            ┌─────────────────┐
                            │     Follows     │
                            ├─────────────────┤
                            │ Id (PK)         │
                            │ UserId          │
                            │ AuctionId (FK)  │
                            │ CreatedAt       │
                            └─────────────────┘
                            (UK: UserId + AuctionId)

┌─────────────────────┐
│   ResponseCodes     │
├─────────────────────┤
│ Code (PK)           │
│ Name                │
│ MessageZhTw         │
│ MessageEn           │
│ Category            │
│ Severity            │
└─────────────────────┘
```

---

## Summary

- **4 核心實體**: Auction, Category, Follow, ResponseCode
- **Snowflake ID**: 所有主鍵使用 64-bit Long
- **樂觀鎖**: Auction.RowVersion 防止並發編輯
- **全文檢索**: SearchVector + GIN 索引 + trigger 自動更新
- **JSON 儲存**: ImageUrls 使用 JSONB 欄位
- **外鍵約束**: Category → Auction (Restrict), Auction → Follow (Cascade)
- **唯一索引**: Category.Name, Follow(UserId + AuctionId)

---

**Last Updated**: 2025-11-20  
**Author**: GitHub Copilot (Claude Sonnet 4.5)
