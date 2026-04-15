# Implementation Plan: Auction 模組 — 商品拍賣、追蹤清單與排程結標

**Branch**: `003-auction-module` | **Date**: 2026-04-15 | **Spec**: [spec.md](./spec.md)  
**Input**: Feature specification from `/specs/003-auction-module/spec.md`

## Summary

實作 Auction 模組的完整拍賣商品管理功能：賣家建立拍賣商品（建立即 Active，無草稿步驟）、編輯上架商品（僅限非競標敏感欄位：title/description/category/images）、瀏覽商品列表（關鍵字搜尋僅比對 title、分類篩選、分頁 20 筆）、商品詳情（含目前最高出價，由 `IBiddingQueryService` null stub 提供）、競標者加入/移除追蹤清單（冪等）、查詢追蹤清單（預設含 Active+Ended，`?status=active` 篩選）、`AuctionEndBackgroundService` 排程結標（每 60 秒掃描，批次上限 100 筆，發布 `AuctionWonEvent`）。狀態機：`Active → Ended`（無 Draft，無逆轉）。

## Technical Context

**Language/Version**: C# 13 / .NET 10  
**Primary Dependencies**: MediatR 12.x、FluentValidation 12.x、EF Core 10 + Npgsql、Microsoft.AspNetCore.Authentication.JwtBearer 10  
**Storage**: PostgreSQL 16，schema: `auction`，tables: `auctions`、`auction_images`、`categories`、`watchlist`  
**Testing**: xUnit 2.9 + Testcontainers.PostgreSql 4.x + FluentAssertions 8.x + NSubstitute 5.x  
**Target Platform**: Linux server（Docker）  
**Project Type**: Modular Monolith Web API  
**Performance Goals**: 商品列表查詢 p95 ≤ 500ms（10,000 筆 Active 商品），其餘端點 p95 ≤ 200ms  
**Constraints**: 結標排程誤差 ≤ 2 分鐘；IBiddingQueryService 本 Phase 為 null stub；跨模組僅透過 MediatR 事件溝通  
**Scale/Scope**: 初期 ≤ 50,000 筆商品，單節點部署，結標批次上限 100 筆/次

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| 原則 | 狀態 | 說明 |
|------|------|------|
| I. Code Quality First | ✅ PASS | 私有建構子 + 靜態工廠 `Create()`；`End()` 方法封裝狀態轉換；IBiddingQueryService 介面隔離跨模組依賴；域邏輯不外洱至 Handler |
| II. TDD（NON-NEGOTIABLE） | ✅ PASS | 所有 Handler 先寫單元測試（NSubstitute mock），再寫實作；AuctionEndBackgroundService 以整合測試驗證；狀態機以單元測試全覆蓋 |
| III. UX Consistency | ✅ PASS | 所有回應使用 `ApiResponse<T>` 包裝；驗證錯誤 422 含 field/message；403/409/404 語義正確 |
| IV. Performance | ✅ PASS | INDEX(status, end_time) 加速結標掃描；INDEX(owner_id)；watchlist UNIQUE(user_id, auction_id)；列表查詢使用 offset 分頁 + projection |
| V. Observability | ✅ PASS | AuctionEndBackgroundService 的 catch 區塊使用 ILogger 記錄例外；Handler 層記錄關鍵操作 |
| Documentation Language (zh-TW) | ✅ PASS | plan.md / data-model.md / quickstart.md 均以繁體中文撰寫 |

**結論**: 無 Constitution 違規，所有 Gate 通過，可進行實作。

## Project Structure

### Documentation (this feature)

```text
specs/003-auction-module/
├── plan.md          ✅ 本檔案
├── research.md      ✅ Phase 0 完成
├── data-model.md    ✅ Phase 1 完成
├── quickstart.md    ✅ Phase 1 完成
├── contracts/
│   └── openapi.yaml ✅ Phase 1 完成
└── tasks.md         ⏳ 待 /speckit.tasks 產出
```

### Source Code (repository root)

