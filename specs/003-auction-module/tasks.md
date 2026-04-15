# Tasks: Auction 模組 — 商品拍賣、追蹤清單與排程結標

**Branch**: `003-auction-module`  
**Input**: `specs/003-auction-module/` (plan.md, spec.md, data-model.md, contracts/openapi.yaml, research.md, quickstart.md)  
**Baseline Tests**: 185 (Unit 144 + Integration 41), all green  
**TDD**: 每個 User Story 的測試任務 **必須** 先寫並確認 FAIL 後再實作

## Format: `[ID] [P?] [Story] Description`

- **[P]**: 可平行執行（不同檔案，無未完成依賴）
- **[Story]**: 對應的 User Story（US1–US6）
- **Setup/Foundational/Polish**: 無 Story 標籤

---

## Phase 1: Setup（清除 Scaffold 舊設計，建立新基礎）

**Purpose**: 將現有 scaffold 實體（Pending/Active/Ended/Sold/Cancelled 狀態機）全面重構為規格要求的 Active/Ended 設計，並新增缺少的實體與基礎設施。

> ⚠️ 現有 `AuctionItem.cs`、`AuctionDbContext.cs`、相關測試需全部重構，不可保留舊狀態機邏輯。

- [ ] T001 重構並重命名 `src/Modules/Auction/Domain/Entities/AuctionItem.cs` → `Auction.cs`，類別名稱 `AuctionItem` → `Auction`：移除 Pending/Sold/Cancelled 狀態，改為 `Active/Ended` 兩態；`Create()` 直接建立 Active；`End(Guid? winnerId, decimal? soldAmount)` 接受可空參數；新增 OwnerId、Description、CategoryId、WinnerId、SoldAmount、EndTime 屬性（H3 修正：與 data-model.md 命名對齊）
- [ ] T002 [P] 新增 `src/Modules/Auction/Domain/Entities/AuctionImage.cs`：OwnedEntity 值物件含 Url(string)、DisplayOrder(int)
- [ ] T003 [P] 新增 `src/Modules/Auction/Domain/Entities/Category.cs`：含 Id(Guid)、Name(string)、ParentId(Guid?) 屬性，私有建構子 + 靜態 `Create()`
- [ ] T004 [P] 新增 `src/Modules/Auction/Domain/Entities/Watchlist.cs`：含 Id(Guid)、UserId(Guid)、AuctionId(Guid)、AddedAt(DateTime) 屬性，私有建構子 + 靜態 `Create()`
- [ ] T005 [P] 新增 `src/Modules/Auction/Domain/Events/AuctionWonEvent.cs`：實作 `IDomainEvent`，含 AuctionId、WinnerId、SoldAmount、SellerId 屬性（readonly）
- [ ] T006 [P] 新增 `src/Modules/Auction/Application/Abstractions/IBiddingQueryService.cs`：定義 `record BidInfoDto(Guid WinnerId, decimal Amount)`；定義 `Task<BidInfoDto?> GetHighestBidAsync(Guid auctionId, CancellationToken ct)` 介面（C1 修正：回傳 BidInfoDto? 同時含 WinnerId 與 Amount，供結標背景服務使用）
- [ ] T007 [P] 新增 `src/Modules/Auction/Infrastructure/Services/NullBiddingQueryService.cs`：實作 `IBiddingQueryService`，`GetHighestBidAsync()` 永遠回傳 `null`（即 BidInfoDto? = null，Phase stub，Bidding 模組完成後替換）

---

## Phase 2: Foundational（EF Core、Migration、DI）

**Purpose**: 建立資料層基礎，所有 User Story 的實作均依賴此 Phase 完成。

**⚠️ CRITICAL**: 直到此 Phase 完成前，任何 User Story 均無法進行資料庫操作。

