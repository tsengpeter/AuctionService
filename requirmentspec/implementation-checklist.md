# Implementation Checklist: AuctionService 後端模組化單體

**Date**: 2026-03-18  
**Architecture Ref**: [architecture-design.md](./architecture-design.md)  
**Backend Spec Ref**: [auction-app-spec-backend.md](./auction-app-spec-backend.md)

---

## Phase 0 — Git 初始化

- [ ] `git checkout -b develop`
- [ ] `git push -u origin develop`
- [ ] 驗證：`git branch -a` 確認 `origin/develop` 存在

---

## Phase 1 — 後端專案骨架 Scaffold

**Branch**: `001-backend-scaffold` (from `develop`)

### SpecKit 流程

- [ ] `git checkout develop && git pull`
- [ ] `git checkout -b 001-backend-scaffold`
- [ ] `/speckit.specify` 建立 ASP.NET Core 10 Modular Monolith 後端骨架
- [ ] `/speckit.clarify`
  - [ ] 確認 PostgreSQL 連線字串格式與 Schema naming
  - [ ] 確認 JWT 設定（HS256 / secret source）
  - [ ] 確認專案 namespace 格式（`AuctionService.{Module}.{Layer}`）
- [ ] `/speckit.plan`
- [ ] `/speckit.tasks`
  - [ ] 確認包含 `dotnet new sln`
  - [ ] 確認包含各模組 `dotnet new classlib` × 3 層
  - [ ] 確認包含 `docker-compose.yml`（PostgreSQL + pgAdmin）
  - [ ] 確認包含 `.gitignore`（含 `bin/`, `obj/`, `*.user`, `.env`）
  - [ ] 確認包含 `AuctionService.Shared` 基礎類別
  - [ ] 確認包含 `AuctionService.Api` Swagger + JWT Middleware
- [ ] `/speckit.implement`
- [ ] `/speckit.checklist`

### 驗收

- [ ] `dotnet build` 零錯誤
- [ ] `docker compose up -d` PostgreSQL 啟動成功
- [ ] `dotnet run` + 開啟 `https://localhost:5001/swagger` 可存取
- [ ] `git commit -m "feat: scaffold ASP.NET Core 10 Modular Monolith backend"`
- [ ] `git push -u origin 001-backend-scaffold`
- [ ] PR: `001-backend-scaffold` → `develop` → Merge

---

## Phase 2 — Member 模組

**Branch**: `002-member-module` (from `develop`)  
**依賴**: Phase 1 已 merge

### SpecKit 流程

- [ ] `git checkout develop && git pull`
- [ ] `git checkout -b 002-member-module`
- [ ] `/speckit.specify` Member 模組（註冊、登入、JWT、Refresh Token）
- [ ] `/speckit.clarify`
  - [ ] Token 有效期設定
  - [ ] Refresh Token 旋轉策略
  - [ ] 重複 email 錯誤訊息格式
- [ ] `/speckit.plan`
- [ ] `/speckit.tasks`
- [ ] `/speckit.implement`
- [ ] `/speckit.checklist`

### TDD 重點

- [ ] Red: `RegisterCommandHandlerTests`（重複 email → 409）
- [ ] Red: `LoginCommandHandlerTests`（錯誤密碼 → 401）
- [ ] Green: 實作 Handler
- [ ] Refactor: 抽取 `IPasswordHasher`、`IJwtTokenService` 介面

### 驗收

- [ ] `POST /api/auth/register` → 201 / 409
- [ ] `POST /api/auth/login` → 200 + token / 401
- [ ] `POST /api/auth/refresh` → 200 + 新 token
- [ ] 單元測試覆蓋率 > 80%
- [ ] PR: `002-member-module` → `develop` → Merge

---

## Phase 3 — Auction 模組

**Branch**: `003-auction-module` (from `develop`)  
**依賴**: Phase 2 已 merge

### SpecKit 流程

- [ ] `git checkout develop && git pull`
- [ ] `git checkout -b 003-auction-module`
- [ ] `/speckit.specify` Auction 模組（CRUD、狀態機、追蹤清單、結標事件）
- [ ] `/speckit.clarify`
- [ ] `/speckit.plan`
- [ ] `/speckit.tasks`
- [ ] `/speckit.implement`
- [ ] `/speckit.checklist`

### TDD 重點

- [ ] Red: `AuctionStatusMachineTests`（Draft → Active → Ended 流轉）
- [ ] Red: `PlaceBidValidationTests`（結標後禁止出價）
- [ ] Red: `AuctionWonEventPublishedTests`（結標時確認事件發布）
- [ ] Green + Refactor

### 驗收

- [ ] `POST /api/auctions` → 201（需 JWT）
- [ ] `GET /api/auctions?category=&q=&page=` → 200 + 分頁
- [ ] 結標觸發 `AuctionWonEvent` 正確進入 MediatR pipeline
- [ ] 單元測試覆蓋率 > 80%
- [ ] PR: `003-auction-module` → `develop` → Merge

---

## Phase 4 — Bidding 模組

**Branch**: `004-bidding-module` (from `develop`)  
**依賴**: Phase 2 + Phase 3 已 merge

### SpecKit 流程

