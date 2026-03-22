# 實作任務清單：後端模組化單體骨架

**功能分支**: `001-backend-scaffold`  
**建立日期**: 2026-03-22  
**狀態**: Phase 2 任務清單  
**規格參考**: [spec.md](spec.md) | [plan.md](plan.md) | [research.md](research.md) | [data-model.md](data-model.md)

---

## 實作策略

**MVP 範圍（第一階段交付）**: US1 + US2 + 核心共享層  
**進階範圍（第二階段）**: US3（API 一致性）+ US4（自動化測試）

**並行機會**:
- T001-T008（Setup 階段）全序列
- T021-T030（各模組 Infrastructure）可並行 [P] — 各自獨立 schema 和 migration
- T031-T038（Domain Entity 定義）可並行 [P] — 無交叉依賴
- T101-T108（US1 测试框架）可並行 [P] — 各自獨立 setup

---

## Phase 1：專案初始化

### Setup Infrastructure & Version Control

- [x] T001 初始化 Git 分支結構：確認在 `001-backend-scaffold` 分支，建立初始 commit 紀錄
- [x] T002 建立 `.gitignore`：排除 `bin/`, `obj/`, `.vs/`, `appsettings.Development.json`, `.env`
- [x] T003 [P] 建立 Solution 檔案：執行 `dotnet new sln -n AuctionService` 於 Repository root

### Project Structure & Package Setup

- [x] T004 建立目錄結構：建立 `src/`, `src/Modules/`, `tests/` 目錄（無需程式碼，純資料夾）
- [x] T005 [P] 建立 API Host csproj：`dotnet new webapi -n AuctionService.Api --output src/AuctionService.Api/`，加入至 sln
- [x] T006 [P] 建立 Shared Library csproj：`dotnet new classlib -n AuctionService.Shared --output src/AuctionService.Shared/`，加入至 sln
- [x] T007 [P] 建立五個 Module csproj（Member, Auction, Bidding, Ordering, Notification）：每個執行 `dotnet new classlib -n {Module} --output src/Modules/{Module}/`，並加入至 sln
- [x] T007b 驗證跨專案引用：檢查各 Module.csproj 是否包含對 Shared.csproj 的 `<ProjectReference>`；檢查 API Host csproj 是否包含所有 5 個模組的引用（驗收：`grep ProjectReference` 確認正確路徑）

### NuGet Dependencies

- [x] T008 [P] 新增 NuGet 套件至 Shared：MediatR 12.4.1、FluentValidation 12.0.0、FluentValidation.DependencyInjectionExtensions 12.0.0

---

## Phase 2：共享層骨架（Foundational）

### Shared Layer — Core Types

- [x] T009 實作 `BaseEntity`：檔案 `src/AuctionService.Shared/BaseEntity.cs`（包含 `Id: Guid`、`CreatedAt`、`UpdatedAt` 屬性，無參數 protected 建構子）
- [x] T010 實作 `IDomainEvent` 介面：檔案 `src/AuctionService.Shared/Events/IDomainEvent.cs`（繼承 `INotification`，含 `EventId` 和 `OccurredAt` 屬性）
- [x] T011 實作 `ApiResponse<T>`（成功）：檔案 `src/AuctionService.Shared/ApiResponse.cs`（record 型態，含 `Success: bool`, `Data: T?`, `StatusCode: int`）
- [x] T012 實作 `ApiResponse` + `FieldError`（失敗）：同檔案 `src/AuctionService.Shared/ApiResponse.cs`（record：Success, Errors, Error, StatusCode）
- [x] T013 實作 `ApiResponse` 工廠方法：同檔案，靜態方法 `Ok<T>()`, `Fail()`, `ValidationFail()`

### Shared Layer — Validation Pipeline

