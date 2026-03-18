# Architecture Design: AuctionService 後端模組化單體

**Date**: 2026-03-18  
**Version**: 1.0.0  
**Status**: Proposed

---

## Overview

採用 **ASP.NET Core 10 Modular Monolith** 單一專案，內含 5 個垂直切分的業務模組（Member、Auction、Bidding、Ordering、Notification），以 PostgreSQL Schema 做資料隔離，MediatR 作為模組間非同步事件匯流排。整體部署為單一可執行檔，保留未來切割微服務的結構性彈性。

---

## Current State

- 僅有規格文件（`requirmentspec/`）與 `.specify/` 工作流程設施
- 尚無任何原始碼、solution 或 migration
- `master` 分支乾淨，所有功能分支已清除
- Constitution v1.1.0 定義了 TDD、SOLID、200ms p95 等不可妥協原則

---

## Requirements

### Functional

- 會員註冊/登入（JWT）、帳號管理
- 拍賣商品 CRUD、結標流程、狀態機
- 競標出價、出價歷史查詢
- 得標後自動建立訂單、訂單狀態流轉（PendingPayment → Paid → Shipped → Completed）
- 通知訊息寫入、已讀/未讀管理

### Non-Functional

- API p95 < 200ms（Constitution IV）
- 單元測試覆蓋率 > 80%（Constitution II）
- 無 N+1 查詢問題
- JWT 身份驗證、Schema 級別資料隔離
- 模組邊界不得直接跨 DbContext 存取（Constitution I / SOLID）

---

## Proposed Architecture

### 整體結構

```
AuctionService/
├── src/
│   ├── AuctionService.Api/              # Entry point：Controller、DI 組裝、Middleware
│   ├── AuctionService.Shared/           # 跨模組共用：BaseEntity、IEvent、JWT、ApiResponse<T>
│   └── Modules/
│       ├── Member/
│       │   ├── Member.Domain/           # Entity、Value Object、Domain Event
│       │   ├── Member.Application/      # Command/Query (CQRS via MediatR)、Interface
│       │   └── Member.Infrastructure/   # EF Core、Repository 實作
│       ├── Auction/     （同上結構）
│       ├── Bidding/     （同上結構）
│       ├── Ordering/    （同上結構）
│       └── Notification/（同上結構）
├── tests/
│   ├── Member.UnitTests/
│   ├── Member.IntegrationTests/
│   ├── Auction.UnitTests/
│   └── ...（每模組對應）
├── docker-compose.yml
└── AuctionService.sln
```

### 模組邊界與職責

| 模組 | 職責 | 對外 API 前綴 | DB Schema |
|---|---|---|---|
| **Member** | 使用者身份、JWT 發放、Refresh Token | `/api/auth`, `/api/users` | `member` |
| **Auction** | 拍賣商品 CRUD、狀態機、結標觸發 | `/api/auctions` | `auction` |
| **Bidding** | 出價驗證、最高價追蹤、出價歷史 | `/api/bids` | `bidding` |
| **Ordering** | 監聽 `AuctionWonEvent`、訂單生命週期、出貨更新 | `/api/orders` | `ordering` |
| **Notification** | 訊息寫入、已讀標記、歷史查詢 | `/api/notifications` | `notification` |

### 模組間通訊設計

```
[Auction Module]
  結標完成 → Publish AuctionWonEvent (MediatR)
                ↓                        ↓
        [Ordering Module]        [Notification Module]
        建立 Order               通知買家「恭喜得標」

[Ordering Module]（付款後）
  → Publish OrderStatusChangedEvent
                ↓
        [Notification Module]
        通知賣家「請出貨」
```

**規則**：模組間**禁止直接相依** Application/Infrastructure 層，只能透過 `INotification`（MediatR）或 `Shared` 層定義的 Interface 通訊。

### 統一 API 回應結構（`AuctionService.Shared`）

```csharp
record ApiResponse<T>(bool Success, T? Data, string? Error, int StatusCode);
```

所有 Controller 回傳此結構，滿足 Constitution III（一致性）。

---

## Key Decisions & Trade-offs

### 1. Modular Monolith vs. Microservices

