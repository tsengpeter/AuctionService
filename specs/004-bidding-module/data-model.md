# Data Model: Bidding 模組

**Branch**: `004-bidding-module` | **Date**: 2026-04-27

---

## 實體：Bid（出價）

### C# 實體定義

```csharp
namespace Bidding.Domain.Entities;

public class Bid : BaseEntity
{
    public Guid AuctionId { get; private set; }        // 邏輯參照（無 EF Navigation）
    public Guid BidderId { get; private set; }         // 邏輯參照（無 EF Navigation）
    public string BidderUsername { get; private set; } = string.Empty; // 反正規化顯示用
    public long Amount { get; private set; }           // 正整數，單位：元
    public DateTimeOffset PlacedAt { get; private set; }
    public BidStatus Status { get; private set; }

    private Bid() { }  // EF Core 使用

    public static Bid Place(Guid auctionId, Guid bidderId, string bidderUsername, long amount)
    {
        if (auctionId == Guid.Empty) throw new ArgumentException("AuctionId cannot be empty.");
        if (bidderId == Guid.Empty) throw new ArgumentException("BidderId cannot be empty.");
        ArgumentException.ThrowIfNullOrWhiteSpace(bidderUsername);
        if (amount <= 0) throw new ArgumentException("Amount must be greater than 0.");

        var now = DateTimeOffset.UtcNow;
        return new Bid
        {
            Id = Guid.NewGuid(),
            AuctionId = auctionId,
            BidderId = bidderId,
            BidderUsername = bidderUsername,
            Amount = amount,
            PlacedAt = now,
            Status = BidStatus.Leading,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void SetStatus(BidStatus newStatus)
    {
        Status = newStatus;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
```

### BidStatus Enum

```csharp
namespace Bidding.Domain.Enums;

public enum BidStatus
{
    Leading,  // 目前最高出價者（Active 拍賣中）
    Outbid,   // 已被他人超越（Active 拍賣中）
    Won,      // 得標（拍賣已結束）
    Lost      // 未得標（拍賣已結束）
}
```

---

## EF Core 配置

```csharp
namespace Bidding.Infrastructure.Persistence.Configurations;

public class BidConfiguration : IEntityTypeConfiguration<Bid>
{
    public void Configure(EntityTypeBuilder<Bid> builder)
    {
        builder.ToTable("bids", "bidding");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AuctionId).IsRequired();
        builder.Property(x => x.BidderId).IsRequired();
        builder.Property(x => x.BidderUsername).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Amount).IsRequired();  // bigint
        builder.Property(x => x.PlacedAt).IsRequired();
        builder.Property(x => x.Status)
               .IsRequired()
               .HasConversion<string>()
               .HasMaxLength(20);

        // 無跨模組 EF Foreign Key
        builder.HasIndex(x => new { x.AuctionId, x.Amount })  // 加速 MAX(Amount) WHERE AuctionId
               .HasDatabaseName("IX_bids_auction_amount");
        builder.HasIndex(x => x.BidderId)                      // 加速 GetMyBids
               .HasDatabaseName("IX_bids_bidder_id");
        builder.HasIndex(x => new { x.AuctionId, x.Status })   // 加速結標批次更新
               .HasDatabaseName("IX_bids_auction_status");
    }
}
```

---

## 資料庫 Schema（PostgreSQL）

### 表：`bidding.bids`

| 欄位 | 類型 | Nullable | 說明 |
|------|------|----------|------|
| `Id` | `uuid` | NOT NULL | PK，Guid.NewGuid() |
| `AuctionId` | `uuid` | NOT NULL | 邏輯參照 auction.auctions.Id |
| `BidderId` | `uuid` | NOT NULL | 邏輯參照 member.users.Id |
| `BidderUsername` | `varchar(100)` | NOT NULL | 反正規化自 Member 模組 |
| `Amount` | `bigint` | NOT NULL | 正整數，元 |
| `PlacedAt` | `timestamptz` | NOT NULL | 出價時間 UTC |
| `Status` | `varchar(20)` | NOT NULL | Leading / Outbid / Won / Lost |
| `CreatedAt` | `timestamptz` | NOT NULL | |
| `UpdatedAt` | `timestamptz` | NOT NULL | |

### 索引

| 索引名稱 | 欄位 | 用途 |
|---------|------|------|
| `PK_bids` | `Id` | PK |
| `IX_bids_auction_amount` | `(AuctionId, Amount)` | `MAX(Amount) WHERE AuctionId`（快取遺失 fallback）|
| `IX_bids_bidder_id` | `BidderId` | `GetMyBids`（依使用者查詢）|
| `IX_bids_auction_status` | `(AuctionId, Status)` | 批次更新 Won/Lost |

---

## Migration 策略

