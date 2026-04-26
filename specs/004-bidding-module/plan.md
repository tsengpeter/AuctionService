# 實作計畫：Bidding 模組

**Branch**: `004-bidding-module` | **Date**: 2026-04-27 | **Spec**: [spec.md](./spec.md)  
**Input**: Feature specification from `specs/004-bidding-module/spec.md`

## 摘要

實作 Bidding 模組，允許已登入使用者對 Active 拍賣商品出價。出價比較以 Redis 快取為主，非同步持久化至 PostgreSQL。核心功能包含：出價（含頻率限制、所有者禁止出價、金額驗證）、查詢商品出價歷史（分頁）、查詢我的出價清單（含 leading/outbid/won/lost 狀態），以及監聽 `AuctionWonEvent` 批次更新結標狀態。同時替換 Auction 模組現有的 `NullBiddingQueryService`，改為查詢 Bidding 模組真實資料。

## Technical Context

**Language/Version**: C# 13 / .NET 10  
**Primary Dependencies**: MediatR 12.x、FluentValidation 12.x、EF Core 10 (Npgsql)、StackExchange.Redis、Microsoft.Extensions.Caching.StackExchangeRedis  
**Storage**: PostgreSQL 16（schema: `bidding`，table: `bids`）+ Redis（快取 leading/outbid 狀態與頻率限制）  
**Testing**: xUnit 2.9、Testcontainers.PostgreSql 4.x、Testcontainers.Redis、FluentAssertions 8.x、NSubstitute 5.x  
**Target Platform**: Linux server（Docker）  
**Project Type**: 模組化單體（Modular Monolith）  
**Performance Goals**: 有效出價 2 秒內完成（p95 < 200ms API 回應）  
**Constraints**: 每使用者每商品每秒最多 1 次出價；`bids` 表金額為正整數；跨模組無 EF Navigation  
**Scale/Scope**: 與現有 Member / Auction 模組整合；替換 `NullBiddingQueryService`

## Constitution Check

*GATE：在 Phase 0 之前必須通過。Phase 1 設計後重新檢核。*

| 原則 | 狀態 | 說明 |
|------|------|------|
| I. Code Quality First | ✅ | 私有建構子 + 靜態工廠；DI 注入介面 |
| II. TDD (NON-NEGOTIABLE) | ✅ | 所有 Handler 先寫 Red tests，再實作 Green |
| III. User Experience Consistency | ✅ | 統一 `ApiResponse<T>` 回應；HTTP 422/409/403/429 對應業務語義 |
| IV. Performance Requirements | ✅ | Redis 快取避免每次出價全掃 DB；`INDEX(AuctionId, Amount DESC)` |
| V. Observability and Monitoring | ✅ | Structured logging 記錄出價行為、快取命中/遺失、結標事件 |
| Documentation Language | ✅ | 本文件以繁體中文撰寫（zh-TW） |

**GATE 結果**：✅ 全部通過，可進入 Phase 0。

## Project Structure

### 文件（本 Feature）

```text
specs/004-bidding-module/
├── plan.md          ← 本文件
├── research.md      ← Phase 0 產出
├── data-model.md    ← Phase 1 產出
├── quickstart.md    ← Phase 1 產出
├── contracts/
│   └── openapi.yaml ← Phase 1 產出
└── tasks.md         ← /speckit.tasks 產出（尚未建立）
```

### 原始碼（本 Feature 新增 / 修改）

