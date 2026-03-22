# Implementation Checklist: AuctionService 後端模組化單體

**Date**: 2026-03-18  
**Architecture Ref**: [architecture-design.md](./architecture-design.md)  
**Backend Spec Ref**: [auction-app-spec-backend.md](./auction-app-spec-backend.md)

---

## Phase 0 — Git 初始化

- [x] `git checkout -b develop` (develop 已存在，直接切換)
- [x] `git push -u origin develop` (已推送至原點)
- [x] 驗證：`git branch -a` 確認 `origin/develop` 存在 ✓

---

## Phase 1 — 後端專案骨架 Scaffold

**Branch**: `001-backend-scaffold` (from `develop`)

### 1-A. 建立分支

```bash
git checkout develop
git pull origin develop
git checkout -b 001-backend-scaffold
```

### 1-B. SpecKit 流程（在 VS Code Copilot Chat 依序執行）

- [x] **Specify** — 貼上以下指令到 Copilot Chat：
  ```
  /speckit.specify 建立 ASP.NET Core 10 Modular Monolith 後端骨架，包含 Member、Auction、Bidding、Ordering、Notification 五個模組，每模組採 Domain/Application/Infrastructure 三層結構，使用 PostgreSQL Schema 隔離（各模組對應 schema: member, auction, bidding, ordering, notification），MediatR 作為模組間 In-Memory 事件匯流排，JWT HS256 身份驗證，FluentValidation 輸入驗證，Swagger/OpenAPI 文件，xUnit + Testcontainers 整合測試，統一 ApiResponse<T> 回應格式
  ```

- [x] **Clarify** — 貼上以下指令到 Copilot Chat：
  ```
  /speckit.clarify
  ```
  回答問題時確認以下細節：
  - [x] PostgreSQL 連線字串用 `IConfiguration`，從 `appsettings.Development.json` 讀取，不 hardcode
  - [x] JWT Secret 從 `appsettings.Development.json` 的 `Jwt:Secret` 讀取，長度 >= 32 字元
  - [x] Namespace 格式：`AuctionService.Modules.{ModuleName}.{Domain|Application|Infrastructure}`
  - [x] 所有模組共用 `AuctionService.Api` 入口點，Controller 放 `src/AuctionService.Api/Controllers/{Module}/`

- [x] **Plan** — 貼上以下指令到 Copilot Chat：
  ```
  /speckit.plan
  ```
  確認 plan.md 產出包含：
  - [x] Solution 結構圖（sln + 各 csproj）
  - [x] NuGet 套件清單（EF Core Npgsql、MediatR、JWT、Swagger、FluentValidation、xUnit、Testcontainers）
  - [x] Docker Compose 服務：PostgreSQL 5432、pgAdmin 5050
  - [x] `AuctionService.Shared` 需包含：`BaseEntity`、`IEvent`、`ApiResponse<T>`、`GlobalExceptionMiddleware`

- [x] **Tasks** — 貼上以下指令到 Copilot Chat：
  ```
  /speckit.tasks
  ```
  確認 tasks.md 包含以下任務：
  - [x] `dotnet new sln -n AuctionService`
  - [x] `dotnet new webapi -n AuctionService.Api`
  - [x] `dotnet new classlib` × 3（Domain/Application/Infrastructure）× 5 模組 = 15 個 csproj
  - [x] `dotnet new classlib -n AuctionService.Shared`
  - [x] 所有 csproj 加入 sln：`dotnet sln add`
  - [x] 建立各模組間 project reference（Api → 各模組 Application；各模組 Infrastructure → Domain + Application）
  - [x] 建立 `docker-compose.yml`
  - [x] 建立 `Dockerfile`（multi-stage build: `sdk:10.0` → `aspnet:10.0`，非 root 使用者 `appuser`，EXPOSE 8080）
  - [x] 建立 `appsettings.Development.json`（含 DB 連線字串、JWT config）
  - [x] 建立 `.gitignore`（含 `bin/`, `obj/`, `*.user`, `*.suo`, `.env`, `appsettings.*.json`）
  - [x] 建立 `.env.example`（說明所有必填環境變數）
  - [x] 在 `Program.cs` 設定 Swagger、JWT middleware、各模組 DI 註冊、全域 exception handler

- [x] **Implement** — 貼上以下指令到 Copilot Chat：
  ```
  /speckit.implement
  ```

- [x] **Checklist** — 貼上以下指令到 Copilot Chat：
  ```
  /speckit.checklist
  ```