- [ ] T008 更新 `src/Modules/Auction/Infrastructure/Persistence/AuctionDbContext.cs`：新增 `DbSet<Category> Categories`、`DbSet<Watchlist> Watchlist`；移除舊的 `AuctionItems` DbSet，改為 `Auctions`（Entity: `Auction`，對應 T001 重命名）；引入 Configurations
- [ ] T009 [P] 新增 `src/Modules/Auction/Infrastructure/Persistence/Configurations/AuctionConfiguration.cs`：Table `auctions`、schema `auction`；Status 存為 string；INDEX(status, end_time)；INDEX(owner_id)；Owned Entity `AuctionImages`（最多5筆） 
- [ ] T010 [P] 新增 `src/Modules/Auction/Infrastructure/Persistence/Configurations/AuctionImageConfiguration.cs`：Owned entity 設定，Table `auction_images`，Url VARCHAR(500) required
- [ ] T011 [P] 新增 `src/Modules/Auction/Infrastructure/Persistence/Configurations/CategoryConfiguration.cs`：Table `categories`，Name VARCHAR(100) required
- [ ] T012 [P] 新增 `src/Modules/Auction/Infrastructure/Persistence/Configurations/WatchlistConfiguration.cs`：Table `watchlist`，UNIQUE INDEX(user_id, auction_id)
- [ ] T013 新增 `src/Modules/Auction/Application/DTOs/PagedResult.cs`：泛型 `PagedResult<T>` 含 Items、TotalCount、Page、PageSize、TotalPages
- [ ] T014 更新 `src/Modules/Auction/Application/DependencyInjection.cs`：註冊 `IBiddingQueryService → NullBiddingQueryService`（`AddScoped`）；確保 MediatR 掃描 Auction assembly
- [ ] T015 執行 EF Migration：`dotnet ef migrations add AuctionModuleFullSchema --project src/Modules/Auction --startup-project src/AuctionService.Api --output-dir Infrastructure/Persistence/Migrations`（刪除舊兩個 scaffold migration 或新增 migration 覆蓋）
- [ ] T015a 在 `tests/AuctionService.IntegrationTests/Infrastructure/` WebApplicationFactory 的 `InitializeAsync` 中新增 `categories` 表 Seed 資料（至少 5 筆測試分類，含確定的 Id），供所有 Auction Integration Tests 使用（M1 修正：提前至 Phase 2，確保 T017 分類篩選測試可正確執行）

**Checkpoint**: `dotnet test` 通過（舊測試需隨 T001 重構同步更新），DB schema 正確建立，categories seed 資料就位

---

## Phase 3: User Story 1 — 瀏覽商品列表與搜尋 (Priority: P1) 🎯 MVP

**Goal**: 任何用戶（含未登入訪客）可瀏覽 Active 商品、依分類篩選、關鍵字搜尋（title LIKE）、分頁（pageSize=20）。  
**Public endpoint**: `GET /api/auctions` — 無需 JWT。

**Independent Test**: 預先種入數筆 Active 商品後，無條件查詢、分類篩選、關鍵字搜尋三種情境均回傳正確結果及分頁資訊。

### Tests for User Story 1 ⚠️ 先寫測試並確認 FAIL

- [ ] T016 [P] [US1] 撰寫 `tests/AuctionService.UnitTests/Auction/Application/GetAuctionsQueryHandlerTests.cs`：測試無條件查詢回傳分頁結果、關鍵字篩選、分類篩選、結果為空時 HTTP 200 + empty array、Ended 商品不出現在結果中（共 5 個測試用例）
- [ ] T017 [P] [US1] 撰寫 `tests/AuctionService.IntegrationTests/Auction/GetAuctionsIntegrationTests.cs`：透過 HTTP Client 測試 `GET /api/auctions`、`GET /api/auctions?q=keyword`、`GET /api/auctions?categoryId=xxx`、`GET /api/auctions?page=2&pageSize=5`（共 5 個 end-to-end 測試）

### Implementation for User Story 1

