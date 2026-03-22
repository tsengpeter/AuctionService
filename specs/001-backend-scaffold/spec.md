# Feature Specification: Backend Modular Monolith Scaffold

**Feature Branch**: `001-backend-scaffold`  
**Created**: 2026-03-19  
**Status**: Draft  
**Input**: User description: "建立 ASP.NET Core 10 Modular Monolith 後端骨架，包含 Member、Auction、Bidding、Ordering、Notification 五個模組，每模組採 Domain/Application/Infrastructure 三層結構，使用 PostgreSQL Schema 隔離，MediatR 作為模組間 In-Memory 事件匯流排，JWT HS256 身份驗證，FluentValidation 輸入驗證，Swagger/OpenAPI 文件，xUnit + Testcontainers 整合測試，統一 ApiResponse<T> 回應格式"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - 開發者可在本機一鍵啟動並驗證後端服務 (Priority: P1)

開發者將專案 clone 到本機後，執行啟動指令，即可看到後端 API 服務正常運行、所有模組端點可存取、資料庫連線正常。

**Why this priority**: 這是所有後續開發的基礎。若開發者無法在本機快速啟動服務，所有功能開發都將受阻。

**Independent Test**: 可透過「執行啟動指令 + 開啟 API 文件頁面」獨立驗證，不需要任何業務功能即可交付開發環境就緒的價值。

**Acceptance Scenarios**:

1. **Given** 開發者已 clone 專案並設定環境變數，**When** 執行資料庫啟動與 API 啟動指令，**Then** API 服務於指定 port 正常回應，API 文件頁面可開啟並列出所有模組端點
2. **Given** API 正常運行，**When** 呼叫健康檢查端點，**Then** 回傳服務正常及資料庫連線狀態
3. **Given** 環境變數設定不完整，**When** 嘗試啟動服務，**Then** 回報明確的設定錯誤訊息，指出缺少哪個設定項目

---

### User Story 2 - 開發者可在模組邊界內獨立開發新功能而不影響其他模組 (Priority: P2)

開發者在 Member 模組新增功能時，不需修改 Auction、Bidding、Ordering、Notification 模組的任何檔案，各模組有清楚的資料儲存隔離邊界。

**Why this priority**: 模組邊界是系統長期可維護的核心。確保模組獨立後，多名開發者可並行開發不同模組，並大幅降低意外耦合的風險。

**Independent Test**: 可透過「在任一模組新增空白 class 並執行建置」驗證模組可獨立編譯；「查詢各模組 DB Schema」驗證資料隔離。

**Acceptance Scenarios**:

1. **Given** 五個模組各自有獨立的資料儲存區域，**When** 開發者修改 Member 模組的資料結構，**Then** 其他模組的資料結構不受影響
2. **Given** 模組間需要傳遞事件，**When** Auction 模組發布結標事件，**Then** Ordering 和 Notification 模組可各自訂閱處理，且兩個模組間彼此不直接相依
3. **Given** 開發者在 Bidding 模組執行測試，**When** 測試完成，**Then** 僅測試 Bidding 模組的邏輯，不需啟動其他模組的基礎建設

---

### User Story 3 - 開發者可透過一致格式的 API 回應快速整合前端 (Priority: P3)

前端開發者呼叫任何模組的 API 端點，都能收到相同結構的回應格式，包含成功狀態、資料內容、錯誤訊息，無論呼叫哪個模組都行為一致。

**Why this priority**: 一致的 API 合約減少前後端整合摩擦，降低前端針對不同錯誤格式撰寫特殊處理邏輯的需求。

**Independent Test**: 可透過「對任意兩個不同模組的端點發送請求，比較回應結構」獨立驗證。

**Acceptance Scenarios**:

1. **Given** 前端呼叫成功的 API 操作，**When** 收到回應，**Then** 回應包含成功標示、資料內容，結構與其他模組的成功回應完全相同
2. **Given** 前端送出含有錯誤欄位的請求，**When** 收到驗證錯誤回應，**Then** 回應包含失敗標示、明確的欄位錯誤訊息，HTTP 狀態碼為 422
3. **Given** 前端呼叫需要身份驗證的端點但未提供有效憑證，**When** 收到回應，**Then** 回應包含失敗標示、錯誤說明，HTTP 狀態碼為 401