- [x] T014 實作 `ValidationBehavior<TRequest, TResponse>`：檔案 `src/AuctionService.Shared/Behaviors/ValidationBehavior.cs`（實作 `IPipelineBehavior<,>`，使用所有 `IValidator<TRequest>` 執行驗證，拋出 `ValidationException`）
- [x] T015 [P] 新增 NuGet 至各 Module csproj：MediatR 12.4.1、FluentValidation 12.0.0、EF Core 10.0.0、Npgsql.EFCore.PostgreSQL 10.0.0

### Shared Layer — Extension Methods

- [x] T016 實作服務註冊擴充方法：檔案 `src/AuctionService.Shared/DependencyInjection.cs`（靜態類別 `SharedDependencyInjection` 含 `AddSharedServices()` 方法，註冊 `ValidationBehavior`）

---

## Phase 2.5：共享層單元測試（TDD 合規）

### Shared Layer Unit Tests

- [x] T016b [P] 實作 `ApiResponseTests`：檔案 `tests/AuctionService.UnitTests/Shared/ApiResponseTests.cs`（測試 `Ok<T>()`、`Fail()`、`ValidationFail()` 正確序列化；驗證 StatusCode 範圍；測試 null data 和重複 errors）
- [x] T016c [P] 實作 `ValidationBehaviorTests`：檔案 `tests/AuctionService.UnitTests/Shared/ValidationBehaviorTests.cs`（測試有效請求通過；測試無效請求拋出 `ValidationException`；測試例外包含完整的欄位錯誤）
- [x] T016d [P] 實作 `GlobalExceptionMiddlewareTests`：檔案 `tests/AuctionService.UnitTests/API/GlobalExceptionMiddlewareTests.cs`（測試 `ValidationException` → 422 ApiResponse mapping；測試泛用 Exception → 500；測試 middleware 記錄例外）

---

## Phase 3：API Host 設定

### Program.cs & Middleware

- [x] T017 設定 Program.cs 基礎：檔案 `src/AuctionService.Api/Program.cs`（建立 WebApplication Builder，註冊 Shared services、各 Module DI、GlobalExceptionMiddleware）
- [x] T017b 實作 JWT 認證：檔案 `src/AuctionService.Api/Program.cs` JWT 區段（註冊 `JwtBearerDefaults.AuthenticationScheme`；實裝 `OnChallenge` 和 `OnForbidden` 事件處理器回傳 `ApiResponse` 格式；驗證 JWT_SECRET 長度 ≥ 32 字元）
- [x] T017c 實作結構化日誌：檔案 `src/AuctionService.Api/Program.cs` Logging 區段（設定 `ILogger<T>`，Development 環境使用 JSON Console formatter，登記 api 啟動日誌）
- [x] T018 實作 `GlobalExceptionMiddleware`：檔案 `src/AuctionService.Api/Middleware/GlobalExceptionMiddleware.cs`（捕捉例外，轉換為 `ApiResponse` 格式，含 `ValidationException` → 422 映射，記錄完整 stack trace）
- [x] T019 實作 appsettings 檔案：`src/AuctionService.Api/appsettings.json`（非敏感預設值，含 Logging 組織），`src/AuctionService.Api/appsettings.Development.json`（環境變數佔位符，Logging 設為 Debug 級）

### Health Check

- [x] T020 實作健康檢查端點：程式碼新增至 Program.cs 或另檔 `src/AuctionService.Api/Health/HealthCheckEndpoint.cs`（GET `/health` 回傳 `{ status: "Healthy|Degraded|Unhealthy", duration, entries: {...} }`）

---

## Phase 4：Module Infrastructure — Member

### Member Domain & DbContext