- [ ] T018 [US1] 新增 `src/Modules/Auction/Application/Queries/GetAuctions/AuctionListItemDto.cs`：含 Id、Title、StartingPrice、EndTime、Status、CategoryId、ThumbnailUrl(nullable)
- [ ] T019 [US1] 新增 `src/Modules/Auction/Application/Queries/GetAuctions/GetAuctionsQuery.cs`：MediatR `IRequest<PagedResult<AuctionListItemDto>>`，含 Q(string?)、CategoryId(Guid?)、Page(int=1)、PageSize(int=20)
- [ ] T020 [US1] 新增 `src/Modules/Auction/Application/Queries/GetAuctions/GetAuctionsQueryHandler.cs`：查詢 `AuctionItem` where Status==Active；`q` 參數用 `EF.Functions.ILike(a.Title, $"%{q}%")`；offset 分頁；projection 至 `AuctionListItemDto`（ThumbnailUrl 取 displayOrder=0 的第一張圖）
- [ ] T021 [US1] 新增 `src/AuctionService.Api/Controllers/AuctionsController.cs`：`[ApiController][Route("api/auctions")]`；`GET /` 對應 `GetAuctionsQuery`；回傳 `ApiResponse<PagedResult<AuctionListItemDto>>` HTTP 200

**Checkpoint**: `dotnet test` 全綠，`GET /api/auctions` 可獨立運作

---

## Phase 4: User Story 2 — 查看商品詳情（含目前最高出價） (Priority: P2)

**Goal**: 競標者查詢特定商品完整資訊，含圖片列表與目前最高出價（本 Phase null stub）。  
**Public endpoint**: `GET /api/auctions/{id}` — 無需 JWT。

**Independent Test**: 查詢存在的商品 ID，驗證回傳完整欄位；查詢不存在的 ID 得 404；無出價時 currentHighestBid 為 null。

### Tests for User Story 2 ⚠️ 先寫測試並確認 FAIL

- [ ] T022 [P] [US2] 撰寫 `tests/AuctionService.UnitTests/Auction/Application/GetAuctionDetailQueryHandlerTests.cs`：測試商品存在回傳完整 DTO（含 currentHighestBid=null）、商品不存在拋 NotFoundException、IBiddingQueryService 被呼叫（NSubstitute verify）（共 3 個測試用例）
- [ ] T023 [P] [US2] 撰寫 `tests/AuctionService.IntegrationTests/Auction/GetAuctionDetailIntegrationTests.cs`：測試 `GET /api/auctions/{id}` 成功、404 情境（共 2 個 end-to-end 測試）

### Implementation for User Story 2

- [ ] T023a [P] [US2] 新增 `src/Modules/Auction/Application/Queries/GetAuctionDetail/AuctionImageDto.cs`：含 Url(string)、DisplayOrder(int) 屬性（H1 修正：補充缺失的 DTO 定義）
- [ ] T024 [US2] 新增 `src/Modules/Auction/Application/Queries/GetAuctionDetail/AuctionDetailDto.cs`：含全部欄位（Id、OwnerId、Title、Description、StartingPrice、EndTime、Status、CategoryId、CurrentHighestBid(decimal?)、WinnerId、SoldAmount、Images(List<AuctionImageDto>)、CreatedAt、UpdatedAt）
- [ ] T025 [US2] 新增 `src/Modules/Auction/Application/Queries/GetAuctionDetail/GetAuctionDetailQuery.cs`：`IRequest<AuctionDetailDto>`，含 AuctionId(Guid)
- [ ] T026 [US2] 新增 `src/Modules/Auction/Application/Queries/GetAuctionDetail/GetAuctionDetailQueryHandler.cs`：查詢含 Images 的 `Auction`（`Include(a => a.Images)`）；呼叫 `IBiddingQueryService.GetHighestBidAsync()` 取得 `BidInfoDto?`；商品不存在拋 `NotFoundException`；projection 至 `AuctionDetailDto`（H3 修正：使用 Auction 類別名稱）
- [ ] T027 [US2] 在 `src/AuctionService.Api/Controllers/AuctionsController.cs` 新增 `GET /{id}` action：回傳 `ApiResponse<AuctionDetailDto>` HTTP 200；404 由 GlobalExceptionMiddleware 處理

**Checkpoint**: `dotnet test` 全綠，`GET /api/auctions/{id}` 可獨立運作

---

## Phase 5: User Story 3 — 追蹤清單（加入/移除/查詢） (Priority: P3)