- **Pros**：單一部署、直接方法呼叫效能佳、開發複雜度低、DB transaction 跨模組仍可用
- **Cons**：模組邊界需靠 Convention 維護（無硬性 network 隔離）、單點擴展無法模組級橫向擴充
- **Alternatives**：微服務（網路複雜度過高，目前規模不合）、Layer 架構（無模組邊界）
- **Decision**：**Modular Monolith**，採 vertical slice 確保結構上可日後切割

### 2. 每模組三層（Domain / Application / Infrastructure）vs. 垂直 Slice

- **Pros（三層）**：職責清晰、Application 層可獨立測試、符合 SOLID
- **Cons（三層）**：初期 boilerplate 較多
- **Alternatives**：CQRS Vertical Slice（每個 Feature 自成一 class）
- **Decision**：**三層 + MediatR CQRS**，兼顧可測試性與彈性；Vertical Slice 留作未來可選重構方向

### 3. 模組間溝通：MediatR In-Memory vs. Outbox Pattern

- **Pros（In-Memory）**：零延遲、零基礎建設依賴、實作簡單
- **Cons（In-Memory）**：不具持久化，應用崩潰可能遺失事件；不具重試機制
- **Alternatives**：Outbox Pattern（DB 寫入 + background poller）；RabbitMQ/Kafka（引入外部依賴）
- **Decision**：**MediatR In-Memory**（Phase 1–6 範疇內適用）；建議在 Ordering/Payment 整合時加入 Outbox 作為 ADR 追蹤項目

### 4. 統一 DbContext vs. 每模組獨立 DbContext

- **Pros（統一）**：跨模組 transaction 容易
- **Cons（統一）**：模組耦合加深，未來切割困難
- **Alternatives**：每模組獨立 DbContext（邊界清晰，但跨模組 transaction 需 saga/補償）
- **Decision**：**每模組獨立 DbContext**，以 PostgreSQL Schema 隔離，跨模組業務邏輯透過事件補償，不使用 distributed transaction

---

## Risks & Mitigations

| 風險 | 說明 | 緩解措施 |
|---|---|---|
| **模組邊界侵蝕** | 開發者繞過事件直接注入其他模組 Service | CI 加入 `ArchUnitNET` 架構測試，禁止跨模組 namespace 直接參考 |
| **In-Memory 事件遺失** | 應用崩潰導致 `AuctionWonEvent` 未被 Ordering 處理 | 訂單頁補充冪等查詢；後期引入 Outbox |
| **N+1 查詢** | 列表頁懶載入導致效能問題 | 所有 Query Handler 強制使用 `AsNoTracking` + 明確 `Include`；整合測試含查詢計數斷言 |
| **JWT Secret 管理** | 硬碼或環境變數外洩 | 使用 ASP.NET Core `IConfiguration` + Docker secret / Azure Key Vault；絕不 commit secret |
| **測試覆蓋率不足** | 模組間事件流難以單元測試 | Application 層完全 interface-based，使用 Mock；Integration test 用 Testcontainers 起真實 PostgreSQL |

---

## ADR

### ADR-001: 採用每模組獨立 DbContext

- **Context**：需要在單一 PostgreSQL 實例中隔離模組資料，同時保留日後切割彈性
- **Decision**：每個模組自備 `XxxDbContext`，僅存取該模組的 Schema；禁止跨 DbContext 直接 Join
- **Consequences**：跨模組資料聚合由 Application 層組合，或由 Read-only Projection view 處理；無法使用單一 EF Core migration，各模組獨立 migration 歷史
- **Status**：Proposed

### ADR-002: MediatR 作為模組間事件匯流排（含 Outbox 升級路徑）

- **Context**：結標 → 建立訂單 → 通知，需要低耦合的非同步通訊
- **Decision**：Phase 1 使用 MediatR `INotification`；當 Ordering 確認付款場景上線時，Outbox Pattern 作為強制性升級
- **Consequences**：短期簡單，長期需一次性切換為 Outbox；需在任務追蹤中記錄技術債
- **Status**：Proposed

---

## Validation Plan

- **架構邊界驗證**：加入 `ArchUnitNET` 測試，CI 確保模組間無非法參考
- **效能驗證**：`k6` 對 `/api/auctions`、`/api/bids` 進行 p95 基準測試，目標 < 200ms
- **安全驗證**：JWT 未授權端點返回 401，越權操作（他人拍賣）返回 403
- **Rollout**：`master` → `develop` → feature branches，每模組 PR 需通過 build + test 才能合併
