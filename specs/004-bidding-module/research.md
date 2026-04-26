# Research: Bidding 模組

**Branch**: `004-bidding-module` | **Date**: 2026-04-27

---

## 決策 1：出價比較與狀態維護策略（Redis 快取 + Lua Script 原子操作）

**決策**：以 Redis 作為「目前最高出價」的主要快取層，`leading`/`outbid` 狀態同步更新至快取與 DB。最高出價的比較與設定使用 **Lua Script 原子操作**（`CompareAndSetHighestBidAsync`）以避免 TOCTOU 競態條件。

**架構流程**：
```
PlaceBid 請求
  → 頻率限制檢查（Redis SET NX TTL 1s）
  → 讀 auction 資訊（IAuctionQueryService → auction.auctions raw SQL）
      取得：Status、OwnerId、StartingPrice、StartTime、EndTime
    └─ 商品不存在 → 404
    └─ Status ≠ Active → 409
    └─ now < StartTime 或 now > EndTime → 409（競標時間外）
  → 確認會員存在（IMemberQueryService → member.users raw SQL）
  → 執行 Redis Lua Script（CompareAndSetHighestBidAsync）：
      如果 amount > current_highest（或快取為空）：
        HSET bid:highest:{auctionId} amount/bidderId
        回傳 1（允許繼續）
      否則：回傳 0 → 422
  → [Lua 回 1] DB Transaction：
      UPDATE bids SET Status='Outbid' WHERE AuctionId=X AND Status='Leading'
      INSERT INTO bids (Status='Leading', ...)
  → [DB 失敗] 回滾 Redis：重執行 Lua Script 帶舊值復原
```

**原子比較設定（Lua Script）**：
```lua
-- KEYS[1] = bid:highest:{auctionId}
-- ARGV[1] = new_amount (string), ARGV[2] = new_bidderId (string)
local current = redis.call('HGET', KEYS[1], 'amount')
if current == false or tonumber(ARGV[1]) > tonumber(current) then
  redis.call('HSET', KEYS[1], 'amount', ARGV[1], 'bidderId', ARGV[2])
  return 1
end
return 0
```

**Redis Key 設計**：
| Key 模式 | 類型 | TTL | 說明 |
|---------|------|-----|------|
| `bid:highest:{auctionId}` | Hash | 無（拍賣結束後由結標事件清除） | fields: `amount`、`bidderId` |
| `bid:ratelimit:{userId}:{auctionId}` | String | 1 秒 | 頻率限制，SET NX EX 1 |

**Redis/DB 失敗補償策略**：
| 失敗情境 | 處理方式 |
|---------|----------|
| Lua Script 回 1，DB Transaction 失敗 | 回滾 Redis：重執行 Lua Script 以舊值強制覆寫快取 |
| DB 失敗且無法取得舊快取值 | 記錄 WARN log；下次 cache miss 時從 DB `MAX(Amount)` 自動重建（FR-012 覆蓋）|
| Redis 無法存取（cache miss） | fallback 查 DB `MAX(Amount) WHERE AuctionId`，重建快取後繼續 |
| Redis 服務完全不可用 | 外拋 500，記錄 ERROR log |

**採用理由**：
- 避免每次出價都做全表 `MAX(Amount)` 掃描
- Lua Script 在 Redis 單線程執行，天然原子，無需分散式鎖
- 快取遺失時 fallback 到 DB，保證正確性
- StackExchange.Redis 是 .NET 生態最成熟的 Redis 客戶端

**考慮的替代方案**：
- `IMemoryCache`（單機記憶體快取）：水平擴展時快取不共享，不採用
- Optimistic Concurrency（DB 版本控制）：每次出價都打 DB，高競爭時效能差，不採用
- Distributed Lock（Redlock）：實作複雜，對此場景過度設計，不採用
- 應用層先讀後寫 Redis：TOCTOU 競態（兩個並發請求可能同時通過比較），不採用

---

## 決策 2：IAuctionQueryService 實作方式