**Goal**: 已登入競標者可加入/移除追蹤清單（冪等，204），查詢自己的追蹤清單（預設全部，`?status=active` 篩選 Active）。  
**Endpoints**: `POST /api/auctions/{id}/watchlist`、`DELETE /api/auctions/{id}/watchlist`、`GET /api/watchlist` — 需要 JWT。

**Independent Test**: 登入後加入商品至追蹤清單，查詢可見；移除後查詢不見。重複操作均 204 不報錯。

### Tests for User Story 3 ⚠️ 先寫測試並確認 FAIL

- [ ] T028 [P] [US3] 撰寫 `tests/AuctionService.UnitTests/Auction/Application/AddWatchlistCommandHandlerTests.cs`：測試成功加入、重複加入冪等（不重複 insert）（共 2 個測試用例）
- [ ] T029 [P] [US3] 撰寫 `tests/AuctionService.UnitTests/Auction/Application/RemoveWatchlistCommandHandlerTests.cs`：測試成功移除、移除不存在的項目冪等（共 2 個測試用例）
- [ ] T030 [P] [US3] 撰寫 `tests/AuctionService.UnitTests/Auction/Application/GetWatchlistQueryHandlerTests.cs`：測試查詢回傳全部（含 Ended）、`status=active` 僅回傳 Active（共 2 個測試用例）
- [ ] T031 [P] [US3] 撰寫 `tests/AuctionService.IntegrationTests/Auction/WatchlistIntegrationTests.cs`：HTTP 測試加入/移除/查詢追蹤清單、冪等、JWT 保護（401 未登入）（共 5 個 end-to-end 測試）

### Implementation for User Story 3

- [ ] T032 [US3] 新增 `src/Modules/Auction/Application/Commands/AddWatchlist/AddWatchlistCommand.cs` 與 `AddWatchlistCommandHandler.cs`：先 check `Watchlist` 是否存在（user_id + auction_id），不存在則 insert；若商品不存在拋 `NotFoundException`；UserId 從 Command 傳入（Controller 從 JWT 取得）
- [ ] T033 [US3] 新增 `src/Modules/Auction/Application/Commands/RemoveWatchlist/RemoveWatchlistCommand.cs` 與 `RemoveWatchlistCommandHandler.cs`：查找 `Watchlist` 記錄，存在則刪除；不存在則靜默（冪等）
- [ ] T034 [US3] 新增 `src/Modules/Auction/Application/Queries/GetWatchlist/WatchlistItemDto.cs`：含 AuctionId、Title、StartingPrice、EndTime、Status、ThumbnailUrl(nullable)、AddedAt
- [ ] T035 [US3] 新增 `src/Modules/Auction/Application/Queries/GetWatchlist/GetWatchlistQuery.cs` 與 `GetWatchlistQueryHandler.cs`：Join Watchlist + AuctionItem；`status` 參數為 null 則全部，`"active"` 則篩選 Active；按 AddedAt 降序
- [ ] T036 [US3] 在 `src/AuctionService.Api/Controllers/AuctionsController.cs` 新增 `[Authorize] POST /{id}/watchlist` 與 `[Authorize] DELETE /{id}/watchlist` actions：從 `User.FindFirst` 取得 userId，回傳 `ApiResponse` HTTP 204
- [ ] T037 [US3] 新增 `src/AuctionService.Api/Controllers/WatchlistController.cs`：`[Authorize][ApiController][Route("api/watchlist")]`；`GET /` 對應 `GetWatchlistQuery`，傳入 `status` 查詢參數及 JWT userId；回傳 `ApiResponse<List<WatchlistItemDto>>` HTTP 200

**Checkpoint**: `dotnet test` 全綠，追蹤清單三個端點可獨立運作

---

## Phase 6: User Story 4 — 排程結標（AuctionEndBackgroundService） (Priority: P4)

**Goal**: 系統每 60 秒自動掃描 Active 且 endTime ≤ now 的商品，狀態改為 Ended；若有出價則發布 `AuctionWonEvent`（WinnerId/SoldAmount/SellerId）。  
**Strategy**: catch-log-continue，服務不中斷，60 秒後自動重試。