- [x] T021 [P] 建立 Member Module 目錄結構：`src/Modules/Member/Domain/`, `./Application/`, `./Infrastructure/Persistence/Migrations/`
- [x] T022 [P] 實作 `MemberUser` Entity：檔案 `src/Modules/Member/Domain/Entities/MemberUser.cs`（繼承 `BaseEntity`，含 Email, Username, PasswordHash 欄位，含驗證規則註解）
- [x] T022b [P] 實作 `MemberUserTests`：檔案 `tests/AuctionService.UnitTests/Member/MemberUserTests.cs`（測試 Entity 建構子；測試 Email 驗證規則；測試 PasswordHash 不可為空）
- [x] T023 [P] 實作 `MemberDbContext`：檔案 `src/Modules/Member/Infrastructure/Persistence/MemberDbContext.cs`（繼承 DbContext，`HasDefaultSchema("member")`，DbSet<MemberUser>，Email unique index）
- [x] T023b [P] 實作 `MemberDbContextTests`：檔案 `tests/AuctionService.IntegrationTests/Member/MemberDbContextTests.cs`（測試 Schema 名稱為 "member"；測試 Email unique constraint；測試可透過 DbContext 建立/讀取實體）
- [x] T024 [P] 建立初始 Migration：執行 `dotnet ef migrations add InitialCreate --project src/Modules/Member/Member.csproj --startup-project src/AuctionService.Api/AuctionService.Api.csproj --output-dir Infrastructure/Persistence/Migrations`（驗收：Migration 檔案存在於 `Infrastructure/Persistence/Migrations/`，檔名符合 `yyyyMMddHHmmss_InitialCreate.cs` 格式）

### Member DI & Module Registration

- [x] T025 [P] 實作 `MemberDependencyInjection.cs`：檔案 `src/Modules/Member/Application/DependencyInjection.cs`（靜態類別含 `AddMemberModule()` 方法，註冊 MemberDbContext, MediatR handlers, FluentValidation validators）
- [x] T025b [P] 實作 `MemberDependencyInjectionTests`：檔案 `tests/AuctionService.UnitTests/Member/MemberDependencyInjectionTests.cs`（測試 DI 容器可正確註冊所有服務）

---

## Phase 5：Module Infrastructure — Auction, Bidding, Ordering, Notification

### Auction Module

- [x] T026 [P] 建立 Auction Module 結構：`src/Modules/Auction/Domain/`, `./Application/`, `./Infrastructure/`
- [x] T027 [P] 實作 `AuctionStatus` enum 和 `AuctionItem` Entity：檔案 `src/Modules/Auction/Domain/Entities/AuctionItem.cs`（ID, Title, StartingPrice, Status, EndsAt）
- [x] T027b [P] 實作 `AuctionItemTests`：檔案 `tests/AuctionService.UnitTests/Auction/AuctionItemTests.cs`（測試狀態轉移；測試 StartingPrice > 0 驗證）
- [x] T028 [P] 實作 `AuctionDbContext`：檔案 `src/Modules/Auction/Infrastructure/Persistence/AuctionDbContext.cs`（schema: "auction"）
- [x] T028b [P] 實作 `AuctionDbContextTests`：檔案 `tests/AuctionService.IntegrationTests/Auction/AuctionDbContextTests.cs`
- [x] T029 [P] 建立 Auction Migration：`dotnet ef migrations add InitialCreate --project src/Modules/Auction/Auction.csproj --startup-project src/AuctionService.Api/AuctionService.Api.csproj --output-dir Infrastructure/Persistence/Migrations`（驗收：Migration 檔案存在）
- [x] T030 [P] 實作 `AuctionDependencyInjection.cs`
- [x] T030b [P] 實作 `AuctionDependencyInjectionTests`

### Bidding Module

- [x] T031 [P] 建立 Bidding Module 結構
- [x] T032 [P] 實作 `Bid` Entity：檔案 `src/Modules/Bidding/Domain/Entities/Bid.cs`（AuctionId 邏輯 ID, BidderId 邏輯 ID, Amount, PlacedAt，無資料庫 FK）
- [x] T032b [P] 實作 `BidTests`：檔案 `tests/AuctionService.UnitTests/Bidding/BidTests.cs`
- [x] T033 [P] 實作 `BiddingDbContext`：schema: "bidding"
- [x] T033b [P] 實作 `BiddingDbContextTests`：檔案 `tests/AuctionService.IntegrationTests/Bidding/BiddingDbContextTests.cs`
- [x] T034 [P] 建立 Bidding Migration：驗收：Migration 檔案存在於正確路徑
- [x] T035 [P] 實作 `BiddingDependencyInjection.cs`
- [x] T035b [P] 實作 `BiddingDependencyInjectionTests`