**決策**：以 `NpgsqlDataSource`（原生 SQL）查詢 `auction.auctions` 表，不引用 Auction 模組的 EF DbContext。一次查詢同時取得 `Status`、`OwnerId`、`StartingPrice`、`StartTime`、`EndTime`，回傳單一 `AuctionInfoDto`（避免多次往返）。

**查詢**：
```sql
SELECT "Status", "OwnerId", "StartingPrice", "StartTime", "EndTime"
FROM auction.auctions
WHERE "Id" = @auctionId
LIMIT 1
```

**回傳型別**：
```csharp
public record AuctionInfoDto(
    AuctionStatus Status,
    Guid OwnerId,
    long StartingPrice,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime);
```

**有效出價時間判斷**（`PlaceBidCommandHandler`）：
```csharp
var now = DateTimeOffset.UtcNow;
if (now < auctionInfo.StartTime || now > auctionInfo.EndTime)
    → 409 Conflict（商品目前不在競標時間內）
```

**採用理由**：
- 符合模組隔離原則：Bidding 不可以 project reference Auction 模組
- 同一 PostgreSQL 實例下直接跨 schema 查詢效能等同本地查詢
- 一次查詢包含 `StartingPrice`，供無出價時的最低門檻比較（FR-003）
- 同時取得 `StartTime`/`EndTime`，Handler 可直接判斷現在時間是否在競標窗口內，無需額外查詢
- `NpgsqlDataSource` 已透過 DI 注冊（共用連線池）

**考慮的替代方案**：
- HTTP 呼叫 Auction API：引入網路延遲與循環依賴風險，不採用
- 引用 `Auction.csproj`：違反模組邊界，不採用
- 在 Bidding 建立 Auction 的只讀 EF DbContext（AuctionReadOnlyContext）：可行但過度複雜，raw SQL 更直接

---

## 決策 3：bid 金額資料類型

**決策**：DB 欄位改為 `bigint`（PostgreSQL），C# 類型為 `long`。FluentValidation 在 Application 層確保輸入為正整數。

**採用理由**：
- 規格要求整數出價（澄清 Q4：僅允許整數）
- `bigint` 支援最大值 9,223,372,036,854,775,807，足夠任何實際拍賣場景
- 現有 Migration 使用 `decimal(18,2)`，需要新 Migration 修改欄位類型

**Migration 策略**：
```sql
ALTER TABLE bidding.bids ALTER COLUMN "Amount" TYPE bigint USING "Amount"::bigint;
```

---

## 決策 4：BidStatus 儲存方式

**決策**：以 `string` 儲存（EF Core `HasConversion<string>()`），值：`Leading`、`Outbid`、`Won`、`Lost`。

**採用理由**：與現有 Auction 模組的 `AuctionStatus` 慣例一致（字串儲存，易讀易 debug）。

---

## 決策 5：AuctionWonEvent Handler 的事務處理

**決策**：使用 `BiddingDbContext` 的 `SaveChangesAsync` 包裝批次更新（非手動 Transaction）。

**流程**：
```csharp
// 找出得標的最高出價記錄
var winnerBid = await db.Bids
    .Where(b => b.AuctionId == e.AuctionId && b.BidderId == e.WinnerId)
    .OrderByDescending(b => b.Amount)
    .FirstOrDefaultAsync();

// 批次更新所有出價
var allBids = await db.Bids.Where(b => b.AuctionId == e.AuctionId).ToListAsync();
foreach (var bid in allBids)
    bid.SetStatus(bid.Id == winnerBid?.Id ? BidStatus.Won : BidStatus.Lost);

await db.SaveChangesAsync();

// 清除 Redis 快取（拍賣已結束）
await cacheService.RemoveHighestBidCacheAsync(e.AuctionId);
```

---

## 決策 6：替換 NullBiddingQueryService

**決策**：在 `Auction.Infrastructure/Services/` 建立 `BiddingQueryService`，以原生 SQL 查詢 `bidding.bids`。

**查詢**：
```sql
SELECT "BidderId", MAX("Amount") as "Amount"
FROM bidding.bids
WHERE "AuctionId" = @auctionId
  AND "Status" IN ('Leading', 'Won')
GROUP BY "BidderId"
ORDER BY "Amount" DESC
LIMIT 1
```