**Independent Test**: 植入過期 Active 商品（含/不含出價），觸發排程後驗證狀態為 Ended，AuctionWonEvent 正確發布/不發布。

### Tests for User Story 4 ⚠️ 先寫測試並確認 FAIL

- [ ] T038 [P] [US4] 撰寫 `tests/AuctionService.UnitTests/Auction/Application/AuctionEndBackgroundServiceTests.cs`：mock `AuctionDbContext` + `IMediator`；測試過期 Active 商品被結標、有出價發布 Event、無出價不發布、Ended 商品冪等不重複處理、批次上限 100（共 5 個測試用例）
- [ ] T039 [P] [US4] 撰寫 `tests/AuctionService.IntegrationTests/Auction/AuctionEndBackgroundServiceIntegrationTests.cs`：植入過期商品 → manually trigger / wait → 驗證 DB 狀態為 Ended（共 2 個 end-to-end 測試）

### Implementation for User Story 4

- [ ] T040 [US4] 新增 `src/Modules/Auction/Infrastructure/BackgroundServices/AuctionEndBackgroundService.cs`：`BackgroundService` 實作；`PeriodicTimer(TimeSpan.FromSeconds(60))`；每輪：查詢 `status=Active AND end_time <= now LIMIT 100`；對每筆呼叫 `IBiddingQueryService.GetHighestBidAsync()` 取得 `BidInfoDto?`；呼叫 `auction.End(bidInfo?.WinnerId, bidInfo?.Amount)`；`SaveChangesAsync()`；若 `bidInfo?.WinnerId != null` 則透過 `IMediator.Publish(new AuctionWonEvent(...))`；try-catch 外層 catch Exception → `_logger.LogError(ex, "...")`  → continue（C1+H3 修正：使用 BidInfoDto? 及 Auction 類別名稱）
- [ ] T041 [US4] 在 `src/Modules/Auction/Application/DependencyInjection.cs` 新增 `services.AddHostedService<AuctionEndBackgroundService>()`

**Checkpoint**: `dotnet test` 全綠，排程結標可獨立驗證

---

## Phase 7: User Story 5 — 建立拍賣商品（賣家，直接上架） (Priority: P5)

**Goal**: 已登入賣家 `POST /api/auctions` 建立商品，直接 Active，回傳 201 + Location header。  
**Requires JWT**.

**Independent Test**: 賣家登入後建立商品，驗證 201 + Location header，DB 中存在 Active 商品，可立即出現在 `GET /api/auctions` 列表中。

### Tests for User Story 5 ⚠️ 先寫測試並確認 FAIL

- [ ] T042 [P] [US5] 撰寫 `tests/AuctionService.UnitTests/Auction/Application/CreateAuctionCommandHandlerTests.cs`：測試成功建立（回傳新 Guid）、起標價 ≤ 0 拋 ValidationException、endTime < now+1min 拋 ValidationException（共 3 個測試用例）
- [ ] T043 [P] [US5] 撰寫 `tests/AuctionService.IntegrationTests/Auction/CreateAuctionIntegrationTests.cs`：HTTP 測試成功建立 201、未登入 401、驗證失敗 422 三種情境（共 3 個 end-to-end 測試）

### Implementation for User Story 5