### Ordering Module

- [x] T036 [P] 建立 Ordering Module 結構
- [x] T037 [P] 實作 `OrderStatus` enum 和 `Order` Entity：AuctionId, BuyerId, Amount, Status（無資料庫 FK）
- [x] T037b [P] 實作 `OrderTests`：檔案 `tests/AuctionService.UnitTests/Ordering/OrderTests.cs`
- [x] T038 [P] 實作 `OrderingDbContext`：schema: "ordering"
- [x] T038b [P] 實作 `OrderingDbContextTests`：檔案 `tests/AuctionService.IntegrationTests/Ordering/OrderingDbContextTests.cs`
- [x] T039 [P] 建立 Ordering Migration：驗收：Migration 檔案存在
- [x] T040 [P] 實作 `OrderingDependencyInjection.cs`
- [x] T040b [P] 實作 `OrderingDependencyInjectionTests`

### Notification Module

- [x] T041 [P] 建立 Notification Module 結構
- [x] T042 [P] 實作 `NotificationRecord` Entity：RecipientId 邏輯 ID, Type, Payload (JSON string), SentAt
- [x] T042b [P] 實作 `NotificationRecordTests`：檔案 `tests/AuctionService.UnitTests/Notification/NotificationRecordTests.cs`
- [x] T043 [P] 實作 `NotificationDbContext`：schema: "notification"
- [x] T043b [P] 實作 `NotificationDbContextTests`：檔案 `tests/AuctionService.IntegrationTests/Notification/NotificationDbContextTests.cs`
- [x] T044 [P] 建立 Notification Migration：驗收：Migration 檔案存在
- [x] T045 [P] 實作 `NotificationDependencyInjection.cs`
- [x] T045b [P] 實作 `NotificationDependencyInjectionTests`

---

## Phase 6：Infrastructure & Configuration

### Docker Compose & Environment

- [x] T046 建立 `docker-compose.yml`：檔案 `docker-compose.yml`（PostgreSQL 16 port 5432, pgAdmin 4 port 5050；定義 service network；volumes for postgres_data；environment variables from .env；depends_on 確保連線順序；驗收：`docker compose up -d` 成功啟動兩個服務）
- [x] T046b 建立 `Dockerfile`：Multi-stage build（Stage 1: `sdk:10.0` restore + `dotnet publish -c Release`；Stage 2: `aspnet:10.0` runtime only）；建立非 root 使用者 `appuser`；`EXPOSE 8080`；`ASPNETCORE_URLS=http://+:8080`；所有敏感設定透過環境變數注入；驗收：`docker build -t auctionservice:latest .` 成功，`docker run` 後 `GET /health` 回傳 Healthy
- [x] T047 建立 `.env.example`：檔案 `.env.example`（所有必要環境變數：ASPNETCORE_ENVIRONMENT, POSTGRES_USER, POSTGRES_PASSWORD, POSTGRES_DB, JWT_SECRET ≥32 chars, JWT_ISSUER, JWT_AUDIENCE, JWT_EXPIRY_MINUTES, PGADMIN_PASSWORD；驗收：`.env` copy from example 後，所有變數定義完整）

### API Contract & Documentation

- [x] T048 集成 Swagger/OpenAPI：新增 `Microsoft.AspNetCore.Mvc.ApiExplorer`, `Swashbuckle.AspNetCore` 至 API Host csproj，在 Program.cs 註冊 Swagger services 和 middleware（限非 Production 環境）

---

## Phase 7：Testing Framework Setup

### Unit Test Infrastructure