```text
src/
├── Modules/
│   ├── Bidding/
│   │   ├── Bidding.csproj                         # 新增 Redis NuGet 套件
│   │   ├── Application/
│   │   │   ├── Abstractions/
│   │   │   │   ├── IAuctionQueryService.cs         # 新增
│   │   │   │   └── IMemberQueryService.cs          # 新增
│   │   │   ├── Commands/
│   │   │   │   └── PlaceBid/
│   │   │   │       ├── PlaceBidCommand.cs          # 新增
│   │   │   │       ├── PlaceBidCommandHandler.cs   # 新增
│   │   │   │       └── PlaceBidCommandValidator.cs # 新增
│   │   │   ├── Queries/
│   │   │   │   ├── GetAuctionBids/
│   │   │   │   │   ├── GetAuctionBidsQuery.cs      # 新增
│   │   │   │   │   ├── GetAuctionBidsQueryHandler.cs # 新增
│   │   │   │   │   └── BidDto.cs                  # 新增
│   │   │   │   └── GetMyBids/
│   │   │   │       ├── GetMyBidsQuery.cs           # 新增
│   │   │   │       ├── GetMyBidsQueryHandler.cs    # 新增
│   │   │   │       └── MyBidDto.cs                # 新增
│   │   │   ├── EventHandlers/
│   │   │   │   └── AuctionWonEventHandler.cs      # 新增
│   │   │   ├── DTOs/
│   │   │   │   └── PagedResult.cs                 # 新增（複製自 Auction 模組）
│   │   │   └── DependencyInjection.cs             # 修改（加入 Redis、FluentValidation）
│   │   ├── Domain/
│   │   │   ├── Entities/
│   │   │   │   └── Bid.cs                         # 修改（加入 BidStatus、BidderUsername）
│   │   │   └── Enums/
│   │   │       └── BidStatus.cs                   # 新增
│   │   └── Infrastructure/
│   │       ├── Cache/
│   │       │   └── BidCacheService.cs             # 新增（Redis 操作封裝）
│   │       ├── Services/
│   │       │   ├── AuctionQueryService.cs         # 新增（IAuctionQueryService 實作）
│   │       │   └── MemberQueryService.cs          # 新增（IMemberQueryService 實作）
│   │       └── Persistence/
│   │           ├── BiddingDbContext.cs            # 修改（加入 BidStatus、BidderUsername 配置）
│   │           ├── Configurations/
│   │           │   └── BidConfiguration.cs       # 新增
│   │           └── Migrations/
│   │               └── <timestamp>_AddBidStatusAndBidder.cs # 新增 migration
│   └── Auction/
│       └── Infrastructure/
│           └── Services/
│               └── BiddingQueryService.cs         # 新增（替換 NullBiddingQueryService）
├── AuctionService.Api/
│   └── Controllers/
│       └── BidsController.cs                     # 新增

tests/
├── AuctionService.UnitTests/
│   └── Bidding/
│       ├── PlaceBidCommandTests.cs               # 新增
│       ├── GetAuctionBidsQueryTests.cs           # 新增
│       ├── GetMyBidsQueryTests.cs                # 新增
│       └── AuctionWonEventHandlerTests.cs        # 新增
└── AuctionService.IntegrationTests/
    └── Bidding/
        ├── PlaceBidIntegrationTests.cs           # 新增
        ├── GetAuctionBidsIntegrationTests.cs     # 新增
        └── GetMyBidsIntegrationTests.cs          # 新增
```

**Structure Decision**: 沿用現有模組化單體慣例。`BidCacheService` 封裝所有 Redis 操作，`AuctionQueryService` 以原生 SQL 查詢 `auction.auctions` 表，避免跨模組 EF Navigation。

## 實作計畫

### Phase 1 — Domain 層強化

**目標**：在現有 `Bid` 實體加入 `BidStatus`（enum）與 `BidderUsername`（string），`Amount` 改為 `long`（整數）。

**任務**：
1. 新增 `BidStatus.cs`（enum：Leading / Outbid / Won / Lost）
2. 修改 `Bid.cs`：加入 `Status`、`BidderUsername`；`Amount` 型別改為 `long`；更新靜態工廠 `Place()`
3. 更新 `BiddingDbContext.cs`：新增 `Status`（string 轉換）、`BidderUsername` 欄位；修改 `Amount` 為 `bigint`；補 `INDEX(AuctionId, Amount DESC)` 與 `INDEX(BidderId)`
4. 新增 EF Migration：`AddBidStatusAndBidder`

### Phase 2 — Application 層

**目標**：定義所有介面、CQRS handlers、FluentValidation validators、AuctionWonEvent handler。

**任務**：
1. 新增 `IAuctionQueryService`（`GetAuctionStatusAsync` / `GetAuctionOwnerIdAsync`）
2. 新增 `IMemberQueryService`（`GetMemberUsernameAsync`：確認會員存在並取得顯示名稱）
3. 新增 `PlaceBidCommand` + `Validator`（驗證：amount > 0 整數；由 Handler 執行業務規則）
4. 新增 `PlaceBidCommandHandler`（流程：頻率限制 → **確認使用者為已登入會員** → 驗證 auction Active → 驗證 owner ≠ bidder → 讀 Redis 取最高出價 → 比較 → 更新 Redis → DB 寫入）
5. 新增 `GetAuctionBidsQuery` + `Handler`（offset 分頁，bidTime 降序）
6. 新增 `GetMyBidsQuery` + `Handler`（計算每筆 leading/outbid/won/lost）
7. 新增 `AuctionWonEventHandler`：批次 UPDATE bids（WinnerId → Won，其餘 auctionId → Lost）
8. 更新 `DependencyInjection.cs`：加入 `AddValidatorsFromAssembly`、Redis、`IAuctionQueryService`、`IMemberQueryService`

### Phase 3 — Infrastructure 層

**目標**：實作 Redis 快取服務、`IAuctionQueryService`、`IMemberQueryService`（原生 SQL 查詢跨 schema）。