### 1-C. 本機驗收（Terminal）

```bash
# 建置
dotnet build

# 啟動 PostgreSQL
docker compose up -d

# 等待 DB 就緒後執行 migration（各模組）
dotnet ef database update --project src/Modules/Member/Member.Infrastructure

# 啟動 API
dotnet run --project src/AuctionService.Api
# 開啟瀏覽器：https://localhost:5001/swagger
```

驗收檢查：
- [x] `dotnet build` 零錯誤、零 warning（或 warning 全部為已知可接受項）
- [x] `docker compose up -d` PostgreSQL port 5432 正常啟動
- [x] `https://localhost:5001/swagger` Swagger UI 可開啟，顯示所有模組 Controller
- [x] Health check endpoint `GET /health` 回傳 200
- [x] `dotnet test` 全綠（即使只有少量測試）

### 1-D. Commit & PR

```bash
git add .
git commit -m "feat(scaffold): ASP.NET Core 10 Modular Monolith 後端骨架

- 建立 5 個模組結構（Member/Auction/Bidding/Ordering/Notification）
- 每模組 Domain/Application/Infrastructure 三層
- PostgreSQL Schema 隔離 + EF Core
- MediatR 事件匯流排、JWT 驗證、Swagger UI
- Docker Compose for local PostgreSQL
- AuctionService.Shared：ApiResponse<T>、BaseEntity、GlobalExceptionMiddleware"

git push -u origin 001-backend-scaffold
```

- [ ] 在 GitHub 建立 PR：`001-backend-scaffold` → `develop`
- [ ] PR 通過後 Merge，刪除分支：
  ```bash
  git checkout develop
  git pull origin develop
  git branch -d 001-backend-scaffold
  ```

---

## Phase 2 — Member 模組

**Branch**: `002-member-module` (from `develop`)  
**依賴**: Phase 1 已 merge

### 2-A. 建立分支

```bash
git checkout develop
git pull origin develop
git checkout -b 002-member-module
```

### 2-B. SpecKit 流程

- [ ] **Specify**：
  ```
  /speckit.specify 實作 Member 模組：使用者以 email + username + password 註冊帳號（重複 email 回 409）、以 email + password 登入取得 JWT Access Token（15分鐘）及 Refresh Token（7天）、使用 Refresh Token 換取新 Access Token（Token Rotation）、登出（使 Refresh Token 失效）、查詢自己的個人資料、更新個人資料（username、顯示名稱），密碼使用 BCrypt 雜湊儲存，schema: member，tables: users, refresh_tokens
  ```
- [ ] **Clarify**：`/speckit.clarify`
  - [ ] Access Token 有效期：15 分鐘
  - [ ] Refresh Token 有效期：7 天，旋轉策略（使用後舊 token 失效）
  - [ ] 重複 email 回 409 Conflict，錯誤訊息：`"Email already registered"`
- [ ] **Plan**：`/speckit.plan`
- [ ] **Tasks**：`/speckit.tasks`
- [ ] **Implement**：`/speckit.implement`
- [ ] **Checklist**：`/speckit.checklist`

### 2-C. TDD 重點（實作前先寫測試）

```bash
# 先建立測試檔，確認測試為 Red，再實作
dotnet test --filter "Member" # 預期初始為 Red
```

- [ ] Red: `RegisterCommandHandlerTests` — 重複 email → `DomainException` (409)
- [ ] Red: `LoginCommandHandlerTests` — 錯誤密碼 → `UnauthorizedException` (401)
- [ ] Red: `RefreshTokenCommandHandlerTests` — 過期 token → 401
- [ ] Green: 實作各 Command Handler
- [ ] Refactor: 抽取 `IPasswordHasher`、`IJwtTokenService` 介面到 Application 層

### 2-D. 本機驗收

```bash
dotnet test --filter "Member" --collect:"XPlat Code Coverage"
# 確認覆蓋率 > 80%

# 手動測試
curl -X POST https://localhost:5001/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"test@test.com","username":"testuser","password":"Test@1234"}'
# 預期：201

curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@test.com","password":"Test@1234"}'
# 預期：200 含 accessToken + refreshToken
```

- [ ] `POST /api/auth/register` → 201 / 409（重複 email）
- [ ] `POST /api/auth/login` → 200 + token / 401（錯誤密碼）
- [ ] `POST /api/auth/refresh` → 200 + 新 token
- [ ] `POST /api/auth/logout` → 204
- [ ] `GET /api/users/me` → 200（需 Bearer token）/ 401（無 token）
- [ ] 單元測試覆蓋率 > 80%