- [ ] T044 [US5] 新增 `src/Modules/Auction/Application/Commands/CreateAuction/CreateAuctionCommand.cs`：`IRequest<Guid>`，含 OwnerId(Guid)、Title、Description?、StartingPrice(decimal)、EndTime(DateTime)、CategoryId(Guid?)、ImageUrls(List<string>?)
- [ ] T045 [US5] 新增 `src/Modules/Auction/Application/Commands/CreateAuction/CreateAuctionCommandValidator.cs`：FluentValidation；`Title` NotEmpty MaxLength(200)；`StartingPrice` GreaterThan(0)；`EndTime` GreaterThan(DateTime.UtcNow.AddMinutes(1))；`ImageUrls` MaximumCount(5) 且每個 URL 必須為有效絕對 URI（`Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))`）；`CategoryId` 本 Phase 不驗證存在性（nullable，允許任意合法 Guid）（M3+M6 修正）
- [ ] T046 [US5] 新增 `src/Modules/Auction/Application/Commands/CreateAuction/CreateAuctionCommandHandler.cs`：呼叫 `Auction.Create(ownerId, title, description, startingPrice, endTime, categoryId, imageUrls)`；`_context.Auctions.Add(auction)`；`SaveChangesAsync()`；回傳 `auction.Id`（H3 修正：使用 Auction 類別名稱）
- [ ] T047 [US5] 在 `src/AuctionService.Api/Controllers/AuctionsController.cs` 新增 `[Authorize] POST /`：從 JWT 取得 OwnerId；成功回傳 `CreatedAtAction(nameof(GetById), new { id }, ApiResponse<Guid>.Success(id, 201))`；含 Location header

**Checkpoint**: `dotnet test` 全綠，`POST /api/auctions` 可獨立運作

---

## Phase 8: User Story 6 — 編輯上架商品（賣家） (Priority: P6)

**Goal**: 賣家 `PUT /api/auctions/{id}` 修改 Active 商品的 title/description/categoryId/imageUrls；禁止改 startingPrice/endTime（422）；Ended 不可改（409）；非擁有者（403）。  
**Requires JWT**.

**Independent Test**: 建立 Active 商品後更新 title 成功；嘗試更新 startingPrice 得 422；Ended 商品更新得 409；其他用戶操作得 403。

### Tests for User Story 6 ⚠️ 先寫測試並確認 FAIL

- [ ] T048 [P] [US6] 撰寫 `tests/AuctionService.UnitTests/Auction/Application/UpdateAuctionCommandHandlerTests.cs`：測試更新成功、startingPrice 在 body 時拋 ValidationException(422)、Ended 商品拋 ConflictException(409)、非擁有者拋 ForbiddenException(403)、商品不存在拋 NotFoundException(404)（共 5 個測試用例）
- [ ] T049 [P] [US6] 撰寫 `tests/AuctionService.IntegrationTests/Auction/UpdateAuctionIntegrationTests.cs`：HTTP 測試成功更新 200、422/409/403/404 各錯誤情境、未登入 401（共 5 個 end-to-end 測試）

### Implementation for User Story 6

- [ ] T049a [US6] 新增 `src/AuctionService.Api/Controllers/Models/UpdateAuctionRequest.cs`：僅含允許欄位（Title(string?)、Description(string?)、CategoryId(Guid?)、ImageUrls(List<string>?)）；**不含** StartingPrice/EndTime 欄位——Controller 藉此在 model binding 層自然拒絕這兩個欄位，若呼叫方傳入則 JSON deserializer 自動忽略（H2 修正：補充缺失的 API Request Model）
- [ ] T050 [US6] 新增 `src/Modules/Auction/Application/Commands/UpdateAuction/UpdateAuctionCommand.cs`：`IRequest<AuctionDetailDto>`，含 AuctionId(Guid)、RequesterId(Guid)、Title(string?)、Description(string?)、CategoryId(Guid?)、ImageUrls(List<string>?)；**注意**：Command 中不含 StartingPrice/EndTime
- [ ] T051 [US6] 新增 `src/Modules/Auction/Application/Commands/UpdateAuction/UpdateAuctionCommandValidator.cs`：`Title` 若 not null 則 NotEmpty MaxLength(200)；`ImageUrls` MaximumCount(5) 且每個 URL 必須為有效絕對 URI（`Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))`）；`CategoryId` 本 Phase 不驗證存在性（M3+M6 修正）
- [ ] T052 [US6] 新增 `src/Modules/Auction/Application/Commands/UpdateAuction/UpdateAuctionCommandHandler.cs`：查找 `Auction`；不存在拋 `NotFoundException`；Status==Ended 拋 `ConflictException`；OwnerId ≠ RequesterId 拋 `ForbiddenException`；更新允許欄位（null-safe：只有非 null 才覆寫）；`SaveChangesAsync()`；回傳更新後 `AuctionDetailDto`（H3 修正：使用 Auction 類別名稱）
- [ ] T053 [US6] 在 `src/AuctionService.Api/Controllers/AuctionsController.cs` 新增 `[Authorize] PUT /{id}` action：binding `UpdateAuctionRequest`（T049a）——StartingPrice/EndTime 欄位在 model binding 層自然排除；呼叫 `UpdateAuctionCommand`；回傳 `ApiResponse<AuctionDetailDto>` HTTP 200（H2 修正）

