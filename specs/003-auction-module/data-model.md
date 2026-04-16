# 資料模型：Auction 模組

**Branch**: `003-auction-module`  
**Date**: 2026-04-15  
**Schema**: `auction`

---

## 實體關係圖

```
auction.categories (1) ─────────── (N) auction.auctions
  id (UUID, PK)                          id (UUID, PK)
  name (VARCHAR 100, NOT NULL)           owner_id (UUID, NOT NULL)         ← logical ref to member.users
  parent_id (UUID, FK self, NULL)        title (VARCHAR 200, NOT NULL)
                                         description (TEXT, NULL)
                                         starting_price (DECIMAL 18,2)
                                         start_time (TIMESTAMPTZ, NOT NULL)
                                         end_time (TIMESTAMPTZ, NOT NULL)
auction.auctions (1) ──── (N) ──── auction.auction_images
                                         id (UUID, PK)
                                         auction_id (UUID, FK)
                                         url (TEXT, NOT NULL)
                                         display_order (INT, NOT NULL)

auction.auctions (N) ──── (N) ──── auction.watchlist (via user_id + auction_id)
                                         id (UUID, PK)
                                         user_id (UUID, NOT NULL)         ← logical ref to member.users
                                         auction_id (UUID, FK)
                                         created_at (TIMESTAMPTZ)
                                         UNIQUE(user_id, auction_id)
```

---

## Domain 實體

### Auction（`src/Modules/Auction/Domain/Entities/Auction.cs`）

```csharp
public enum AuctionStatus { Active, Ended }

public class Auction : BaseEntity
{
    public Guid OwnerId { get; private set; }           // 賣家 ID（邏輯 Guid，非 EF FK）
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal StartingPrice { get; private set; }
    public DateTimeOffset StartTime { get; private set; }
    public DateTimeOffset EndTime { get; private set; }
    public AuctionStatus Status { get; private set; } = AuctionStatus.Active;
    public Guid? CategoryId { get; private set; }
    public Guid? WinnerId { get; private set; }         // 結標後填入（邏輯 Guid）
    public decimal? SoldAmount { get; private set; }    // 結標後填入

    private readonly List<AuctionImage> _images = new();
    public IReadOnlyCollection<AuctionImage> Images => _images.AsReadOnly();

    private Auction() { }   // EF Core

    public static Auction Create(
        Guid ownerId,
        string title,
        string? description,
        decimal startingPrice,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        Guid? categoryId,
        IEnumerable<string> imageUrls)
    {
        var now = DateTimeOffset.UtcNow;
        var auction = new Auction
        {
            OwnerId = ownerId,
            Title = title.Trim(),
            Description = description?.Trim(),
            StartingPrice = startingPrice,
            StartTime = startTime,
            EndTime = endTime,
            Status = AuctionStatus.Active,   // 建立即 Active
            CategoryId = categoryId,
            CreatedAt = now,
            UpdatedAt = now
        };

        var urls = imageUrls.ToList();
        for (int i = 0; i < urls.Count; i++)
            auction._images.Add(AuctionImage.Create(auction.Id, urls[i], i + 1));  // DisplayOrder 1-indexed

        return auction;
    }

    // 修改全欄位（競標開放前，now < StartTime）
    public void UpdateAll(
        string title,
        string? description,
        decimal startingPrice,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        Guid? categoryId,
        IEnumerable<string> imageUrls)
    {
        if (Status == AuctionStatus.Ended)
            throw new InvalidOperationException("Cannot update an ended auction.");
        if (DateTimeOffset.UtcNow >= StartTime)
            throw new InvalidOperationException("Bidding has started; cannot update competitive fields.");

        Title = title.Trim();
        Description = description?.Trim();
        StartingPrice = startingPrice;
        StartTime = startTime;
        EndTime = endTime;
        CategoryId = categoryId;
        ReplaceImages(imageUrls);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    // 修改非競標敏感欄位（競標開放後，now >= StartTime）
    public void UpdateNonSensitive(
        string? title,
        string? description,
        Guid? categoryId,
        IEnumerable<string>? imageUrls)
    {
        if (Status == AuctionStatus.Ended)
            throw new InvalidOperationException("Cannot update an ended auction.");

        if (title is not null) Title = title.Trim();
        if (description is not null) Description = description.Trim();
        if (categoryId is not null) CategoryId = categoryId;
        if (imageUrls is not null) ReplaceImages(imageUrls);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private void ReplaceImages(IEnumerable<string> imageUrls)
    {
        _images.Clear();
        var urls = imageUrls.ToList();
        for (int i = 0; i < urls.Count; i++)
            _images.Add(AuctionImage.Create(Id, urls[i], i + 1));  // DisplayOrder 1-indexed
    }

    public void End(Guid? winnerId, decimal? soldAmount)
    {
        if (Status != AuctionStatus.Active)
            throw new InvalidOperationException($"Cannot end auction in status {Status}.");
        Status = AuctionStatus.Ended;
        WinnerId = winnerId;
        SoldAmount = soldAmount;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
```

---

### AuctionImage（`src/Modules/Auction/Domain/Entities/AuctionImage.cs`）

Owned Entity，生命週期依附於 `Auction`。

```csharp
public class AuctionImage
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid AuctionId { get; private set; }
    public string Url { get; private set; } = string.Empty;
    public int DisplayOrder { get; private set; }

    private AuctionImage() { }   // EF Core

    public static AuctionImage Create(Guid auctionId, string url, int displayOrder) =>
        new() { AuctionId = auctionId, Url = url, DisplayOrder = displayOrder };
}
```

---

### Category（`src/Modules/Auction/Domain/Entities/Category.cs`）

系統預先種入，本 Phase 不提供 CRUD 管理。

```csharp
public class Category : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public Guid? ParentId { get; private set; }   // 支援層級結構（查詢僅精確匹配）

    private Category() { }

    public static Category Create(string name, Guid? parentId = null) =>
        new()
        {
            Name = name,
            ParentId = parentId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
}
```

---

### Watchlist（`src/Modules/Auction/Domain/Entities/Watchlist.cs`）

```csharp
public class Watchlist : BaseEntity
{
    public Guid UserId { get; private set; }       // 邏輯 Guid，非 EF FK
    public Guid AuctionId { get; private set; }

    private Watchlist() { }

    public static Watchlist Create(Guid userId, Guid auctionId) =>
        new()
        {
            UserId = userId,
            AuctionId = auctionId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
}
```

---

### AuctionWonEvent（`src/Modules/Auction/Domain/Events/AuctionWonEvent.cs`）

```csharp
public record AuctionWonEvent(
    Guid AuctionId,
    Guid WinnerId,
    decimal SoldAmount,
    Guid SellerId
) : IDomainEvent;
```

---

## Application 層

### IBiddingQueryService（Application/Abstractions）

```csharp
public record BidInfoDto(Guid WinnerId, decimal Amount);

public interface IBiddingQueryService
{
    /// <summary>
    /// 取得指定拍賣的最高出價資訊。
    /// 回傳 null 表示尚無出價。
    /// WinnerId 為目前安排最高出價者，不應為 null。
    /// </summary>
    Task<BidInfoDto?> GetHighestBidAsync(Guid auctionId, CancellationToken ct = default);
}
```

### PagedResult\<T\>（Application/DTOs）

```csharp
public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
```

### Commands

| Command | 必填欄位 | 驗證規則 | 錯誤 |
|---------|---------|---------|------|
| `CreateAuctionCommand` | title, startingPrice, startTime, endTime, ownerId（from JWT） | startingPrice > 0；startTime > UtcNow；endTime > startTime+1min；imageUrls ≤ 5；URL 格式有效 | 422 |
| `UpdateAuctionCommand` | id, ownerId（from JWT） | 商品需存在；狀態為 Active；requesterId == ownerId；競標開放前可修改全欄位；競標開放後禁止修改 startingPrice/startTime/endTime | 404/409/403/422 |
| `AddWatchlistCommand` | auctionId, userId（from JWT） | 冪等（已存在則 skip） | — |
| `RemoveWatchlistCommand` | auctionId, userId（from JWT） | 冪等（不存在則 skip） | — |

### Queries

| Query | 參數 | 回傳 |
|-------|------|------|
| `GetAuctionsQuery` | q?, categoryId?, page=1, pageSize=20（max 100） | `PagedResult<AuctionListItemDto>` |
| `GetAuctionDetailQuery` | id | `AuctionDetailDto`（含 currentHighestBid?） |
| `GetWatchlistQuery` | userId（from JWT）, status?（null=全部/active=Active only） | `List<WatchlistItemDto>` |

---

## Infrastructure 層

### AuctionDbContext（Schema 隔離）

```csharp
public class AuctionDbContext : DbContext
{
    public DbSet<Auction> Auctions => Set<Auction>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Watchlist> Watchlist => Set<Watchlist>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("auction");
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
    }
}
```

### EF Core Configuration 重點

```csharp
// AuctionConfiguration.cs
builder.ToTable("auctions");
builder.HasKey(a => a.Id);
builder.Property(a => a.Title).HasMaxLength(200).IsRequired();
builder.Property(a => a.StartingPrice).HasPrecision(18, 2);
builder.Property(a => a.Status).HasConversion<string>();
builder.Property(a => a.StartTime).IsRequired();

// Indexes
builder.HasIndex(a => new { a.Status, a.EndTime });   // 結標掃描
builder.HasIndex(a => a.OwnerId);                      // 擁有者查詢
builder.HasIndex(a => a.CategoryId);                   // 分類篩選

// Owned Entity：AuctionImage
builder.OwnsMany(a => a.Images, img =>
{
    img.ToTable("auction_images");
    img.Property(i => i.Url).IsRequired();
    img.HasIndex(i => i.AuctionId);
});

// WatchlistConfiguration.cs
builder.ToTable("watchlist");
builder.HasIndex(w => new { w.UserId, w.AuctionId }).IsUnique();   // 冪等約束
builder.HasIndex(w => w.UserId);

// CategoryConfiguration.cs
builder.ToTable("categories");
builder.HasIndex(c => c.ParentId);
```

---

## 資料庫 Schema

```sql
-- auction schema tables
auction.auctions
auction.auction_images
auction.categories
auction.watchlist

-- Key indexes
CREATE INDEX idx_auctions_status_end_time ON auction.auctions (status, end_time);
CREATE INDEX idx_auctions_start_time ON auction.auctions (start_time);
CREATE INDEX idx_auctions_owner_id ON auction.auctions (owner_id);
CREATE INDEX idx_auctions_category_id ON auction.auctions (category_id);
CREATE UNIQUE INDEX idx_watchlist_user_auction ON auction.watchlist (user_id, auction_id);
```