### 2-E. Commit & PR

```bash
git add .
git commit -m "feat(member): 會員註冊/登入/JWT/Refresh Token 模組

- RegisterCommand、LoginCommand、RefreshTokenCommand、LogoutCommand
- BCrypt 密碼雜湊、JWT HS256 生成
- member schema migration（users, refresh_tokens tables）
- 單元測試覆蓋率 > 80%"

git push -u origin 002-member-module
```

- [ ] 建立 PR：`002-member-module` → `develop` → Merge
- [ ] Merge 後：
  ```bash
  git checkout develop ; git pull ; git branch -d 002-member-module
  ```

---

## Phase 3 — Auction 模組

**Branch**: `003-auction-module` (from `develop`)  
**依賴**: Phase 2 已 merge

### 3-A. 建立分支

```bash
git checkout develop
git pull origin develop
git checkout -b 003-auction-module
```

### 3-B. SpecKit 流程

- [ ] **Specify**：
  ```
  /speckit.specify 實作 Auction 模組：賣家建立拍賣商品（標題、描述、起標價、結標時間、分類、最多5張圖片 URL）、編輯草稿商品、上架商品（Draft→Active）、瀏覽商品列表（支援分類篩選、關鍵字搜尋、分頁 pageSize=20）、商品詳細頁（含目前最高出價）、將商品加入/移除追蹤清單、查詢追蹤清單、排程結標（結標時間到自動將 Active→Ended，若有出價則發布 AuctionWonEvent 含 AuctionId/WinnerId/SoldAmount）；商品狀態機：Draft→Active→Ended；schema: auction，tables: auctions, auction_images, categories, watchlist
  ```
- [ ] **Clarify**：`/speckit.clarify`
  - [ ] 結標排程：使用 Background Service 每分鐘掃描過期 Active 商品
  - [ ] 最高出價查詢：Auction 模組透過 `IBiddingQueryService`（Shared Interface）查詢，避免直接依賴 Bidding 模組
- [ ] **Plan**：`/speckit.plan`
- [ ] **Tasks**：`/speckit.tasks`
- [ ] **Implement**：`/speckit.implement`
- [ ] **Checklist**：`/speckit.checklist`

### 3-C. TDD 重點

- [ ] Red: `AuctionStatusMachineTests` — Draft→Active→Ended 狀態流轉
- [ ] Red: `AuctionEndServiceTests` — 結標時 `AuctionWonEvent` 正確發布（含 WinnerId、SoldAmount）
- [ ] Red: `CreateAuctionCommandTests` — 驗證 end_time > now，否則 422
- [ ] Green + Refactor

### 3-D. 本機驗收

```bash
dotnet test --filter "Auction" --collect:"XPlat Code Coverage"
```

- [ ] `POST /api/auctions` → 201（需 JWT）
- [ ] `PUT /api/auctions/{id}` → 200（需為商品擁有者）/ 403
- [ ] `PATCH /api/auctions/{id}/publish` → 200（Draft→Active）
- [ ] `GET /api/auctions?category=&q=&page=1&pageSize=20` → 200 + 分頁
- [ ] `GET /api/auctions/{id}` → 200 含商品詳情
- [ ] `POST /api/auctions/{id}/watchlist` → 204（加入追蹤）
- [ ] `DELETE /api/auctions/{id}/watchlist` → 204（移除追蹤）
- [ ] `GET /api/watchlist` → 200 追蹤清單
- [ ] 結標觸發 `AuctionWonEvent` 正確進入 MediatR pipeline
- [ ] 單元測試覆蓋率 > 80%

### 3-E. Commit & PR

```bash
git add .
git commit -m "feat(auction): 拍賣商品 CRUD、狀態機、追蹤清單、結標事件

- CreateAuction、UpdateAuction、PublishAuction Commands
- 商品列表查詢（分類/關鍵字/分頁）
- AuctionEndBackgroundService 排程結標
- AuctionWonEvent 發布機制
- Watchlist 加入/移除/查詢
- auction schema migration
- 單元測試覆蓋率 > 80%"

git push -u origin 003-auction-module
```

- [ ] 建立 PR：`003-auction-module` → `develop` → Merge
- [ ] Merge 後：
  ```bash
  git checkout develop ; git pull ; git branch -d 003-auction-module
  ```

---

## Phase 4 — Bidding 模組

**Branch**: `004-bidding-module` (from `develop`)  
**依賴**: Phase 2 + Phase 3 已 merge