**Checkpoint**: `dotnet test` 全綠，`PUT /api/auctions/{id}` 全部情境通過

---

## Phase 9: 舊 Scaffold 測試更新

**Purpose**: 同步更新因 Phase 1 重構造成的舊測試失敗。

- [ ] T054 更新並重命名 `tests/AuctionService.UnitTests/Auction/AuctionItemTests.cs` → `AuctionTests.cs`：移除 `Activate()`、`MarkSold()`、`Cancel()` 相關測試；新增 `Auction.Create()` 直接為 Active、`End(winnerId, soldAmount)` 成功、`End()` 從非 Active 狀態拋例外的測試（H3+M2 修正：與 T001 的 Auction 類別重命名一致）
- [ ] T055 [P] 更新 `tests/AuctionService.IntegrationTests/Auction/AuctionDbContextTests.cs`：更新以符合新 Schema（auctions、categories、auction_images、watchlist）
- [ ] T056 [P] 更新 `tests/AuctionService.UnitTests/Auction/AuctionDependencyInjectionTests.cs`：驗證 `IBiddingQueryService` 已正確注冊

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: 跨 User Story 的橫切面關注，確保品質門檻。

- [ ] T057 [P] 確認 GlobalExceptionMiddleware 能處理 `NotFoundException → 404`、`ConflictException → 409`、`ForbiddenException → 403`（若尚未支援則新增對應的 Exception 類型至 `src/AuctionService.Shared/`）
- [ ] T058 [P] 在所有 CommandHandler/QueryHandler 中新增適當的 `ILogger` 呼叫（Info：建立/更新成功；Warn：商品未找到；Error：結標排程例外）；使用 Structured Logging Property 納入 CorrelationId（與 `src/AuctionService.Api/Middleware/CorrelationIdMiddleware.cs` 的追蹤 ID 對齊，符合 Constitution V 要求）（M5 修正）
- [ ] T059 驗證 `tests/AuctionService.IntegrationTests/Infrastructure/` WebApplicationFactory 的 Seed 資料完整性（T015a 已建立 categories seed）；確認所有 Auction Integration Tests 中 categoryId 筛選使用的分類 ID 與 Seed 資料一致（M1 修正後的驗證步驟）
- [ ] T060 驗證 `dotnet test` 全部通過（Unit + Integration，目標 > 250 tests），輸出覆蓋率報告確認 Auction 模組 > 80%
- [ ] T060a [P] 效能基準測試（Constitution IV 要求）：植入 10,000 筆 Active 商品，執行 `GET /api/auctions` 驗證 p95 ≤ 500ms；其他端點（Detail/Create/Update/Watchlist）驗證 p95 ≤ 200ms（可使用 wrk、k6 或整合測試中的 `Stopwatch` 計時斷言）（H4 修正）
- [ ] T061 執行 `quickstart.md` 中的驗證清單，確認所有端點手動測試通過（使用 `src/AuctionService.Api/AuctionService.Api.http` 或 Swagger UI）

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1 (Setup)
    └── Phase 2 (Foundational / EF+DI)
            ├── Phase 3 (US1: Get Auctions)
            ├── Phase 4 (US2: Get Detail)     ← 依賴 Phase 3 的 Controller 骨架
            ├── Phase 5 (US3: Watchlist)
            ├── Phase 6 (US4: Background Service)
            ├── Phase 7 (US5: Create Auction)
            └── Phase 8 (US6: Update Auction) ← 依賴 Phase 7 建立的端點
