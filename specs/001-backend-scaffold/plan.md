# 實作計畫：後端模組化單體骨架

**分支**: `001-backend-scaffold` | **日期**: 2026-03-22 | **規格**: [spec.md](spec.md)  
**輸入**: `/specs/001-backend-scaffold/spec.md`

## 摘要

建立 ASP.NET Core 10 模組化單體後端骨架，包含 **Member、Auction、Bidding、Ordering、Notification** 五個業務模組，採 Domain/Application/Infrastructure 三層資料夾結構（同一 csproj）。技術棧：MediatR 12 作為 In-Memory 事件匯流排、EF Core 10 + PostgreSQL Schema 隔離、JWT HS256 身份驗證、FluentValidation 12 + MediatR Pipeline Behavior、Swagger/OpenAPI、xUnit + Testcontainers 整合測試，統一 `ApiResponse<T>` 回應格式。

## 技術背景

**語言/版本**: C# 13 / .NET 10 (ASP.NET Core 10)  
**主要依賴**:
- MediatR 12.4.1（CQRS + 事件匯流排）
- FluentValidation 12.x（輸入驗證 + MediatR Pipeline Behavior）
- EF Core 10 + Npgsql.EFCore.PostgreSQL 10（ORM + PostgreSQL Provider）
- Microsoft.AspNetCore.Authentication.JwtBearer 10（JWT HS256）
- Swashbuckle.AspNetCore 8.1（Swagger UI + OpenAPI 文件）
- Testcontainers.PostgreSql 4.x（整合測試容器）
- xUnit 2.9 + FluentAssertions 8.x + NSubstitute 5.x（測試框架）

**儲存**: PostgreSQL 16（Docker），每模組獨立 Schema（`member`, `auction`, `bidding`, `ordering`, `notification`）  
**測試**: xUnit（單元）+ Testcontainers.PostgreSql（整合），ICollectionFixture 共享容器  
**目標平台**: Linux Docker 容器（開發環境 Windows + WSL2）  
**專案類型**: Web API（單一入口點 + 模組化 Library）  
**效能目標**: API p95 回應 < 200ms（憲法 Principle IV）；健康檢查 < 100ms（SC-006）  
**限制**: 模組間不得設定跨 Schema 資料庫外鍵；不引入第三方 Logging 套件；Swagger 僅非 Production 環境開啟  
**規模**: 5 模組、1 個 API Host csproj、1 個 Shared csproj、2 個測試 csproj（共 9 個 csproj）

## 憲法合規檢查

*GATE：Phase 0 研究前必須通過；Phase 1 設計後重新驗證。*

| 原則 | 狀態 | 說明 |
|------|------|------|
| I. 程式碼品質 | ✅ 通過 | 採用 SOLID 設計：DI 貫穿全系統、Domain/Application/Infrastructure 分層、`BaseEntity` 抽象、`IPipelineBehavior` 統一驗證 |
| II. 測試驅動開發 | ✅ 通過 | 所有功能先寫測試（xUnit）；整合測試使用 Testcontainers 隔離；CI 自動執行；骨架測試覆蓋率目標 ≥ 80% |
| III. 使用者體驗一致性 | ✅ 通過 | 所有端點均回傳 `ApiResponse<T>` 統一結構（FR-004）；驗證錯誤固定 422 格式；JWT 401/403 由事件統一攔截（FR-005） |
| IV. 效能要求 | ✅ 通過 | 健康檢查 < 100ms（SC-006）；API p95 < 200ms（憲法要求）；EF Core 配合 Index 設計（Email unique index）；N+1 問題在骨架階段不適用 |
| V. 可觀測性 | ✅ 通過 | `ILogger<T>` 結構化日誌（FR-011）；`GlobalExceptionMiddleware` 記錄完整 stack trace；健康檢查端點（FR-008）；Correlation ID 由 ASP.NET Core 內建 RequestId 提供 |
| 文件語言（繁體中文） | ✅ 通過 | 所有 spec/plan/tasks/quickstart 以繁體中文撰寫；程式碼、commit 訊息、變數名稱以英文 |