### 4-A. 建立分支

```bash
git checkout develop
git pull origin develop
git checkout -b 004-bidding-module
```

### 4-B. SpecKit 流程

- [ ] **Specify**：
  ```
  /speckit.specify 實作 Bidding 模組：使用者對 Active 狀態的拍賣商品出價（出價金額必須大於目前最高出價，否則回 422；商品非 Active 狀態回 409），查詢特定商品的出價歷史（依出價時間降序、分頁），查詢我的出價清單（顯示每筆出價的狀態：leading 我是目前最高出價者 / outbid 已被超越 / won 得標 / lost 未得標），schema: bidding，table: bids
  ```
- [ ] **Clarify**：`/speckit.clarify`
  - [ ] 最高出價查詢：從 `bidding.bids` 以 `MAX(amount) WHERE auction_id=?` 取得，同模組內查詢
  - [ ] 出價狀態「won/lost」在結標後更新（監聽 `AuctionWonEvent`）
- [ ] **Plan**：`/speckit.plan`
- [ ] **Tasks**：`/speckit.tasks`
- [ ] **Implement**：`/speckit.implement`
- [ ] **Checklist**：`/speckit.checklist`

### 4-C. TDD 重點

- [ ] Red: `PlaceBidCommandTests` — 出價 ≤ 最高價 → 422
- [ ] Red: `PlaceBidCommandTests` — 商品非 Active → 409
- [ ] Red: `PlaceBidCommandTests` — 不能對自己的拍賣出價 → 403
- [ ] Red: `GetMyBidsQueryTests` — leading/outbid/won/lost 狀態正確
- [ ] Green + Refactor

### 4-D. 本機驗收

```bash
dotnet test --filter "Bidding" --collect:"XPlat Code Coverage"
```

- [ ] `POST /api/bids` `{"auctionId":"...","amount":100}` → 201（需 JWT）
- [ ] `POST /api/bids` 金額不足 → 422（含欄位錯誤說明）
- [ ] `POST /api/bids` 商品已結標 → 409
- [ ] `GET /api/bids?auctionId={id}` → 200 + 出價歷史（分頁）
- [ ] `GET /api/bids/my` → 200 + 我的出價（含 leading/outbid 狀態）
- [ ] 單元測試覆蓋率 > 80%

### 4-E. Commit & PR

```bash
git add .
git commit -m "feat(bidding): 競標出價、出價歷史、我的出價狀態模組

- PlaceBidCommand（出價驗證：金額/狀態/權限）
- GetAuctionBidsQuery（出價歷史分頁）
- GetMyBidsQuery（leading/outbid/won/lost 狀態）
- AuctionWonEvent handler（更新 won/lost 狀態）
- bidding schema migration（bids table）
- 單元測試覆蓋率 > 80%"

git push -u origin 004-bidding-module
```

- [ ] 建立 PR：`004-bidding-module` → `develop` → Merge
- [ ] Merge 後：
  ```bash
  git checkout develop ; git pull ; git branch -d 004-bidding-module
  ```

---

## Phase 5 — Ordering 模組

**Branch**: `005-ordering-module` (from `develop`)  
**依賴**: Phase 3 已 merge（`AuctionWonEvent`）

### 5-A. 建立分支

```bash
git checkout develop
git pull origin develop
git checkout -b 005-ordering-module
```

### 5-B. SpecKit 流程

- [ ] **Specify**：
  ```
  /speckit.specify 實作 Ordering 模組：監聽 AuctionWonEvent 自動建立訂單（status: PendingPayment，buyer_id=WinnerId，seller_id=AuctionOwnerId，amount=SoldAmount，冪等保護：同一 auction_id 不重複建立）、買家查詢我的訂單列表（分頁，依狀態篩選）、賣家查詢銷售訂單列表、查詢訂單詳情、賣家填入出貨資訊並更新狀態（Paid→Shipped，需填 tracking_number）、買家確認收貨（Shipped→Completed）、發布 OrderStatusChangedEvent（含 OrderId、NewStatus、BuyerId、SellerId），schema: ordering，table: orders
  ```
- [ ] **Clarify**：`/speckit.clarify`
  - [ ] 付款狀態（PendingPayment→Paid）目前由 mock 直接觸發，Payment 模組未來才實作
  - [ ] 冪等：`orders` 表對 `auction_id` 加 UNIQUE 約束
