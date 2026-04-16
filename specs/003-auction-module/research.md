# 技術研究報告：Auction 模組

**Branch**: `003-auction-module`  
**Date**: 2026-04-15  
**Spec**: [spec.md](./spec.md)

---

## 1. 狀態機設計：Active → Ended（無 Draft）

**Decision**: 建立即 Active，單向狀態機 `Active → Ended`，不支援草稿或取消  
**Rationale**:
- 移除 Draft 狀態降低賣家操作複雜度（建立即上架，符合快速交易需求）
- 結標後商品永久進入 Ended，禁止逆轉（保護競標公平性）
- 狀態只有兩個，Domain 層邏輯更簡潔、測試更容易全覆蓋

**Implementation**:
```csharp
public enum AuctionStatus { Active, Ended }

public class Auction : BaseEntity
{
    public AuctionStatus Status { get; private set; } = AuctionStatus.Active;

    public static Auction Create(string title, decimal startingPrice,
        DateTimeOffset endTime, Guid ownerId, ...)
    {
        // 建立即 Active
        return new Auction { Status = AuctionStatus.Active, ... };
    }

    public void End(Guid? winnerId, decimal? soldAmount)
    {
        if (Status != AuctionStatus.Active)
            throw new InvalidOperationException("Auction is not active.");
        Status = AuctionStatus.Ended;
        WinnerId = winnerId;
        SoldAmount = soldAmount;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
```

**Alternatives Considered**:
- `Draft → Active → Ended`：增加上架步驟，被使用者明確拒絕（Clarify Q1）
- `Active → Ended → Sold`：MarkSold 步驟多餘，結標時已知成交金額，合併至 End()

---

## 2. 跨模組最高出價查詢：IBiddingQueryService（null stub）

**Decision**: 定義 `IBiddingQueryService` 介面於 Application/Abstractions，本 Phase 以 `NullBiddingQueryService` 實作（永遠回傳 null）  
**Rationale**:
- 避免 Auction 模組直接依賴 Bidding 模組的 `DbContext` 或 EF 實體，維持模組邊界
- Null stub 讓 Auction 模組可以獨立實作與測試，Bidding 模組完成後替換實作，無需修改 Auction 層
- 介面定義清楚，未來 Bidding 模組只需實作同一介面並在 DI 中覆蓋

**Implementation**:
```csharp
// Application/Abstractions/IBiddingQueryService.cs
public interface IBiddingQueryService
{
    Task<decimal?> GetHighestBidAsync(Guid auctionId, CancellationToken ct = default);
}

// Infrastructure/Services/NullBiddingQueryService.cs
public class NullBiddingQueryService : IBiddingQueryService
{
    public Task<decimal?> GetHighestBidAsync(Guid auctionId, CancellationToken ct = default)
        => Task.FromResult<decimal?>(null);
}
```

**Alternatives Considered**:
- 直接查詢 `bidding.bids` 表（跨 schema EF 查詢）：違反模組邊界原則，被明確排除
- HTTP 內部呼叫：過度設計，Monolith 架構不需要

---

## 3. 排程結標：PeriodicTimer + IHostedService（catch-log-continue）

**Decision**: `AuctionEndBackgroundService` 繼承 `BackgroundService`，使用 `PeriodicTimer(TimeSpan.FromSeconds(60))` 每 60 秒掃描，批次上限 100 筆，catch 例外記錄錯誤日誌後繼續  
**Rationale**:
- `PeriodicTimer` 是 .NET 6+ 推薦的週期性背景任務方式，比 `Task.Delay` 更精確
- 批次上限 100 筆避免單次處理時間過長，符合 SC-003（結標誤差 ≤ 2 分鐘）
- catch-log-continue 策略：DB 連線中斷等暫時性錯誤不會導致服務崩潰，下次循環自動重試（冪等設計：`Status = Active AND EndTime <= UtcNow`，不會重複結標）