---

### User Story 4 - 開發者可執行自動化測試確認程式的正確性 (Priority: P4)

開發者執行測試指令，可對各模組的業務邏輯進行單元測試，以及對跨模組整合情境進行整合測試，測試環境自動建立與清除，不依賴外部服務狀態。

**Why this priority**: 自動化測試基礎建設確保後續每個模組開發都能遵循 TDD 流程，也保障重構不會破壞已有功能。

**Independent Test**: 可透過「執行測試指令並確認全綠」獨立驗證。

**Acceptance Scenarios**:

1. **Given** 開發者在本機執行測試指令，**When** 測試執行完畢，**Then** 單元測試無需外部服務（資料庫、網路），可在任何環境執行
2. **Given** 需要執行整合測試，**When** 測試啟動，**Then** 測試環境自動建立隔離的資料庫實例，測試結束後自動清除，不污染本地開發資料
3. **Given** 開發者執行所有測試，**When** 某個模組的業務邏輯改變，**Then** 受影響的測試案例明確指出哪個行為不符預期

---

### Edge Cases

- 模組事件發布時若訂閱者拋出例外，發布者是否繼續執行？（假設：是，採 fire-and-forget，錯誤記錄至 log，不回滾發布者的 transaction）
- 同一個事件被兩個模組訂閱，其中一個處理失敗，另一個是否仍應成功？（假設：是，各 Handler 獨立隔離）
- 缺少必要環境變數時，服務應在啟動時立即 fail-fast 並明確報錯，而非在首次使用時才出錯
- JWT token 過期或簽章無效時，ASP.NET Core 預設回傳空 body 401 — 系統 MUST 透過 `OnChallenge`/`OnForbidden` event 攔截並覆寫為統一 `ApiResponse` 格式，確保前端不需特殊處理驗證錯誤路徑

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: 系統 MUST 提供統一的 API 入口點，所有模組的端點均透過同一個服務對外暴露
- **FR-002**: 系統 MUST 將五個模組（Member、Auction、Bidding、Ordering、Notification）的資料儲存在各自獨立的邏輯區域，各模組不得直接存取其他模組的資料儲存區
- **FR-003**: 系統 MUST 提供 API 互動文件，列出所有可用端點、請求/回應結構，開發者無需閱讀程式碼即可了解 API 功能
- **FR-004**: 所有 API 回應 MUST 遵循統一的結構：成功時 `{ success: true, data: T, statusCode: number }`；驗證失敗（422）時 `{ success: false, errors: [{ field: string, message: string }], statusCode: 422 }`；其他錯誤時 `{ success: false, error: string, statusCode: number }`
- **FR-005**: 系統 MUST 支援 JWT 身份驗證，所有需要登入的端點在 token 缺失、過期或簽章無效時回傳 `{ success: false, error: "Unauthorized", statusCode: 401 }`；403 Forbidden 同樣包裝成統一 ApiResponse 格式，由 JWT `OnChallenge`/`OnForbidden` event 或 `GlobalExceptionMiddleware` 統一處理
- **FR-006**: 模組間 MUST 能透過非同步事件機制通訊，事件發布者不需知道有哪些訂閱者存在
- **FR-007**: 系統 MUST 對所有進入端點的請求資料進行驗證，驗證失敗時回傳包含具體欄位錯誤說明的 422 回應
- **FR-008**: 系統 MUST 提供健康檢查端點，回報服務狀態及資料庫連線狀態
- **FR-011**: 系統 MUST 使用 .NET 內建 `ILogger<T>` 記錄關鍵事件（請求例外、事件 Handler 失敗），Development 環境以 JSON 格式輸出至 Console；不引入第三方 logging 套件（骨架階段）
- **FR-009**: 系統 MUST 在開發環境提供 Docker Compose 設定，讓開發者可一鍵啟動所需的外部依賴（資料庫）
- **FR-010**: 所有模組 MUST 支援撰寫單元測試（業務邏輯不依賴外部服務）與整合測試（使用隔離的臨時資料庫）
- **FR-012**: 系統 MUST 提供 `Dockerfile`，使用多階段建置（multi-stage build）將應用程式打包為可部署的 Docker Image；執行階段 Image 不含 .NET SDK；容器以非 root 使用者執行；所有敏感設定透過環境變數注入，不寫入 Image

### Key Entities

- **Module（模組）**: 代表一個業務領域邊界，包含 Domain（業務規則與實體）、Application（使用案例與指令）、Infrastructure（資料存取實作）三層，對外僅透過 API 層暴露端點
- **Domain Event（領域事件）**: 代表模組內發生的重要業務事實，可被其他模組訂閱，事件名稱以過去式命名（如 AuctionWonEvent），包含事件發生時的完整業務資料
- **API Response（統一回應）**: 所有 API 操作的回應容器。成功：`{ success: true, data: T, statusCode }`；驗證錯誤（422）：`{ success: false, errors: [{ field, message }], statusCode: 422 }`；一般錯誤：`{ success: false, error: string, statusCode }`，無論哪個模組皆相同

---

## Clarifications

### Session 2026-03-19

- Q: Solution 專案命名與資料夾結構為何？ → A: 每模組一個 csproj，路徑 `src/Modules/{ModuleName}/{ModuleName}.csproj`，內含 `Domain/`、`Application/`、`Infrastructure/` 子資料夾，不拆分 15 個獨立 csproj
- Q: `ApiResponse<T>` 驗證錯誤欄位結構為何？ → A: `{ success: false, errors: [{ field: "email", message: "..." }] }`，驗證失敗回傳 errors 陣列，每個元素含 field 與 message
- Q: EF Core Migration 執行時機策略為何？ → A: 開發環境手動 CLI（`dotnet ef database update`）；整合測試由 Testcontainers 啟動後自動套用 migration，不在應用程式啟動時自動執行
- Q: Logging 策略為何？ → A: 使用內建 `ILogger<T>`，Development 環境輸出至 Console（JSON 格式），骨架階段不引入第三方套件
- Q: JWT token 無效時 401/403 回應格式為何？ → A: 攔截所有 401/403 並統一包裝成 `{ success: false, error: "Unauthorized", statusCode: 401 }`，由 `GlobalExceptionMiddleware` 或 JWT `OnChallenge` event 處理

---

## Assumptions

- JWT Secret 從環境設定讀取，長度至少 32 字元，不可 hardcode 在程式碼中
- 各模組對應獨立的資料庫 Schema：member、auction、bidding、ordering、notification
- **Solution 結構**：每模組一個 csproj（`src/Modules/{ModuleName}/{ModuleName}.csproj`），Domain/Application/Infrastructure 為該 csproj 內的資料夾，非獨立 csproj
- 模組間同步通訊（如跨模組查詢）透過 Shared 層定義的 Interface，由 DI 注入
- 模組間非同步通訊透過 In-Memory 事件匯流排，不使用外部 Message Broker（此為初期設計，後續可升級）
- 每個模組維護自己的資料庫 Migration 歷史，各自獨立管理 Schema 變更
- **Migration 執行策略**：開發環境手動執行 `dotnet ef database update --project src/Modules/{Module}/{Module}.csproj`；整合測試中 Testcontainers 啟動 PostgreSQL 後由測試 fixture 自動套用 migration；應用程式啟動時不自動執行 migration
- Swagger 文件僅在非 Production 環境開啟

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 開發者從 clone 到第一次成功啟動服務（含 API 文件可存取）的時間少於 10 分鐘
- **SC-002**: 執行 `dotnet build` 產生零編譯錯誤，所有模組可獨立編譯
- **SC-003**: 執行 `dotnet test` 全綠，初始骨架測試通過率 100%
- **SC-004**: 任意 API 端點的回應結構一致性達 100%（所有端點均符合統一格式）
- **SC-005**: 修改任一模組的業務邏輯，不需修改其他模組任何檔案（零跨模組副作用）
- **SC-006**: 健康檢查端點在服務正常時回應時間 < 100ms
- **SC-007**: `docker build -t auctionservice:latest .` 零錯誤，`docker run` 啟動後 `GET /health` 回傳 Healthy

