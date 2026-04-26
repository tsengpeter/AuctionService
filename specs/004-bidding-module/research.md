# Research: Bidding 模組

**Branch**: `004-bidding-module` | **Date**: 2026-04-27

---

## 決策 1：出價比較與狀態維護策略（Redis 快取）

**決策**：以 Redis 作為「目前最高出價」的主要快取層，`leading`/`outbid` 狀態同步更新至快取與 DB。

**架構流程**：
```
PlaceBid 請求
  → 頻率限制檢查（Redis SET NX TTL 1s）
  → 讀 auction 狀態（IAuctionQueryService → auction.auctions raw SQL）
  → 讀最高出價（BidCacheService → Redis GET bid:highest:{auctionId}）
    └─ 快取遺失 → DB MAX(Amount) WHERE AuctionId → SET cache
  → 比較金額（new > current → 繼續；否則 → 422）
  → DB Transaction：
      UPDATE bids SET Status='Outbid' WHERE AuctionId=X AND Status='Leading'
      INSERT INTO bids (Status='Leading', ...)
  → Redis SET bid:highest:{auctionId} = {amount}:{bidderId}
```

**Redis Key 設計**：
| Key 模式 | 類型 | TTL | 說明 |
|---------|------|-----|------|
| `bid:highest:{auctionId}` | String | 無（拍賣結束後由結標事件清除） | 格式：`{amount}:{bidderId}` |
| `bid:ratelimit:{userId}:{auctionId}` | String | 1 秒 | 頻率限制，SET NX EX 1 |

**採用理由**：
- 避免每次出價都做全表 `MAX(Amount)` 掃描
- 快取遺失時 fallback 到 DB，保證正確性
- Redis SET NX 原子操作實作 1 次/秒 的頻率限制，無需分散式鎖
- StackExchange.Redis 是 .NET 生態最成熟的 Redis 客戶端

**考慮的替代方案**：
- `IMemoryCache`（單機記憶體快取）：水平擴展時快取不共享，不採用
- Optimistic Concurrency（DB 版本控制）：每次出價都打 DB，高競爭時效能差，不採用
- Distributed Lock（Redlock）：實作複雜，對此場景過度設計，不採用

---

## 決策 2：IAuctionQueryService 實作方式

**決策**：以 `NpgsqlDataSource`（原生 SQL）查詢 `auction.auctions` 表，不引用 Auction 模組的 EF DbContext。

**查詢**：
```sql
SELECT "Status", "OwnerId" FROM auction.auctions WHERE "Id" = @auctionId LIMIT 1
```

**採用理由**：
- 符合模組隔離原則：Bidding 不可以 project reference Auction 模組
- 同一 PostgreSQL 實例下直接跨 schema 查詢效能等同本地查詢
- 不引入額外 HTTP 呼叫，降低複雜度與延遲
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

**考慮的替代方案**：
- HTTP 呼叫 Member API：引入網路延遲與循環依賴風險，不採用
- 僅依賴 JWT sub，不查 DB：無法取得 `BidderUsername`，且無法處理帳號已被刺除的邊界情況，不採用
