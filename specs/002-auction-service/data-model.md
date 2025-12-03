# 資料模型設計：商品拍賣服務

**功能分支**: `002-auction-service`  
**建立日期**: 2025-12-02  
**階段**: Phase 1 - Data Model Design

## 概述

本文件定義商品拍賣服務的核心資料模型，包含實體定義、欄位規格、關聯關係、驗證規則與索引策略。採用 Entity Framework Core Code First 方法，使用 Fluent API 進行配置。

---

## 實體定義

### 1. Auction (拍賣商品)

代表拍賣平台上的商品，包含完整的商品資訊與時間管理。

#### 欄位規格

| 欄位名稱 | 資料型別 | 必填 | 說明 | 驗證規則 |
|---------|---------|------|------|---------|
| `Id` | `Guid` | ✅ | 商品唯一識別碼 (主鍵) | 自動生成 |
| `Name` | `string` | ✅ | 商品名稱 | 長度 3-200 字元 |
| `Description` | `string` | ✅ | 商品描述 | 長度 10-2000 字元 |
| `StartingPrice` | `decimal` | ✅ | 起標價 | > 0, ≤ 10,000,000, 精度 (18,2) |
| `CategoryId` | `int` | ✅ | 分類 ID (外鍵 → Category) | 必須存在於 Categories 資料表 |
| `StartTime` | `DateTime` | ✅ | 拍賣開始時間 | UTC 時區 |
| `EndTime` | `DateTime` | ✅ | 拍賣結束時間 | UTC 時區, 必須 > StartTime, 至少 StartTime + 1 小時 |
| `UserId` | `Guid` | ✅ | 建立者使用者 ID | 來自 Auth Service |
| `CreatedAt` | `DateTime` | ✅ | 建立時間 | UTC 時區, 自動設定 |
| `UpdatedAt` | `DateTime` | ✅ | 更新時間 | UTC 時區, 自動更新 |

#### 關聯關係
- **Category** (多對一): 一個商品屬於一個分類
- **Follows** (一對多): 一個商品可被多個使用者追蹤

#### 計算欄位 (不儲存)
- **Status** (string): 商品狀態，透過查詢時計算
  - `Pending`: `NOW() < StartTime`
  - `Active`: `NOW() >= StartTime AND NOW() < EndTime`
  - `Ended`: `NOW() >= EndTime`

#### 索引
- **主鍵索引**: `Id` (Clustered)
- **外鍵索引**: `CategoryId`
- **效能索引**: `EndTime` (支援狀態篩選查詢)
- **複合索引**: `UserId + CreatedAt` (支援使用者商品查詢與排序)

#### EF Core 配置
```csharp
public class AuctionConfiguration : IEntityTypeConfiguration<Auction>
{
    public void Configure(EntityTypeBuilder<Auction> builder)
    {
        builder.ToTable("Auctions");
        
        builder.HasKey(a => a.Id);
        
        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(a => a.Description)
            .IsRequired()
            .HasMaxLength(2000);
        
        builder.Property(a => a.StartingPrice)
            .IsRequired()
            .HasPrecision(18, 2);
        
        builder.Property(a => a.StartTime)
            .IsRequired();
        
        builder.Property(a => a.EndTime)
            .IsRequired();
        
        builder.Property(a => a.UserId)
            .IsRequired();
        
        builder.Property(a => a.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");
        
        builder.Property(a => a.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");
        
        // 關聯關係
        builder.HasOne(a => a.Category)
            .WithMany()
            .HasForeignKey(a => a.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // 索引
        builder.HasIndex(a => a.EndTime)
            .HasDatabaseName("IX_Auctions_EndTime");
        
        builder.HasIndex(a => a.CategoryId)
            .HasDatabaseName("IX_Auctions_CategoryId");
        
        builder.HasIndex(a => new { a.UserId, a.CreatedAt })
            .HasDatabaseName("IX_Auctions_UserId_CreatedAt");
    }
}
```

---

### 2. Category (商品分類)

代表商品的分類選項，用於組織與篩選商品。

#### 欄位規格

| 欄位名稱 | 資料型別 | 必填 | 說明 | 驗證規則 |
|---------|---------|------|------|---------|
| `Id` | `int` | ✅ | 分類唯一識別碼 (主鍵) | 自動遞增 |
| `Name` | `string` | ✅ | 分類名稱 | 長度 2-50 字元, 唯一 |
| `DisplayOrder` | `int` | ✅ | 顯示順序 | ≥ 0 |
| `IsActive` | `bool` | ✅ | 是否啟用 | 預設 true |
| `CreatedAt` | `DateTime` | ✅ | 建立時間 | UTC 時區 |

#### 關聯關係
- **Auctions** (一對多): 一個分類可包含多個商品