- [x] T049 [P] 建立 UnitTests csproj：`dotnet new xunit -n AuctionService.UnitTests --output tests/AuctionService.UnitTests/`，加入 sln
- [x] T050 [P] 新增 NuGet 至 UnitTests：xunit 2.9.3, FluentAssertions 8.1.0, NSubstitute 5.3.0, coverlet.collector 6.0.3
- [x] T051 [P] 實作 `ApiResponseTests`：檔案 `tests/AuctionService.UnitTests/Shared/ApiResponseTests.cs`（驗證 `ApiResponse<T>.Ok()`, `Fail()`, `ValidationFail()` 正確序列化）

### Integration Test Infrastructure

- [x] T052 [P] 建立 IntegrationTests csproj：`dotnet new xunit -n AuctionService.IntegrationTests --output tests/AuctionService.IntegrationTests/`
- [x] T053 [P] 新增 NuGet 至 IntegrationTests：Testcontainers.PostgreSql 4.2.0, Microsoft.AspNetCore.Mvc.Testing 10.0.0（其餘同 UnitTests）
- [x] T054 [P] 實作 `IntegrationTestFixture`：檔案 `tests/AuctionService.IntegrationTests/Infrastructure/IntegrationTestFixture.cs`（實作 `IAsyncLifetime`，初始化 PostgreSQL Testcontainer，建立 WebApplicationFactory）
- [x] T055 [P] 實作測試集合定義：檔案 `tests/AuctionService.IntegrationTests/Infrastructure/IntegrationCollection.cs`（`[CollectionDefinition]` 共享 `IntegrationTestFixture`）

### Health Check Test

- [x] T056 實作健康檢查整合測試：檔案 `tests/AuctionService.IntegrationTests/Health/HealthCheckTests.cs`（測試 GET `/health` 回傳 200，response time < 100ms，驗證 SC-006）

---

## Phase 8：Acceptance & Validation

### Build & Test Validation

- [x] T057 驗證沒有編譯錯誤：執行 `dotnet build`，驗收：Exit code = 0，ErrorCount = 0，所有 9 csproj 檔案編譯成功，驗證 SC-002
- [x] T058 驗證所有模組可獨立編譯：分別執行 `dotnet build src/AuctionService.Api/ src/AuctionService.Shared/ src/Modules/{Module}/`（各自 exit code = 0），驗證 SC-005（零跨模組副作用）
- [x] T059 驗證所有測試通過：執行 `dotnet test`，驗收：所有單元 + 整合測試全綠，exit code = 0，驗證 SC-003

### API & Database Validation

- [x] T060 驗證一鍵啟動流程：按 [quickstart.md](quickstart.md) 執行步驟 1-6，驗證 SC-001（< 10 分鐘）
- [x] T061 驗證 Swagger 可開啟：開啟 https://localhost:5001/swagger，確認列出所有模組骨架端點
- [x] T062 驗證健康檢查：呼叫 GET `/health`，驗證狀態 200 且 duration < 100ms，驗證 SC-006

### Module Isolation Validation

- [x] T063 驗證各模組 Schema 隔離：連線 PostgreSQL 驗證存在 5 個獨立 schema（member, auction, bidding, ordering, notification），驗證 SC-005

---

## Phase 9：Documentation & Commit

### Final Documentation Updates

- [x] T064 更新 `README.md`：包含快速啟動、專案結構、開發指南連結
- [x] T065 新增 DEVELOPMENT.md：詳細開發者設定、常用指令、troubleshooting
- [x] T066 新增 ARCHITECTURE.md：系統架構圖、模組邊界、事件流程

### Version Control

- [x] T067 Commit 所有程式碼：`git add -A && git commit -m "..."`（遵循 Conventional Commits）
- [x] T068 建立 Pull Request：`001-backend-scaffold` → `develop`，描述包含完成的 SC 和 US 清單

---

## 任務依賴與並行執行圖