- [ ] **Plan**：`/speckit.plan`
- [ ] **Tasks**：`/speckit.tasks`
- [ ] **Implement**：`/speckit.implement`
- [ ] **Checklist**：`/speckit.checklist`

### 5-C. TDD 重點（整合測試使用 Testcontainers）

```bash
dotnet test --filter "Ordering" --collect:"XPlat Code Coverage"
```

- [ ] Red: `AuctionWonEventHandlerIntegrationTests` — 事件觸發後 `ordering.orders` 插入正確資料（Testcontainers）
- [ ] Red: `AuctionWonEventHandlerIntegrationTests` — 同一 `auction_id` 重複觸發不建立第二筆（冪等）
- [ ] Red: `OrderStatusTransitionTests` — 狀態流轉 PendingPayment→Paid→Shipped→Completed
- [ ] Red: `OrderStatusTransitionTests` — 非法流轉（如 PendingPayment→Completed 直接）→ 422
- [ ] Green + Refactor

### 5-D. 本機驗收

- [ ] `AuctionWonEvent` 觸發後 `ordering.orders` 自動插入一筆 `PendingPayment` 訂單
- [ ] `GET /api/orders?role=buyer` → 200 買家訂單列表（分頁）
- [ ] `GET /api/orders?role=seller` → 200 賣家銷售訂單列表
- [ ] `GET /api/orders/{id}` → 200 訂單詳情 / 403（非買賣家）
- [ ] `POST /api/orders/{id}/pay` → 200（mock 觸發付款，PendingPayment→Paid）
- [ ] `POST /api/orders/{id}/ship` `{"trackingNumber":"..."}` → 200（Paid→Shipped，需為賣家）
- [ ] `POST /api/orders/{id}/complete` → 200（Shipped→Completed，需為買家）
- [ ] 整合測試全綠

### 5-E. Commit & PR

```bash
git add .
git commit -m "feat(ordering): 訂單生命週期管理模組

- AuctionWonEvent Handler（自動建立訂單，冪等保護）
- 訂單狀態機：PendingPayment→Paid→Shipped→Completed
- 買家/賣家角色分離的訂單列表查詢
- OrderStatusChangedEvent 發布
- ordering schema migration（orders table）
- Testcontainers 整合測試全綠"

git push -u origin 005-ordering-module
```

- [ ] 建立 PR：`005-ordering-module` → `develop` → Merge
- [ ] Merge 後：
  ```bash
  git checkout develop ; git pull ; git branch -d 005-ordering-module
  ```

---

## Phase 6 — Notification 模組

**Branch**: `006-notification-module` (from `develop`)  
**依賴**: Phase 3 + Phase 5 已 merge（所有模組事件）

### 6-A. 建立分支

```bash
git checkout develop
git pull origin develop
git checkout -b 006-notification-module
```

### 6-B. SpecKit 流程

- [ ] **Specify**：
  ```
  /speckit.specify 實作 Notification 模組：監聽 AuctionWonEvent（寫通知給買家「恭喜得標！請至訂單頁付款」）、監聽 OrderStatusChangedEvent（Paid→通知賣家「訂單已付款，請出貨」；Shipped→通知買家「商品已出貨」；Completed→通知賣家「交易完成」）、查詢我的通知列表（分頁，最新在前，含 is_read 狀態）、標記單筆通知為已讀、標記全部通知為已讀、查詢未讀通知數量，schema: notification，table: notifications
  ```
- [ ] **Clarify**：`/speckit.clarify`
  - [ ] 通知不刪除，僅記錄 is_read 狀態
  - [ ] Event Handler 寫通知冪等：以 `(user_id, source_event_id, event_type)` 作為唯一鍵
- [ ] **Plan**：`/speckit.plan`
- [ ] **Tasks**：`/speckit.tasks`
- [ ] **Implement**：`/speckit.implement`
- [ ] **Checklist**：`/speckit.checklist`

### 6-C. TDD 重點

- [ ] Red: `AuctionWonEventNotificationHandlerTests` — 事件觸發 → `notifications` 表插入正確買家通知
- [ ] Red: `OrderStatusChangedEventNotificationHandlerTests` — 各狀態觸發正確的通知對象與訊息
- [ ] Red: `GetUnreadCountQueryTests` — 未讀數量正確
- [ ] Red: `MarkAsReadCommandTests` — 標記已讀冪等（重複呼叫不報錯）
- [ ] Red: `MarkAllAsReadCommandTests` — 只標記屬於該用戶的通知
- [ ] Green + Refactor

### 6-D. 本機驗收