Phase 9 (Scaffold 測試更新) ← 應與 Phase 1 同步進行
Phase 10 (Polish) ← 依賴所有 User Story Phase 完成
```

### User Story Dependencies

- **US1 (P1)**: 依賴 Foundational Phase 完成 — 獨立可測
- **US2 (P2)**: 依賴 Foundational Phase + AuctionsController 骨架（US1 建立）— 獨立可測
- **US3 (P3)**: 依賴 Foundational Phase — 獨立可測（WatchlistController 獨立）
- **US4 (P4)**: 依賴 Foundational Phase + IBiddingQueryService stub — 獨立可測
- **US5 (P5)**: 依賴 Foundational Phase — 獨立可測
- **US6 (P6)**: 依賴 US5（需要能先建立商品才能測編輯） — 可與 US5 同步實作
- **Polish (P9/P10)**: 依賴所有 US 完成

### Parallel Opportunities

- **Phase 1 內**：T002、T003、T004、T005、T006、T007 可全部同時進行
- **Phase 2 內**：T009、T010、T011、T012、T013 可平行進行（依賴 T008 完成）
- **Phase 3–8 內**：各 US 的測試任務（標 [P] 者）可先於實作任務同步撰寫
- **Phase 3 + Phase 5 可平行**：US1 與 US5 操作不同 Command/Query，無競爭依賴

---

## Parallel Example: User Story 1 + User Story 5（同時開發）

```
Developer A (US1 - Get Auctions):
T016 → T018 → T019 → T020 → T021 (同時進行 T017)

Developer B (US5 - Create Auction):
T042 → T044 → T045 → T046 → T047 (同時進行 T043)

可平行，因為：
- T020 (QueryHandler) 與 T046 (CommandHandler) 操作不同檔案
- T021 (GET endpoint) 與 T047 (POST endpoint) 在同一個 Controller 檔案
  → 建議 A 先建立 Controller 骨架，B 再 append POST action
```

---

## Implementation Strategy

### MVP Scope（最小可驗證功能）

**Phase 1 + Phase 2 + Phase 3 = 最小 MVP**  
完成後：任何人可以瀏覽 Active 商品列表並搜尋，系統具備基礎商品查詢能力。

### Incremental Delivery

| 完成 Phase | 可驗證的功能 |
|------------|------------|
| Phase 1+2+3 | 瀏覽商品列表、關鍵字搜尋、分類篩選、分頁 |
| + Phase 4 | + 查看商品詳情（含最高出價 null stub） |
| + Phase 5 | + 追蹤清單加入/移除/查詢 |
| + Phase 6 | + 排程結標、AuctionWonEvent 發布 |
| + Phase 7 | + 賣家建立商品（直接 Active） |
| + Phase 8 | + 賣家編輯商品（欄位限制） |
| + Phase 9+10 | 全功能完成，測試全綠，覆蓋率達標 |

### Format Validation

✅ 所有任務均符合格式：`- [ ] T### [P?] [Story?] 描述 with file path`  
✅ Setup Phase：無 Story 標籤  
✅ Foundational Phase：無 Story 標籤  
✅ User Story Phase：均含 [US?] 標籤  
✅ Polish Phase：無 Story 標籤

---

## Summary

| 指標 | 數量 |
|------|------|
| 總任務數 | 65 |
| Setup Phase (Phase 1) | 7 |
| Foundational Phase (Phase 2) | 9（含 T015a Seed）|
| US1 (P1) | 6 |
| US2 (P2) | 7（含 T023a AuctionImageDto）|
| US3 (P3) | 10 |
| US4 (P4) | 4 |
| US5 (P5) | 6 |
| US6 (P6) | 7（含 T049a UpdateAuctionRequest）|
| Scaffold 測試更新 (Phase 9) | 3 |
| Polish (Phase 10) | 6（含 T060a 效能測試）|
| 可平行任務（[P] 標記） | 29 |
| Unit Test 任務 | 8 |
| Integration Test 任務 | 8 |
| MVP 範圍（Phase 1+2+3）| 22（含 T015a）|