**任務**：
1. 新增 `BidCacheService`：  
   - `GetHighestBidAsync(auctionId)` → Redis GET `bid:highest:{auctionId}`，缺失則從 DB `MAX(Amount)` 重建  
   - `SetHighestBidAsync(auctionId, amount, bidderId)` → Redis SET with no expiry  
   - `CheckRateLimitAsync(userId, auctionId)` → Redis SET NX TTL 1s（回傳是否允許）
2. 新增 `AuctionQueryService`：注入 `NpgsqlDataSource`，執行  
   `SELECT "Status", "OwnerId" FROM auction.auctions WHERE "Id" = @auctionId`
3. 新增 `MemberQueryService`：注入 `NpgsqlDataSource`，執行  
   `SELECT "Username" FROM member.users WHERE "Id" = @userId LIMIT 1`  
   → 回傳 `null` 表示會員不存在（Handler 回 403 Forbidden）
4. Bidding.csproj 加入 NuGet：`StackExchange.Redis`、`Microsoft.Extensions.Caching.StackExchangeRedis`、`Npgsql`

### Phase 4 — API 層

**目標**：新增 `BidsController`，實作 3 個端點。

| Method | Path | Auth | 說明 |
|--------|------|------|------|
| POST | `/api/auctions/{auctionId}/bids` | JWT | 出價 |
| GET | `/api/auctions/{auctionId}/bids` | 無需 | 出價歷史（分頁） |
| GET | `/api/bids/my` | JWT | 我的出價清單 |

### Phase 5 — 替換 NullBiddingQueryService

**目標**：Auction 模組的 `GetAuctionDetailQueryHandler` 目前使用 `NullBiddingQueryService`（永遠回傳 null），替換為查詢 `bidding.bids` 表的真實實作。

**任務**：
1. 在 `Auction.Infrastructure/Services/` 新增 `BiddingQueryService`，以原生 SQL 查詢 `bidding.bids`：  
   `SELECT "BidderId", MAX("Amount") FROM bidding.bids WHERE "AuctionId" = @auctionId GROUP BY "BidderId" ORDER BY MAX("Amount") DESC LIMIT 1`
2. 更新 Auction 模組 DI，以 `BiddingQueryService` 取代 `NullBiddingQueryService`
3. 確認現有整合測試仍全綠

### Phase 6 — 測試

**單元測試**：

| 測試類別 | 測試案例 |
|---------|---------|
| `PlaceBidCommandTests` | 有效出價成功；出價 ≤ 最高出價 → 422；商品非 Active → 409；自己對自己 → 403；超過頻率限制 → 429；快取遺失時從 DB 重建 |
| `GetAuctionBidsQueryTests` | 正確分頁；依 bidTime 降序；空結果 |
| `GetMyBidsQueryTests` | leading/outbid/won/lost 狀態正確計算 |
| `AuctionWonEventHandlerTests` | WinnerId → Won；其餘 → Lost；無出價時不報錯 |

**整合測試**（Testcontainers.PostgreSql + Testcontainers.Redis）：

| 測試類別 | 測試案例 |
|---------|---------|
| `PlaceBidIntegrationTests` | 出價寫入 DB；Redis 快取更新；並發出價只有 1 筆成功 |
| `GetAuctionBidsIntegrationTests` | 正確排序與分頁 |
| `GetMyBidsIntegrationTests` | 跨多商品狀態正確 |

## 錯誤對應

| 情況 | HTTP | 說明 |
|------|------|------|
| 出價 ≤ 當前最高出價 | 422 | body 含 `currentHighestBid` 欄位 |
| 商品非 Active 狀態 | 409 | Conflict |
| 擁有者對自己商品出價 | 403 | Forbidden |
| 使用者不是已登入會員（Member 不存在） | 403 | Forbidden |
| 超過頻率限制（每秒 > 1 次） | 429 | Too Many Requests |
| 商品不存在 | 404 | Not Found |
| 未認證（保護端點） | 401 | Unauthorized |

## 依賴確認

| 依賴項目 | 狀態 |
|---------|------|
| `AuctionWonEvent`（Auction.Domain.Events） | ✅ 已存在 |
| `BiddingDbContext`（bidding schema、bids table） | ✅ 已存在（需 migration 補欄位）|
| `BiddingDependencyInjection.cs` | ✅ 已存在（需更新）|
| `IBiddingQueryService`（Auction.Application.Abstractions） | ✅ 已存在 |
| `NullBiddingQueryService`（Auction.Infrastructure） | ✅ 已存在（Phase 5 替換）|
| `member.users` 表（Member schema） | ✅ 已存在（`MemberQueryService` 以 raw SQL 查詢 `Username`）|
| Redis（docker-compose.yml） | ⚠️ 需新增至 docker-compose.yml |

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |
