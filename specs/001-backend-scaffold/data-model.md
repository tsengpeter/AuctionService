# 資料模型設計：後端模組化單體骨架

**功能分支**: `001-backend-scaffold`  
**建立日期**: 2026-03-22  
**狀態**: 完成（骨架設計）

---

## 概覽

此為骨架階段資料模型，每個模組僅定義最小必要實體以驗證架構可行性。業務實體（商品、標單、訂單等）將在各對應模組分支中詳細定義。

---

## Shared Layer（`AuctionService.Shared`）

### `BaseEntity`
所有模組實體的基礎類別，提供主鍵與稽核時間戳記。

| 屬性 | 型別 | 說明 |
|------|------|------|
| `Id` | `Guid` | 主鍵，COMB GUID（時序性） |
| `CreatedAt` | `DateTimeOffset` | 建立時間（UTC） |
| `UpdatedAt` | `DateTimeOffset` | 最後更新時間（UTC） |

**驗證規則**:
- `Id` 不可為 `Guid.Empty`
- `CreatedAt`、`UpdatedAt` 由基礎設施層自動填入，不可由外部設定

**C# 定義**:
```csharp
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; protected set; }
    public DateTimeOffset UpdatedAt { get; protected set; }
}
```

---

### `ApiResponse<T>` / `ApiResponse`（非持久化）
API 回應容器，不對應資料庫資料表，純粹用於序列化輸出。

#### 成功回應
```csharp
public record ApiResponse<T>(bool Success, T? Data, int StatusCode);
```

#### 驗證失敗回應（422）
```csharp
public record ApiResponse(bool Success, IEnumerable<FieldError>? Errors, string? Error, int StatusCode);
public record FieldError(string Field, string Message);
```

#### 工廠方法
```csharp
public static class ApiResponse
{
    public static ApiResponse<T> Ok<T>(T data, int statusCode = 200)
        => new(true, data, statusCode);

    public static ApiResponse Fail(string error, int statusCode)
        => new(false, null, error, statusCode);

    public static ApiResponse ValidationFail(IEnumerable<FieldError> errors)
        => new(false, errors, null, 422);
}
```

---

### `IDomainEvent`（介面）
所有模組領域事件的標記介面。

```csharp
public interface IDomainEvent : MediatR.INotification
{
    Guid EventId { get; }
    DateTimeOffset OccurredAt { get; }
}
```

---

## Member 模組（Schema: `member`）

### `MemberUser`（骨架實體）
代表系統使用者，骨架階段僅定義最小必要欄位。

| 屬性 | 型別 | 說明 | 驗證規則 |
|------|------|------|---------|
| `Id` | `Guid` | 主鍵（繼承自 BaseEntity） | 繼承 |
| `Email` | `string` | 使用者信箱（唯一索引） | 非空、RFC 5321 格式、最長 256 字元 |
| `Username` | `string` | 顯示名稱 | 非空、3–50 字元 |
| `PasswordHash` | `string` | bcrypt 雜湊密碼 | 不可直接設定 |
| `CreatedAt` | `DateTimeOffset` | 繼承自 BaseEntity | 繼承 |
| `UpdatedAt` | `DateTimeOffset` | 繼承自 BaseEntity | 繼承 |

**狀態轉移**: 無（骨架階段）

**EF Core 設定**:
```csharp
builder.ToTable("users", "member");
builder.HasIndex(x => x.Email).IsUnique();
builder.Property(x => x.Email).HasMaxLength(256).IsRequired();
builder.Property(x => x.Username).HasMaxLength(50).IsRequired();
```

---

## Auction 模組（Schema: `auction`）

### `AuctionItem`（骨架實體）

| 屬性 | 型別 | 說明 | 驗證規則 |
|------|------|------|---------|
| `Id` | `Guid` | 主鍵 | 繼承 |
| `Title` | `string` | 拍賣品名稱 | 非空、1–200 字元 |
| `StartingPrice` | `decimal` | 起標價 | > 0 |
| `Status` | `AuctionStatus` | 拍賣狀態（見下方） | 不可直接修改 |
| `EndsAt` | `DateTimeOffset` | 結標時間（UTC） | 必須 > CreatedAt |
| `CreatedAt` | `DateTimeOffset` | 繼承 | 繼承 |
| `UpdatedAt` | `DateTimeOffset` | 繼承 | 繼承 |

**狀態機**:
```
Pending → Active → Ended → Sold
                 ↘ Cancelled
```

```csharp
public enum AuctionStatus { Pending, Active, Ended, Sold, Cancelled }
```

---