**採用理由**：完成 Bidding 模組後，`currentHighestBid` 欄位應回傳真實資料，而非 null。

---

## 決策 7：Docker Compose 新增 Redis

**決策**：在 `docker-compose.yml` 新增 Redis 6 container（port 6379）。

```yaml
redis:
  image: redis:7-alpine
  container_name: auction_redis
  ports:
    - "6379:6379"
  restart: unless-stopped
```

**連線字串**（`appsettings.Development.json`）：
```json
"Redis": "localhost:6379"
```

---

## 決策 8：出價前驗證使用者為已登入會員

**決策**：`PlaceBidCommandHandler` 在執行出價逻輯前，先透過 `IMemberQueryService` 查詢 `member.users` 表，確認法則 JWT sub（`userId`）對應的會員確實存在。

**流程轉變**：
```
PlaceBid 請求
  → [Authorize] 屬性（401 若無 JWT）
  → 頻率限制檢查（Redis SET NX TTL 1s）
  → 確認使用者為已登入會員（IMemberQueryService 查 member.users.Username）
    └─ 會員不存在 → 403 Forbidden
  → 驗證 auction 為 Active（不存在 → 404，非 Active → 409）
  → 驗證 owner ≠ bidder（擁有者 → 403）
  → 讀 Redis 取最高出價
  → 比較金額（不足 → 422）
  → DB Transaction ＋ Redis 更新
```

**查詢**：
```sql
SELECT "Username" FROM member.users WHERE "Id" = @userId LIMIT 1
```

**回傳值：**
- `string username`：並同得到 `BidderUsername`（反正規化至 `bids` 表）
- `null`：會員不存在 → Handler 拋出 403

**採用理由**：
- JWT 屬於認證機制，其 sub claim 僅表示濃縮的使用者 ID，不代表該會員仍在 Member 模組中存在
- 查詢 `member.users` 同時完成驗證與取得 `BidderUsername`，一次查詢两用，沒有額外負擔
- 与 `IAuctionQueryService` 相同作法：同一 PostgreSQL 實例跨 schema raw SQL，符合模組隢離原則

**401 vs 403 語義說明**：
- **401 Unauthorized**：JWT 不存在、過期或簽名無效。由 `[Authorize]` 屬性（JwtBearer `OnChallenge`）自動攔截，Handler 不介入。
- **403 Forbidden（會員不存在）**：JWT 有效、`userId` 解析成功，但該 `userId` 在 `member.users` 中已不存在（帳號已刪除）。Handler 主動拒絕，語義等同「擁有者不可自己出價」的 403 — 認證通過但業務規則拒絕。

**考慮的替代方案**：
- HTTP 呼叫 Member API：引入網路延遲與循環依賴風險，不採用
- 僅依賴 JWT sub，不查 DB：無法取得 `BidderUsername`，且無法處理帳號已被刪除的邊界情況，不採用

---

## 決策 9：現有型別統一（decimal → long）

**背景**：`Bidding.bids.Amount` 改為 `long`（bigint），但現有兩個參照仍使用 `decimal`：

| 位置 | 現狀 | 需要變更 |
|------|------|--------|
| `Auction.Application.Abstractions.BidInfoDto` | `decimal Amount` | 改為 `long Amount` |
| `Auction.Domain.Events.AuctionWonEvent` | `decimal SoldAmount` | 改為 `long SoldAmount` |

**決策**：在 Phase 1 修改這兩個現有原始碼檔案，統一為 `long`。

**影響範圍**：
- `AuctionEndBackgroundService`：讀取 `bidInfo.Amount` 型別隨之變為 `long`，無隱式轉型
- `AuctionWonEventHandler`：讀取 `event.SoldAmount` 型別隨之變為 `long`
- `BiddingQueryService`（Phase 5 新增）：建立時直接使用 `long`，無需轉型

**採用理由**：統一整個系統的出價金額型別，避免隱式轉型導致精度遺失或編譯器警告。