```
Phase 1 (T001-T008): Sequential 序列
    ↓
Phase 1b (T007b): Verify references
    ↓
Phase 2 (T009-T016): Sequential（順序堆疊）
    ↓
Phase 2.5 (T016b-T016d): [P] Parallel Shared Layer Tests（TDD）
    ↓
Phase 3 (T017-T017d, T018-T020): Sequential
    - T017: Program.cs base
    - T017b: JWT setup
    - T017c: Logging setup
    - T018-T020: Middleware & health check
    ↓
Phase 4-5 (T021-T045b): Parallel by Module（TDD-First）
    - T021-T025b (Member: Entity → Tests → DbContext → Tests → Migration → DI → Tests)
    - T026-T030b (Auction: same pattern)
    - T031-T035b (Bidding: same pattern)
    - T036-T040b (Ordering: same pattern)
    - T041-T045b (Notification: same pattern)
    ↓
Phase 6 (T046-T048): Sequential [P] Docker 和 Swagger 可並行
    ↓
Phase 7 (T049-T056): [P] Parallel
    - T049-T051 (UnitTests framework)
    - T052-T056 (IntegrationTests framework)
    ↓
Phase 8 (T057-T063): Sequential（驗證）
    ↓
Phase 9 (T064-T068): Sequential（documentation + commit）
```

---

## 任務計數摘要

| Phase | 任務數 | 說明 |
|-------|--------|------|
| 1 | 8 | Setup 基礎設施 |
| 1b | 1 | csproj 引用驗證 |
| 2 | 8 | Shared Layer 骨架 |
| 2.5 | 3 | **Shared Layer 單元測試（TDD）** |
| 3 | 6 | API Host 設定（含 JWT、Logging） |
| 4-5 | 35 | 5 個模組基礎設施 + 測試（TDD；T021-T045 擴展至 T045b） |
| 6 | 3 | Docker + Swagger |
| 7 | 8 | 測試框架 |
| 8 | 7 | 驗收（SC 檢查） |
| 9 | 5 | 文件 + commit |
| **總計** | **84** | |

---

## 各 User Story 相對應任務

| User Story | 優先級 | 對應任務 | 驗收標準 |
|-----------|--------|---------|---------|
| US1：一鍵啟動 | P1 | T001-T048（所有基礎設施），T060-T062（啟動驗證） | < 10 分鐘、Swagger 可開啟、健康檢查成功 |
| US2：模組隔離 | P2 | T021-T045（各模組獨立 setup）, T063（Schema 隔離驗證） | 5 個獨立 schema，各模組無跨 schema FK |
| US3：API 一致性 | P3 | T011-T013（ApiResponse 設計）, T018（GlobalExceptionMiddleware），T048（Swagger） | 所有端點回應格式一致 100% |
| US4：自動化測試 | P4 | T049-T056（測試框架），T057-T059（測試驗收） | 執行 dotnet test 全綠，無外部依賴 |

---

## TDD 實作流程示例（Member 模組）

實際執行順序遵循紅綠重構周期：

1. **紅**（T022b）：寫 MemberUser 測試（全失敗）
2. **綠**（T022）：實作 MemberUser Entity 使測試通過
3. **重構**（+）：如需調整 Entity 結構
4. **紅**（T023b）：寫 MemberDbContext 整合測試（全失敗）
5. **綠**（T023）：實作 DbContext 使測試通過
6. **紅**（T024）：建立 Migration（測試 Migration 套用成功）
7. **綠**（T025b）：寫 DI 測試確保註冊完整
8. **綠**（T025）：實作 DependencyInjection.cs

其他 4 個模組（Auction, Bidding, Ordering, Notification）應同步執行相同模式（T026-T030b, T031-T035b, T036-T040b, T041-T045b）。

---

## 後續（非骨架階段）

以下任務在 Phase 2+ 分支中實裝：

- **002-member-module**: 使用者註冊、登入、JWT 簽發（基於骨架）
- **003-auction-module**: 商品管理、狀態機、結標邏輯
- **004-bidding-module**: 出價業務邏輯
- **005-ordering-module**: 訂單流程
- **006-notification-module**: 通知推送
- **007-arch-tests**: ArchUnitNET 架構測試 + k6 效能測試 + merge to master

---

**狀態**: 準備就緒，可開始 T001 實裝。建議使用此任務清單搭配 `/speckit.implement` 指令生成實際程式碼。