#### 種子資料
```csharp
new Category { Id = 1, Name = "電子產品", DisplayOrder = 1, IsActive = true },
new Category { Id = 2, Name = "家具", DisplayOrder = 2, IsActive = true },
new Category { Id = 3, Name = "收藏品", DisplayOrder = 3, IsActive = true },
new Category { Id = 4, Name = "藝術品", DisplayOrder = 4, IsActive = true },
new Category { Id = 5, Name = "服飾配件", DisplayOrder = 5, IsActive = true },
new Category { Id = 6, Name = "書籍", DisplayOrder = 6, IsActive = true },
new Category { Id = 7, Name = "運動用品", DisplayOrder = 7, IsActive = true },
new Category { Id = 8, Name = "其他", DisplayOrder = 99, IsActive = true }
```

#### EF Core 配置
```csharp
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
            .IsRequired()
            .HasDefaultValueSql("NOW()");
        
        // 唯一約束
        builder.HasIndex(c => c.Name)
            .IsUnique()
            .HasDatabaseName("IX_Categories_Name_Unique");
        
        // 種子資料
        builder.HasData(
            new Category { Id = 1, Name = "電子產品", DisplayOrder = 1, IsActive = true, CreatedAt = DateTime.UtcNow },
            new Category { Id = 2, Name = "家具", DisplayOrder = 2, IsActive = true, CreatedAt = DateTime.UtcNow },
            new Category { Id = 3, Name = "收藏品", DisplayOrder = 3, IsActive = true, CreatedAt = DateTime.UtcNow },
            new Category { Id = 4, Name = "藝術品", DisplayOrder = 4, IsActive = true, CreatedAt = DateTime.UtcNow },
            new Category { Id = 5, Name = "服飾配件", DisplayOrder = 5, IsActive = true, CreatedAt = DateTime.UtcNow },
            new Category { Id = 6, Name = "書籍", DisplayOrder = 6, IsActive = true, CreatedAt = DateTime.UtcNow },
            new Category { Id = 7, Name = "運動用品", DisplayOrder = 7, IsActive = true, CreatedAt = DateTime.UtcNow },
            new Category { Id = 8, Name = "其他", DisplayOrder = 99, IsActive = true, CreatedAt = DateTime.UtcNow }
        );
    }
}
```

---

### 3. Follow (商品追蹤)

代表使用者對商品的追蹤關係，支援使用者追蹤清單功能。

#### 欄位規格

| 欄位名稱 | 資料型別 | 必填 | 說明 | 驗證規則 |
|---------|---------|------|------|---------|
| `Id` | `Guid` | ✅ | 追蹤記錄唯一識別碼 (主鍵) | 自動生成 |
| `UserId` | `Guid` | ✅ | 使用者 ID | 來自 Auth Service |
| `AuctionId` | `Guid` | ✅ | 商品 ID (外鍵 → Auction) | 必須存在於 Auctions 資料表 |
| `CreatedAt` | `DateTime` | ✅ | 追蹤時間 | UTC 時區 |

#### 關聯關係
- **Auction** (多對一): 多個追蹤記錄指向同一個商品

#### 業務規則
- 使用者不能追蹤自己建立的商品
- 同一使用者不能重複追蹤同一商品 (唯一約束)
- 每個使用者最多追蹤 500 個商品 (應用層驗證)

#### 索引
- **主鍵索引**: `Id` (Clustered)
- **外鍵索引**: `AuctionId`
- **複合唯一索引**: `UserId + AuctionId` (防止重複追蹤)
- **使用者查詢索引**: `UserId + CreatedAt` (支援追蹤清單查詢與排序)

#### EF Core 配置
```csharp
public class FollowConfiguration : IEntityTypeConfiguration<Follow>
{
    public void Configure(EntityTypeBuilder<Follow> builder)
    {
        builder.ToTable("Follows");
        
        builder.HasKey(f => f.Id);
        
        builder.Property(f => f.UserId)
            .IsRequired();
        
        builder.Property(f => f.AuctionId)
            .IsRequired();
        
        builder.Property(f => f.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");
        
        // 關聯關係
        builder.HasOne(f => f.Auction)
            .WithMany()
            .HasForeignKey(f => f.AuctionId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // 複合唯一索引 (防止重複追蹤)
        builder.HasIndex(f => new { f.UserId, f.AuctionId })
            .IsUnique()
            .HasDatabaseName("IX_Follows_UserId_AuctionId_Unique");
        
        // 使用者查詢索引
        builder.HasIndex(f => new { f.UserId, f.CreatedAt })
            .HasDatabaseName("IX_Follows_UserId_CreatedAt");
        
        // 外鍵索引
        builder.HasIndex(f => f.AuctionId)
            .HasDatabaseName("IX_Follows_AuctionId");
    }
}
```