**Implementation**:
```csharp
public class AuctionEndBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuctionEndBackgroundService> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(60));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try { await ProcessEndedAuctionsAsync(stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "Error processing ended auctions"); }
        }
    }

    private async Task ProcessEndedAuctionsAsync(CancellationToken ct)
    {
        // IServiceScopeFactory 取得 scoped DbContext
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        var auctions = await db.Auctions
            .Where(a => a.Status == AuctionStatus.Active && a.EndTime <= DateTimeOffset.UtcNow)
            .Take(100)
            .ToListAsync(ct);

        foreach (var auction in auctions)
        {
            // 取最高出價（null stub，未來由 Bidding 模組補充）
            // ...
            auction.End(winnerId: null, soldAmount: null);
        }
        await db.SaveChangesAsync(ct);
        // 發布 AuctionWonEvent（有出價者）
    }
}
```

**Alternatives Considered**:
- Hangfire / Quartz.NET：功能豐富但引入額外依賴，本專案不需要複雜排程
- `Task.Delay(60_000, ct)`：時間漂移問題，`PeriodicTimer` 更準確

---

## 4. 可編輯欄位限制（Active 商品）

**Decision**: Active 商品僅允許修改 `Title`、`Description`、`CategoryId`、`Images`；`StartingPrice` 和 `EndTime` 禁止修改（422）  
**Rationale**:
- 更改起標價或結標時間會影響已出價者的決策，屬於競標不公平行為
- UpdateAuctionCommand 的 Validator 應明確拒絕包含這兩個欄位的請求
- Domain 層的 `Update()` 方法只接受允許的欄位，避免意外修改

**Alternatives Considered**:
- 允許賣家修改所有欄位（若尚無出價）：增加複雜度，被使用者明確拒絕（Clarify Q2）

---

## 5. 關鍵字搜尋：title-only LIKE

**Decision**: `q` 參數僅以 `LIKE %keyword%` 匹配 `title` 欄位，不搜尋 `description`  
**Rationale**:
- 標題匹配精準度優先：搜尋結果相關性更高，避免 description 含大量文字導致不相關結果
- EF Core：`a.Title.Contains(q)`（產生 `LIKE %q%`）
- 若未來需要全文搜尋，可升級為 PostgreSQL `tsvector` + GIN index，不影響現有介面

**Alternatives Considered**:
- 搜尋 title + description：被使用者明確拒絕（Clarify Q3），影響搜尋精準度

---

## 6. 追蹤清單顯示行為：預設顯示全部 + `?status=active` 篩選

**Decision**: `GET /api/watchlist` 預設回傳全部追蹤商品（含 Active 和 Ended），提供 `?status=active` 篩選參數  
**Rationale**:
- 讓競標者知道追蹤商品的結果（是否得標），避免商品「消失」造成困惑
- `?status=active` 是可選的便利篩選，而非強制行為，保留使用者自主性
- Status 欄位包含在回應中，前端可據此差異化顯示（如已結標商品灰色顯示）

**Implementation**:
```csharp
// GetWatchlistQuery
public record GetWatchlistQuery(Guid UserId, string? Status) : IRequest<List<WatchlistItemDto>>;

// Handler
var query = db.Watchlist
    .Where(w => w.UserId == request.UserId)
    .Join(db.Auctions, w => w.AuctionId, a => a.Id, (w, a) => a);

if (request.Status?.ToLower() == "active")
    query = query.Where(a => a.Status == AuctionStatus.Active);
```

---

## 7. EF Core：Owned Entity vs 獨立表（AuctionImage）

**Decision**: `AuctionImage` 作為 `Auction` 的 **Owned Entity**（`OwnsMany`），儲存於獨立的 `auction_images` 表  
**Rationale**:
- `OwnsMany` 語義清楚（圖片生命週期完全依附於商品），無獨立的 Repository
- 每次載入商品詳情時同時載入圖片，無 N+1 問題（EF Core `OwnsMany` 預設 join）
- 最多 5 張的限制在 Domain 層 `Create()` / `Update()` 時驗證，不依賴 DB 約束

**Alternatives Considered**:
- 獨立實體 + Navigation Property：需要 Repository，增加複雜度，超出必要