## Bidding 模組（Schema: `bidding`）

### `Bid`（骨架實體）

| 屬性 | 型別 | 說明 | 驗證規則 |
|------|------|------|---------|
| `Id` | `Guid` | 主鍵 | 繼承 |
| `AuctionId` | `Guid` | 關聯拍賣 ID（跨模組邏輯 ID，非 FK） | 非空 |
| `BidderId` | `Guid` | 出價者 ID（跨模組邏輯 ID，非 FK） | 非空 |
| `Amount` | `decimal` | 出價金額 | > 0 |
| `PlacedAt` | `DateTimeOffset` | 出價時間 | 自動設定 |
| `CreatedAt` | `DateTimeOffset` | 繼承 | 繼承 |
| `UpdatedAt` | `DateTimeOffset` | 繼承 | 繼承 |

> **重要設計決策**: `AuctionId` 和 `BidderId` 不設定資料庫外鍵（跨 Schema FK），僅在應用程式層維護邏輯關聯，符合模組邊界原則。

---

## Ordering 模組（Schema: `ordering`）

### `Order`（骨架實體）

| 屬性 | 型別 | 說明 | 驗證規則 |
|------|------|------|---------|
| `Id` | `Guid` | 主鍵 | 繼承 |
| `AuctionId` | `Guid` | 關聯拍賣 ID（邏輯 ID） | 非空 |
| `BuyerId` | `Guid` | 買家 ID（邏輯 ID） | 非空 |
| `Amount` | `decimal` | 成交金額 | > 0 |
| `Status` | `OrderStatus` | 訂單狀態 | 不可直接修改 |
| `CreatedAt` | `DateTimeOffset` | 繼承 | 繼承 |
| `UpdatedAt` | `DateTimeOffset` | 繼承 | 繼承 |

```csharp
public enum OrderStatus { Pending, Confirmed, Shipped, Completed, Cancelled }
```

---

## Notification 模組（Schema: `notification`）

### `NotificationRecord`（骨架實體）

| 屬性 | 型別 | 說明 | 驗證規則 |
|------|------|------|---------|
| `Id` | `Guid` | 主鍵 | 繼承 |
| `RecipientId` | `Guid` | 接收者 ID（邏輯 ID） | 非空 |
| `Type` | `string` | 通知類型（如 "AuctionWon"） | 非空、最長 50 字元 |
| `Payload` | `string` | JSON 序列化通知內容 | 非空 |
| `SentAt` | `DateTimeOffset?` | 發送時間（null 表示待發送） | 可為 null |
| `CreatedAt` | `DateTimeOffset` | 繼承 | 繼承 |
| `UpdatedAt` | `DateTimeOffset` | 繼承 | 繼承 |

---

## 模組關聯圖

```
member.users ──(邏輯 ID)──→ bidding.bids.BidderId
member.users ──(邏輯 ID)──→ ordering.orders.BuyerId
member.users ──(邏輯 ID)──→ notification.notifications.RecipientId
auction.items ──(邏輯 ID)──→ bidding.bids.AuctionId
auction.items ──(邏輯 ID)──→ ordering.orders.AuctionId
```

> **注意**: 所有跨模組關聯均使用邏輯 ID（Guid），不設定資料庫層面的外鍵約束。完整性由應用程式層事件機制維護。

---

## 領域事件清單（骨架階段定義）

| 事件名稱 | 發布模組 | 訂閱模組 | 欄位 |
|---------|---------|---------|------|
| `AuctionWonEvent` | Auction | Ordering, Notification | `AuctionId`, `WinnerId`, `WinningBid` |
| `OrderCreatedEvent` | Ordering | Notification | `OrderId`, `BuyerId`, `Amount` |
| `BidPlacedEvent` | Bidding | Auction | `BidId`, `AuctionId`, `BidderId`, `Amount` |

---

## Migration 策略

```
每模組各維護獨立 Migration 歷史
├── src/Modules/Member/Infrastructure/Persistence/Migrations/
├── src/Modules/Auction/Infrastructure/Persistence/Migrations/
├── src/Modules/Bidding/Infrastructure/Persistence/Migrations/
├── src/Modules/Ordering/Infrastructure/Persistence/Migrations/
└── src/Modules/Notification/Infrastructure/Persistence/Migrations/
```

- **開發環境**: 手動執行 `dotnet ef database update` per module
- **整合測試**: Testcontainers 啟動後，由 Fixture 的 `InitializeAsync()` 呼叫 `context.Database.MigrateAsync()`
- **生產環境**: 不在應用程式啟動時自動執行（fail-fast 設計原則）