```bash
dotnet test --filter "Notification" --collect:"XPlat Code Coverage"
```

- [ ] 觸發 `AuctionWonEvent` → `notifications` 表自動插入買家通知
- [ ] 觸發 `OrderStatusChangedEvent (Paid)` → 賣家通知自動產生
- [ ] `GET /api/notifications` → 200 + 分頁（最新在前）
- [ ] `GET /api/notifications/unread-count` → 200 `{"count": N}`
- [ ] `PATCH /api/notifications/{id}/read` → 204（重複呼叫仍 204）
- [ ] `PATCH /api/notifications/read-all` → 204
- [ ] 單元測試覆蓋率 > 80%

### 6-E. Commit & PR

```bash
git add .
git commit -m "feat(notification): 通知模組（事件驅動寫入、已讀標記）

- AuctionWonEvent/OrderStatusChangedEvent Handlers
- 通知列表查詢（分頁）、未讀數量查詢
- 標記已讀（單筆/全部）、冪等保護
- notification schema migration（notifications table）
- 單元測試覆蓋率 > 80%"

git push -u origin 006-notification-module
```

- [ ] 建立 PR：`006-notification-module` → `develop` → Merge
- [ ] Merge 後：
  ```bash
  git checkout develop ; git pull ; git branch -d 006-notification-module
  ```

---

## Phase 7 — 架構邊界驗證（Cross-Module）

**Branch**: `007-arch-tests` (from `develop`)  
**依賴**: Phase 1–6 全部 merge

### 7-A. 建立分支

```bash
git checkout develop
git pull origin develop
git checkout -b 007-arch-tests
```

### 7-B. ArchUnitNET 架構邊界測試

```bash
# 建立架構測試專案
dotnet new xunit -n AuctionService.ArchTests -o tests/AuctionService.ArchTests
dotnet add tests/AuctionService.ArchTests/AuctionService.ArchTests.csproj package ArchUnitNET
dotnet sln add tests/AuctionService.ArchTests/AuctionService.ArchTests.csproj
```

撰寫測試規則（`tests/AuctionService.ArchTests/ModuleBoundaryTests.cs`）：
- [ ] Member 模組不得直接參考 Auction/Bidding/Ordering/Notification 的 Application/Infrastructure 層
- [ ] Auction 模組不得直接參考 Bidding/Ordering/Notification 的 Application/Infrastructure 層
- [ ] 任何模組不得參考其他模組的 Infrastructure 層
- [ ] 所有跨模組通訊只允許透過 `AuctionService.Shared` 或 MediatR `INotification`

```bash
dotnet test tests/AuctionService.ArchTests/
# 預期：全綠
```

### 7-C. 效能基準測試（k6）

```bash
# 安裝 k6（若未安裝）
# Windows: winget install k6

# 啟動 API
dotnet run --project src/AuctionService.Api --configuration Release

# 執行 p95 基準測試
k6 run --vus 50 --duration 30s - <<EOF
import http from 'k6/http';
import { check } from 'k6';
export default function () {
  let res = http.get('https://localhost:5001/api/auctions?page=1&pageSize=20');
  check(res, { 'p95 < 200ms': (r) => r.timings.duration < 200 });
}
EOF
```

- [ ] `GET /api/auctions` p95 < 200ms（50 VU，30s）
- [ ] `POST /api/bids` p95 < 200ms（認證情境）

### 7-D. 全系統整合驗收

```bash
dotnet test --collect:"XPlat Code Coverage"
# 確認所有測試全綠
```

- [ ] `dotnet test` 全綠（所有模組單元 + 整合測試）
- [ ] ArchUnit 架構邊界測試全綠
- [ ] k6 效能基準達標（p95 < 200ms）
- [ ] `GET /health` → 200（含 DB 連線狀態）

### 7-E. Commit、PR & Merge to master

```bash
git add .
git commit -m "test(arch): 架構邊界驗證與效能基準測試

- ArchUnitNET 模組邊界規則（禁止跨模組直接參考）
- k6 效能基準測試腳本
- 全系統 dotnet test 全綠確認"

git push -u origin 007-arch-tests
```

- [ ] 建立 PR：`007-arch-tests` → `develop` → Merge
- [ ] **建立 PR：`develop` → `master` → Merge（第一個完整可用版本）**
  ```bash
  git checkout master
  git merge develop
  git push origin master
  git tag -a v1.0.0 -m "AuctionService v1.0.0：5模組 Modular Monolith 後端完成"
  git push origin v1.0.0
  ```

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