**Phase 1 設計後重驗結果**：
- 跨模組邏輯 ID（非資料庫 FK）設計符合原則 I（清晰邊界）及 V（可追蹤）
- `ValidationBehavior` 在 MediatR Pipeline 確保所有 Command/Query 均受驗證，符合原則 II 及 III
- 所有驗證規則以 FluentValidator 表達，可獨立測試，符合原則 II
- **結論：無違規，無需填寫 Complexity Tracking 表格**

## 專案結構

### 本功能文件

```text
specs/001-backend-scaffold/
├── plan.md              # 本文件（/speckit.plan 輸出）
├── research.md          # Phase 0 研究成果
├── data-model.md        # Phase 1 資料模型
├── quickstart.md        # Phase 1 快速啟動指南
├── contracts/           # Phase 1 API 合約
│   └── openapi.yaml     # OpenAPI 3.1 規格
└── tasks.md             # Phase 2 任務清單（/speckit.tasks 指令產出，本指令不建立）
```

### 原始碼（Repository Root）

```text
AuctionService/
├── AuctionService.sln
├── docker-compose.yml
├── .env.example
├── src/
│   ├── AuctionService.Api/
│   │   ├── AuctionService.Api.csproj
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   ├── appsettings.Development.json
│   │   └── Middleware/
│   │       └── GlobalExceptionMiddleware.cs
│   ├── AuctionService.Shared/
│   │   ├── AuctionService.Shared.csproj
│   │   ├── ApiResponse.cs              # ApiResponse<T>, FieldError
│   │   ├── BaseEntity.cs
│   │   ├── IDomainEvent.cs
│   │   └── Behaviors/
│   │       └── ValidationBehavior.cs   # MediatR IPipelineBehavior
│   └── Modules/
│       ├── Member/
│       │   ├── Member.csproj
│       │   ├── Domain/
│       │   │   └── MemberUser.cs
│       │   ├── Application/
│       │   │   └── DependencyInjection.cs
│       │   └── Infrastructure/
│       │       ├── MemberDbContext.cs
│       │       └── Persistence/
│       │           └── Migrations/
│       ├── Auction/
│       │   ├── Auction.csproj
│       │   ├── Domain/
│       │   │   ├── AuctionItem.cs
│       │   │   └── AuctionStatus.cs
│       │   ├── Application/
│       │   │   └── DependencyInjection.cs
│       │   └── Infrastructure/
│       │       └── AuctionDbContext.cs
│       ├── Bidding/
│       │   ├── Bidding.csproj
│       │   ├── Domain/
│       │   │   └── Bid.cs
│       │   ├── Application/
│       │   │   └── DependencyInjection.cs
│       │   └── Infrastructure/
│       │       └── BiddingDbContext.cs
│       ├── Ordering/
│       │   ├── Ordering.csproj
│       │   ├── Domain/
│       │   │   ├── Order.cs
│       │   │   └── OrderStatus.cs
│       │   ├── Application/
│       │   │   └── DependencyInjection.cs
│       │   └── Infrastructure/
│       │       └── OrderingDbContext.cs
│       └── Notification/
│           ├── Notification.csproj
│           ├── Domain/
│           │   └── NotificationRecord.cs
│           ├── Application/
│           │   └── DependencyInjection.cs
│           └── Infrastructure/
│               └── NotificationDbContext.cs
└── tests/
    ├── AuctionService.UnitTests/
    │   ├── AuctionService.UnitTests.csproj
    │   └── Shared/
    │       └── ApiResponseTests.cs     # 驗證 ApiResponse 格式正確性
    └── AuctionService.IntegrationTests/
        ├── AuctionService.IntegrationTests.csproj
        ├── Infrastructure/
        │   └── IntegrationTestFixture.cs  # Testcontainers 設定
        └── Health/
            └── HealthCheckTests.cs        # SC-006 驗證
```

**結構決策**: 採用 Web Application 結構（API Host + Shared + Module csproj）。每模組一個 csproj（非 3 個），Domain/Application/Infrastructure 為資料夾而非獨立專案，符合澄清決策。測試專案分離為 UnitTests（無外部依賴）和 IntegrationTests（Testcontainers），符合 FR-010。
