# 快速開始：Auction 模組開發

**Branch**: `003-auction-module`  
**Date**: 2026-04-15

---

## 前置條件

```bash
# 啟動 PostgreSQL（Docker Compose）
docker compose up -d

# 確認 appsettings.Development.json 有以下設定
# ConnectionStrings__AuctionDb=Host=localhost;Port=5432;Database=auctionservice;...
# JWT Secret 用於 [Authorize] 端點測試
```

---

## 執行 Migration

```bash
# 建立 Migration（需在 repo root 執行）
dotnet ef migrations add InitAuctionSchema \
  --project src/Modules/Auction/Auction.csproj \
  --startup-project src/AuctionService.Api/AuctionService.Api.csproj \
  --output-dir Infrastructure/Persistence/Migrations

# 套用 Migration
dotnet ef database update \
  --project src/Modules/Auction/Auction.csproj \
  --startup-project src/AuctionService.Api/AuctionService.Api.csproj
```

---

## 目錄結構（預計產出）

```text
src/Modules/Auction/
├── Auction.csproj
├── Domain/
│   ├── Entities/
│   │   ├── Auction.cs
│   │   ├── AuctionImage.cs
│   │   ├── Category.cs
│   │   └── Watchlist.cs
│   └── Events/
│       └── AuctionWonEvent.cs
├── Application/
│   ├── Abstractions/
│   │   └── IBiddingQueryService.cs
│   ├── Commands/
│   │   ├── CreateAuction/
│   │   ├── UpdateAuction/
│   │   ├── AddWatchlist/
│   │   └── RemoveWatchlist/
│   ├── Queries/
│   │   ├── GetAuctions/
│   │   ├── GetAuctionDetail/
│   │   └── GetWatchlist/
│   └── DTOs/
│       └── PagedResult.cs
└── Infrastructure/
    ├── Persistence/
    │   ├── AuctionDbContext.cs
    │   ├── Configurations/
    │   └── Migrations/
    ├── BackgroundServices/
    │   └── AuctionEndBackgroundService.cs
    ├── Services/
    │   └── NullBiddingQueryService.cs
    └── DependencyInjection.cs

src/AuctionService.Api/Controllers/
├── AuctionsController.cs
└── WatchlistController.cs
```

---

## 手動測試範例

```bash
# 取得 JWT token（先登入）
TOKEN=$(curl -s -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"seller@test.com","password":"Test@1234"}' \
  | jq -r '.data.accessToken')

# 建立商品
curl -X POST http://localhost:8080/api/auctions \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "iPhone 15 Pro Max",
    "description": "全新未拆封",
    "startingPrice": 30000,
    "endTime": "2026-05-01T12:00:00Z",
    "categoryId": null,
    "imageUrls": ["https://example.com/img1.jpg"]
  }'
# 預期：201 + { "success": true, "data": { "id": "..." }, "statusCode": 201 }

# 瀏覽商品列表
curl "http://localhost:8080/api/auctions?page=1&pageSize=20"
# 預期：200 + { items: [...], totalCount: N, page: 1, pageSize: 20 }

# 關鍵字搜尋
curl "http://localhost:8080/api/auctions?q=iPhone"

# 加入追蹤清單
curl -X POST "http://localhost:8080/api/auctions/{id}/watchlist" \
  -H "Authorization: Bearer $TOKEN"
# 預期：204

# 查詢追蹤清單（含 Ended）
curl "http://localhost:8080/api/watchlist" \
  -H "Authorization: Bearer $TOKEN"

# 只看 Active 商品追蹤清單
curl "http://localhost:8080/api/watchlist?status=active" \
  -H "Authorization: Bearer $TOKEN"
```

---

## TDD 起始步驟

```bash
# 執行 Auction 模組相關測試（預期初始為 Red）
dotnet test --filter "Auction"

# 完整測試套件（確認不破壞既有測試）
dotnet test
```

### 建議實作順序

1. **Domain 先行**：`Auction.cs`（狀態機）→ `AuctionWonEvent.cs`
2. **寫紅燈測試**：`AuctionStatusMachineTests`（Active→Ended，禁止逆轉）
3. **實作 Domain**：讓狀態機測試變綠
4. **Application 抽象**：`IBiddingQueryService`、`NullBiddingQueryService`
5. **依競標者優先順序**實作 US1→US2→US3→US4→US5→US6
6. **每個 Handler 先寫單元測試（Red），再實作（Green）**

---

## 注意事項

- **Minimal API 禁止**：所有端點必須使用 `ControllerBase`
- **狀態機**：`Auction.End()` 必須檢查 `Status == Active`，否則拋出 `InvalidOperationException`
- **跨模組**：禁止直接 `using Bidding.Infrastructure.*` 或直接查詢 `bidding` schema
- **圖片驗證**：最多 5 張在 `CreateAuctionCommandValidator` 和 `UpdateAuctionCommandValidator` 中驗證
- **冪等設計**：`AddWatchlist`/`RemoveWatchlist` 使用 `UPSERT` 或先查後處理模式
- **排程服務**：`AuctionEndBackgroundService` 需要 `IServiceScopeFactory` 取得 scoped DbContext