現有 `InitialCreate`（`20260322094649`）已建立 `bids` 表，但缺少：
- `BidderUsername` 欄位
- `Status` 欄位  
- `Amount` 類型為 `decimal(18,2)`，需改為 `bigint`

**新 Migration 名稱**：`AddBidStatusAndBidder`

**上行 SQL（示意）**：
```sql
-- 新增欄位
ALTER TABLE bidding.bids ADD COLUMN "BidderUsername" varchar(100) NOT NULL DEFAULT '';
ALTER TABLE bidding.bids ADD COLUMN "Status" varchar(20) NOT NULL DEFAULT 'Leading';

-- 修改 Amount 類型（decimal → bigint，現有 0 筆資料，直接轉型）
ALTER TABLE bidding.bids ALTER COLUMN "Amount" TYPE bigint USING "Amount"::bigint;

-- 新增索引
CREATE INDEX "IX_bids_auction_amount" ON bidding.bids ("AuctionId", "Amount");
CREATE INDEX "IX_bids_bidder_id" ON bidding.bids ("BidderId");
CREATE INDEX "IX_bids_auction_status" ON bidding.bids ("AuctionId", "Status");
```

---

## Redis 快取結構

### 最高出價快取

- **Key**：`bid:highest:{auctionId}` （例：`bid:highest:a1b2c3d4-...`）  
- **Type**：Redis Hash  
- **Fields**：`amount`（string，整數值）、`bidderId`（string，Guid）  
- **TTL**：無（手動清除，拍賣結標時由 `AuctionWonEventHandler` 呼叫 `DEL`）  
- **操作**：`HGETALL` 讀取、`HSET` 寫入

### 頻率限制 Key

- **Key**：`bid:ratelimit:{userId}:{auctionId}`  
- **Type**：String  
- **Value**：`1`  
- **TTL**：1 秒  
- **操作**：`SET NX EX 1`（原子性）

---

## 跨模組介面

### IAuctionQueryService（Bidding.Application.Abstractions）

```csharp
public enum AuctionStatus { Active, Ended }

public interface IAuctionQueryService
{
    Task<AuctionStatus?> GetAuctionStatusAsync(Guid auctionId, CancellationToken ct);
    Task<Guid?> GetAuctionOwnerIdAsync(Guid auctionId, CancellationToken ct);
}
```

**實作**（`Bidding.Infrastructure.Services.AuctionQueryService`）：
- 注入 `NpgsqlDataSource`
- 查詢 `auction.auctions` 表（`"Status"`、`"OwnerId"` 欄位，PascalCase，無 `UseSnakeCaseNamingConvention`）
- 回傳 `null` 表示商品不存在

### IMemberQueryService（Bidding.Application.Abstractions）

```csharp
public interface IMemberQueryService
{
    /// 回傳會員的 Username；會員不存在時回傳 null
    Task<string?> GetMemberUsernameAsync(Guid userId, CancellationToken ct);
}
```

**實作**（`Bidding.Infrastructure.Services.MemberQueryService`）：
- 注入 `NpgsqlDataSource`
- 查詢 `member.users` 表：
  ```sql
  SELECT "Username" FROM member.users WHERE "Id" = @userId LIMIT 1
  ```
- 回傳 `null` 表示會員不存在（回傳 HTTP 403）

**使用時機**：`PlaceBidCommandHandler` 在頻率限制檢查後、出價逻輯前呼叫。同步取得 `BidderUsername`（反正規化存入 `bids` 表）。

### IBiddingQueryService（Auction.Application.Abstractions — 已存在）

```csharp
public record BidInfoDto(Guid WinnerId, decimal Amount);

public interface IBiddingQueryService
{
    Task<BidInfoDto?> GetHighestBidAsync(Guid auctionId, CancellationToken ct);
}
```

**替換實作**（`Auction.Infrastructure.Services.BiddingQueryService`）：
- 注入 `NpgsqlDataSource`
- 查詢 `bidding.bids` 表取得最高出價者（Status = Leading 或 Won）
- 取代現有 `NullBiddingQueryService`

---

## DTOs

### BidDto（出價歷史）

```csharp
public record BidDto(
    Guid Id,
    Guid BidderId,
    string BidderUsername,
    long Amount,
    DateTimeOffset PlacedAt
);
```

### MyBidDto（我的出價）

```csharp
public record MyBidDto(
    Guid Id,
    Guid AuctionId,
    long Amount,
    DateTimeOffset PlacedAt,
    string Status  // "leading" | "outbid" | "won" | "lost"
);
```

### PlaceBidResponse（出價成功）

```csharp
public record PlaceBidResponse(
    Guid BidId,
    Guid AuctionId,
    long Amount,
    DateTimeOffset PlacedAt,
    string Status  // "leading"
);
```

### PagedResult\<T\>（通用分頁）

```csharp
public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize
);
```