- [ ] `git checkout develop && git pull`
- [ ] `git checkout -b 004-bidding-module`
- [ ] `/speckit.specify` Bidding 模組（出價、出價歷史、我的出價狀態）
- [ ] `/speckit.clarify`
- [ ] `/speckit.plan`
- [ ] `/speckit.tasks`
- [ ] `/speckit.implement`
- [ ] `/speckit.checklist`

### TDD 重點

- [ ] Red: 出價金額 ≤ 最高價 → 422
- [ ] Red: 非 Active 商品出價 → 409
- [ ] Red: `GetMyBidsQuery` 正確標 leading/outbid 狀態
- [ ] Green + Refactor

### 驗收

- [ ] `POST /api/bids` → 201 / 422 / 409
- [ ] `GET /api/bids?auctionId=` → 200 + 出價歷史
- [ ] `GET /api/bids/my` → 200 + 狀態標示
- [ ] 單元測試覆蓋率 > 80%
- [ ] PR: `004-bidding-module` → `develop` → Merge

---

## Phase 5 — Ordering 模組

**Branch**: `005-ordering-module` (from `develop`)  
**依賴**: Phase 3 已 merge（`AuctionWonEvent`）

### SpecKit 流程

- [ ] `git checkout develop && git pull`
- [ ] `git checkout -b 005-ordering-module`
- [ ] `/speckit.specify` Ordering 模組（監聽事件建立訂單、狀態流轉、出貨確認）
- [ ] `/speckit.clarify`
- [ ] `/speckit.plan`
- [ ] `/speckit.tasks`
- [ ] `/speckit.implement`
- [ ] `/speckit.checklist`

### TDD 重點

- [ ] Red: `AuctionWonEventHandler` 整合測試（Testcontainers + 真實 DB）
- [ ] Red: 狀態流轉測試 `PendingPayment → Paid → Shipped → Completed`
- [ ] Red: 冪等測試（同一 `auction_id` 重複事件不建立兩筆訂單）
- [ ] Green + Refactor

### 驗收

- [ ] `AuctionWonEvent` 觸發後 `ordering.orders` 自動插入 `PendingPayment` 訂單
- [ ] `GET /api/orders` 買家/賣家視角正確分離
- [ ] `POST /api/orders/{id}/shipping` → 204（賣家更新出貨）
- [ ] `POST /api/orders/{id}/complete` → 204（買家確認收貨）
- [ ] 整合測試全綠
- [ ] PR: `005-ordering-module` → `develop` → Merge

---

## Phase 6 — Notification 模組

**Branch**: `006-notification-module` (from `develop`)  
**依賴**: Phase 3 + Phase 5 已 merge（所有模組事件）

### SpecKit 流程

- [ ] `git checkout develop && git pull`
- [ ] `git checkout -b 006-notification-module`
- [ ] `/speckit.specify` Notification 模組（監聽事件寫入通知、已讀標記、未讀數量）
- [ ] `/speckit.clarify`
- [ ] `/speckit.plan`
- [ ] `/speckit.tasks`
- [ ] `/speckit.implement`
- [ ] `/speckit.checklist`

### TDD 重點

- [ ] Red: `AuctionWonEvent` 觸發 → `notifications` 表正確寫入（買家）
- [ ] Red: `OrderStatusChangedEvent` 觸發 → 通知買賣家
- [ ] Red: `GET /api/notifications/unread-count` 回傳正確數字
- [ ] Red: 標記已讀冪等（重複呼叫不報錯）
- [ ] Green + Refactor

### 驗收

- [ ] 事件觸發 → 通知自動產生
- [ ] `GET /api/notifications` → 200 + 分頁
- [ ] `PATCH /api/notifications/{id}/read` → 204
- [ ] `GET /api/notifications/unread-count` → 200 + 數字
- [ ] 單元測試覆蓋率 > 80%
- [ ] PR: `006-notification-module` → `develop` → Merge

---

## Phase 7 — 架構邊界驗證（Cross-Module）

**Branch**: `007-arch-tests` (from `develop`)  
**依賴**: Phase 1–6 全部 merge

- [ ] `git checkout develop && git pull`
- [ ] `git checkout -b 007-arch-tests`
- [ ] 加入 `ArchUnitNET` 套件至 `tests/AuctionService.ArchTests/`
- [ ] 撰寫規則：`Modules.Member.*` 不得直接參考其他模組 Application/Infrastructure 層
- [ ] `dotnet test tests/AuctionService.ArchTests/` 全綠
- [ ] `k6` 對 `/api/auctions`、`/api/bids` 執行 p95 基準測試，目標 < 200ms
- [ ] PR: `007-arch-tests` → `develop` → Merge
- [ ] PR: `develop` → `master` → Merge（第一個完整可用版本）

---

## 全域 Definition of Done

每個 Phase PR 需滿足以下條件才可 Merge：

- [ ] `dotnet build` 零錯誤
- [ ] `dotnet test` 全綠
- [ ] 單元測試覆蓋率 > 80%（Constitution II）
- [ ] SpecKit checklist 通過
- [ ] API 回應格式符合 `ApiResponse<T>` 統一結構（Constitution III）
- [ ] 無明文 secret / 連線字串 commit（Constitution I / Security）
- [ ] PR 描述包含：功能說明、測試結果截圖或指令輸出