---

### 4. ResponseCode (API 回應代碼)

代表 API 回應的標準化狀態代碼與訊息對照表，支援多語系與統一錯誤處理。

#### 欄位規格

| 欄位名稱 | 資料型別 | 必填 | 說明 | 驗證規則 |
|---------|---------|------|------|---------|
| `Code` | `string` | ✅ | 狀態代碼 (主鍵) | 例如 "AUCTION_NOT_FOUND", 大寫蛇形 |
| `Name` | `string` | ✅ | 顯示名稱 | 長度 2-100 字元 |
| `MessageZhTw` | `string` | ✅ | 繁體中文訊息 | 長度 2-500 字元 |
| `MessageEn` | `string` | ✅ | 英文訊息 | 長度 2-500 字元 |
| `Category` | `string` | ✅ | 分類 | Success / ClientError / ServerError |
| `Severity` | `string` | ✅ | 嚴重性 | Info / Warning / Error |
| `CreatedAt` | `DateTime` | ✅ | 建立時間 | UTC 時區 |

#### 種子資料範例
```csharp
// 成功回應
new ResponseCode { Code = "AUCTION_CREATED", Name = "商品已建立", MessageZhTw = "商品建立成功", MessageEn = "Auction created successfully", Category = "Success", Severity = "Info" },
new ResponseCode { Code = "AUCTION_UPDATED", Name = "商品已更新", MessageZhTw = "商品更新成功", MessageEn = "Auction updated successfully", Category = "Success", Severity = "Info" },
new ResponseCode { Code = "AUCTION_DELETED", Name = "商品已刪除", MessageZhTw = "商品刪除成功", MessageEn = "Auction deleted successfully", Category = "Success", Severity = "Info" },

// 客戶端錯誤
new ResponseCode { Code = "AUCTION_NOT_FOUND", Name = "商品不存在", MessageZhTw = "查無此商品", MessageEn = "Auction not found", Category = "ClientError", Severity = "Warning" },
new ResponseCode { Code = "AUCTION_INVALID_END_TIME", Name = "結束時間無效", MessageZhTw = "結束時間必須至少在 1 小時之後", MessageEn = "End time must be at least 1 hour from now", Category = "ClientError", Severity = "Warning" },
new ResponseCode { Code = "AUCTION_HAS_BIDS", Name = "商品已有出價", MessageZhTw = "已有出價的商品無法編輯或刪除", MessageEn = "Cannot edit or delete auction with existing bids", Category = "ClientError", Severity = "Warning" },
new ResponseCode { Code = "AUCTION_UNAUTHORIZED", Name = "無權限操作", MessageZhTw = "無權限操作此商品", MessageEn = "Unauthorized to access this auction", Category = "ClientError", Severity = "Error" },
new ResponseCode { Code = "FOLLOW_ALREADY_EXISTS", Name = "已追蹤", MessageZhTw = "已在追蹤清單中", MessageEn = "Already following this auction", Category = "ClientError", Severity = "Info" },
new ResponseCode { Code = "FOLLOW_OWN_AUCTION", Name = "無法追蹤自己的商品", MessageZhTw = "無法追蹤自己的商品", MessageEn = "Cannot follow your own auction", Category = "ClientError", Severity = "Warning" },
new ResponseCode { Code = "FOLLOW_LIMIT_EXCEEDED", Name = "追蹤數量超過限制", MessageZhTw = "追蹤商品數量已達上限 (500)", MessageEn = "Follow limit exceeded (500)", Category = "ClientError", Severity = "Warning" },

// 伺服器錯誤
new ResponseCode { Code = "INTERNAL_SERVER_ERROR", Name = "內部伺服器錯誤", MessageZhTw = "伺服器發生錯誤，請稍後再試", MessageEn = "Internal server error, please try again later", Category = "ServerError", Severity = "Error" },
new ResponseCode { Code = "BIDDING_SERVICE_UNAVAILABLE", Name = "競標服務無法使用", MessageZhTw = "競標服務暫時無法使用", MessageEn = "Bidding service is unavailable", Category = "ServerError", Severity = "Error" }
```

#### EF Core 配置
```csharp
public class ResponseCodeConfiguration : IEntityTypeConfiguration<ResponseCode>
{
    public void Configure(EntityTypeBuilder<ResponseCode> builder)
    {
        builder.ToTable("ResponseCodes");
        
        builder.HasKey(rc => rc.Code);
        
        builder.Property(rc => rc.Name)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(rc => rc.MessageZhTw)
            .IsRequired()
            .HasMaxLength(500);
        
        builder.Property(rc => rc.MessageEn)
            .IsRequired()
            .HasMaxLength(500);
        
        builder.Property(rc => rc.Category)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(rc => rc.Severity)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(rc => rc.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");
        
        // 種子資料 (簡化版)
        builder.HasData(
            new ResponseCode 
            { 
                Code = "AUCTION_CREATED", 
                Name = "商品已建立", 
                MessageZhTw = "商品建立成功", 
                MessageEn = "Auction created successfully", 
                Category = "Success", 
                Severity = "Info",
                CreatedAt = DateTime.UtcNow
            },
            // ... 其他種子資料
        );
    }
}
```

---

## 實體關係圖 (ERD)

```
┌─────────────────┐
│   Categories    │
├─────────────────┤
│ Id (PK)         │
│ Name            │
│ DisplayOrder    │
│ IsActive        │
│ CreatedAt       │
└─────────────────┘
        │
        │ 1
        │
        │ N
        ▼
┌─────────────────┐
│    Auctions     │
├─────────────────┤
│ Id (PK)         │
│ Name            │
│ Description     │
│ StartingPrice   │
│ CategoryId (FK) │◄───────┐
│ StartTime       │        │
│ EndTime         │        │
│ UserId          │        │
│ CreatedAt       │        │
│ UpdatedAt       │        │
└─────────────────┘        │
        │                  │
        │ 1                │
        │                  │
        │ N                │
        ▼                  │
┌─────────────────┐        │
│     Follows     │        │
├─────────────────┤        │
│ Id (PK)         │        │
│ UserId          │        │
│ AuctionId (FK)  │────────┘
│ CreatedAt       │
└─────────────────┘

┌─────────────────┐
│ ResponseCodes   │ (獨立資料表)
├─────────────────┤
│ Code (PK)       │
│ Name            │
│ MessageZhTw     │
│ MessageEn       │
│ Category        │
│ Severity        │
│ CreatedAt       │
└─────────────────┘
```

---

## 資料驗證規則總結

### Auction 驗證
- ✅ Name: 3-200 字元
- ✅ Description: 10-2000 字元
- ✅ StartingPrice: > 0, ≤ 10,000,000
- ✅ EndTime: 必須 > 當前時間 + 1 小時
- ✅ EndTime: 必須 > StartTime
- ✅ CategoryId: 必須存在於 Categories
- ✅ UserId: 不可為空 (來自 Auth Token)

### Follow 驗證
- ✅ 不可追蹤自己的商品 (UserId ≠ Auction.UserId)
- ✅ 不可重複追蹤 (唯一約束)
- ✅ 每個使用者最多 500 個追蹤

### Category 驗證
- ✅ Name: 唯一, 2-50 字元
- ✅ DisplayOrder: ≥ 0

---

## 索引策略總結

### 效能關鍵索引
| 資料表 | 索引 | 用途 |
|-------|------|------|
| Auctions | `EndTime` | 支援狀態篩選 (WHERE EndTime > NOW()) |
| Auctions | `UserId, CreatedAt` | 支援使用者商品查詢與排序 |
| Auctions | `CategoryId` | 支援分類篩選 |
| Follows | `UserId, AuctionId` | 防止重複追蹤 (唯一) |
| Follows | `UserId, CreatedAt` | 支援追蹤清單查詢與排序 |
| Categories | `Name` | 唯一約束, 支援分類查詢 |

---

## Migration 策略

### 初始 Migration
```bash
dotnet ef migrations add InitialCreate --project src/AuctionService.Infrastructure --startup-project src/AuctionService.Api
dotnet ef database update --project src/AuctionService.Infrastructure --startup-project src/AuctionService.Api
```

### Migration 命名規範
- 格式: `<YYYYMMDDHHMMSS>_<DescriptiveName>`
- 範例: 
  - `20251202100000_InitialCreate`
  - `20251202110000_AddEndTimeIndex`
  - `20251202120000_SeedCategoriesAndResponseCodes`

---

## 資料庫連線字串範例

### 開發環境
```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=auction_dev;Username=postgres;Password=postgres;Pooling=true;MinPoolSize=5;MaxPoolSize=100"
}
```

### 生產環境
```json
"ConnectionStrings": {
  "DefaultConnection": "Host=prod-db.example.com;Port=5432;Database=auction_prod;Username=auction_user;Password=${DB_PASSWORD};Pooling=true;MinPoolSize=10;MaxPoolSize=200;SSL Mode=Require"
}
```

---

## 總結

資料模型設計完成，包含 4 個核心實體 (Auction, Category, Follow, ResponseCode)、完整的關聯關係、驗證規則與索引策略。採用 EF Core Code First 方法，使用 Fluent API 配置，符合正規化原則與效能要求。後續將基於此模型生成 API 契約定義。