```text
src/Modules/Auction/
├── Auction.csproj
├── Domain/
│   ├── Entities/
│   │   ├── Auction.cs              # 主實體（AuctionStatus enum 內含）
│   │   ├── AuctionImage.cs         # Owned Entity（依附於 Auction）
│   │   ├── Category.cs             # 分類實體
│   │   └── Watchlist.cs            # 追蹤清單實體
│   └── Events/
│       └── AuctionWonEvent.cs      # IDomainEvent
├── Application/
│   ├── Abstractions/
│   │   └── IBiddingQueryService.cs
│   ├── Commands/
│   │   ├── CreateAuction/
│   │   │   ├── CreateAuctionCommand.cs
│   │   │   ├── CreateAuctionCommandHandler.cs
│   │   │   └── CreateAuctionCommandValidator.cs
│   │   ├── UpdateAuction/
│   │   │   ├── UpdateAuctionCommand.cs
│   │   │   ├── UpdateAuctionCommandHandler.cs
│   │   │   └── UpdateAuctionCommandValidator.cs
│   │   ├── AddWatchlist/
│   │   │   ├── AddWatchlistCommand.cs
│   │   │   └── AddWatchlistCommandHandler.cs
│   │   └── RemoveWatchlist/
│   │       ├── RemoveWatchlistCommand.cs
│   │       └── RemoveWatchlistCommandHandler.cs
│   ├── Queries/
│   │   ├── GetAuctions/
│   │   │   ├── GetAuctionsQuery.cs
│   │   │   ├── GetAuctionsQueryHandler.cs
│   │   │   └── AuctionListItemDto.cs
│   │   ├── GetAuctionDetail/
│   │   │   ├── GetAuctionDetailQuery.cs
│   │   │   ├── GetAuctionDetailQueryHandler.cs
│   │   │   └── AuctionDetailDto.cs
│   │   └── GetWatchlist/
│   │       ├── GetWatchlistQuery.cs
│   │       ├── GetWatchlistQueryHandler.cs
│   │       └── WatchlistItemDto.cs
│   └── DTOs/
│       └── PagedResult.cs
└── Infrastructure/
    ├── Persistence/
    │   ├── AuctionDbContext.cs
    │   ├── Configurations/
    │   │   ├── AuctionConfiguration.cs
    │   │   ├── AuctionImageConfiguration.cs
    │   │   ├── CategoryConfiguration.cs
    │   │   └── WatchlistConfiguration.cs
    │   └── Migrations/
    ├── BackgroundServices/
    │   └── AuctionEndBackgroundService.cs
    ├── Services/
    │   └── NullBiddingQueryService.cs   # IBiddingQueryService stub
    └── DependencyInjection.cs

src/AuctionService.Api/Controllers/
├── AuctionsController.cs     # /api/auctions/*
└── WatchlistController.cs    # /api/watchlist

tests/AuctionService.UnitTests/Auction/
├── Domain/
│   └── AuctionStatusMachineTests.cs
└── Application/
    ├── CreateAuctionCommandTests.cs
    ├── UpdateAuctionCommandTests.cs
    ├── GetAuctionsQueryTests.cs
    ├── GetWatchlistQueryTests.cs
    ├── AddWatchlistCommandTests.cs
    └── AuctionEndServiceTests.cs

tests/AuctionService.IntegrationTests/Auction/
```

**Structure Decision**: Modular Monolith — Auction 模組獨立 C# 專案，與 Bidding 模組無 EF 跨模組依賴（透過 `IBiddingQueryService` 介面隔離）；Controller 位於 API 層，Handler 位於 Application 層。

## Implementation Phases

| Phase | 內容 | 目標 |
|-------|------|------|
| 1 | Setup | 移除舊式 AuctionItem scaffold，重構 AuctionDbContext |
| 2 | Domain | Auction、AuctionImage、Category、Watchlist 實體；AuctionWonEvent |
| 3 | Application Abstractions | IBiddingQueryService 介面；PagedResult DTO；NullBiddingQueryService |
| 4 | US1 GetAuctions | 商品列表查詢（關鍵字/分類/分頁，僅 Active） |
| 5 | US2 GetAuctionDetail | 商品詳情（含 null 最高出價） |
| 6 | US3 Watchlist | 加入/移除/查詢追蹤清單（含 ?status=active 篩選） |
| 7 | US4 AuctionEndBackgroundService | 排程結標 + AuctionWonEvent 發布 |
| 8 | US5 CreateAuction | 建立商品（直接 Active，Validator） |
| 9 | US6 UpdateAuction | 編輯商品（欄位限制，403/409） |
| 10 | EF Migration | auction schema migration（含 index） |
| 11 | Polish | Integration tests、覆蓋率門檻、Logging |
