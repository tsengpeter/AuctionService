# Implementation Plan: 商品拍賣服務

**Branch**: `002-auction-service` | **Date**: 2025-12-02 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/002-auction-service/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

實作完整的商品拍賣服務後端 REST API，提供商品的建立、查詢、管理與使用者追蹤功能。採用 ASP.NET Core 10 Web API 搭配 PostgreSQL 資料庫，使用 Entity Framework Core Code First 工作流程。本專案為微服務架構中的「商品拍賣服務」(Auction Service)，未來將透過獨立的 API Gateway 專案（使用 YARP）提供統一入口。重點功能包括：商品瀏覽與搜尋、商品建立與管理、商品追蹤、以及被動式狀態管理（透過查詢時計算狀態）。

## Technical Context

**Language/Version**: C# 13 / .NET 10  
**Primary Dependencies**: ASP.NET Core 10 Web API, Entity Framework Core 10, Npgsql (PostgreSQL)  
**Storage**: PostgreSQL 16+ (主要資料庫)  
**Testing**: xUnit, FluentAssertions, Moq, Testcontainers (整合測試用 PostgreSQL 容器)  
**Target Platform**: Linux Docker containers (production), Windows/Linux/macOS (development)  
**Project Type**: Web API 微服務架構 (Backend REST API only, no frontend)  
**Performance Goals**: 
- 商品清單查詢 <200ms (p95)
- 商品詳細資訊查詢 <300ms (p95)
- 支援 100+ requests/second
- 支援 1000+ 同時進行的商品
- 系統回應時間目標: P50 ≤ 100ms, P95 ≤ 200ms, P99 ≤ 500ms
- 成功率: ≥ 99.5%
- 記憶體使用: ≤ 512MB (穩態)
- CPU 使用率: ≤ 70% (平均)

**Constraints**: 
- 不使用 AutoMapper，採用 POCO 直接映射 DTO
- 不使用 Minimal APIs，採用傳統 Controller-based API
- 商品狀態不儲存，透過查詢時即時計算 (StartTime/EndTime)
- 與 Bidding Service 整合需記錄所有呼叫日誌  

**Scale/Scope**: 
- 預期支援 10,000+ 商品
- 預期支援 1,000+ 同時上線使用者
- 每個使用者最多追蹤 500 個商品

## 資料庫策略

### 開發環境（本地開發）

**資料庫部署方式**:
- **選項 A（推薦）**: 使用 Docker 容器執行 PostgreSQL 16
  ```bash
  docker run -d --name auctionservice-db \
    -e POSTGRES_USER=auctionservice \
    -e POSTGRES_PASSWORD=Dev@Password123 \
    -e POSTGRES_DB=auctionservice_dev \
    -p 5432:5432 postgres:16-alpine
  ```
- **選項 B**: 本機安裝 PostgreSQL 16（Windows/macOS/Linux）

**連線字串**:
```bash
DB_CONNECTION_STRING="Host=localhost;Port=5432;Database=auctionservice_dev;Username=auctionservice;Password=Dev@Password123"
```

**資料庫初始化流程**（EF Core Code-First）:
```bash
# 1. 建立遷移檔案（開發者在新增/修改實體後執行）
cd src/AuctionService.Infrastructure
dotnet ef migrations add InitialCreate --startup-project ../AuctionService.Api

# 2. 執行遷移，自動建立/更新資料庫結構
dotnet ef database update --startup-project ../AuctionService.Api
```

**優點**:
- ✅ 完全本地控制，無需網路連線
- ✅ 快速啟動與測試（Docker 容器秒級啟動）
- ✅ 開發者可自由建立/刪除資料庫進行測試
- ✅ 支援離線開發
- ✅ 無雲端資源成本

### 正式環境（Production）

**雲端資料庫服務**（選擇其一）:

#### 選項 1: Azure Database for PostgreSQL - Flexible Server
- **規格建議**: 
  - Compute: General Purpose, 2 vCores, 8GB RAM（初期）
  - Storage: 128GB, Auto-growth enabled
  - Backup: 7 天自動備份，異地備援
- **連線方式**: Private Endpoint（透過 VNet 連線，不對外公開）
- **高可用性**: Zone-redundant HA（可選，建議正式環境啟用）

#### 選項 2: AWS RDS for PostgreSQL
- **規格建議**: 
  - Instance: db.t4g.medium (2 vCPU, 4GB RAM)（初期）
  - Storage: 100GB gp3, Auto-scaling enabled
  - Backup: 7 天自動備份，Multi-AZ 部署（HA）
- **連線方式**: 置於 Private Subnet，透過 Security Group 限制存取

**連線字串配置**（透過環境變數注入）:
```bash
# Azure 範例
DB_CONNECTION_STRING="Host=auctionservice-prod.postgres.database.azure.com;Port=5432;Database=auctionservice_prod;Username=adminuser;Password=${PROD_DB_PASSWORD};SslMode=Require"

# AWS RDS 範例
DB_CONNECTION_STRING="Host=auctionservice-prod.abc123.us-east-1.rds.amazonaws.com;Port=5432;Database=auctionservice_prod;Username=adminuser;Password=${PROD_DB_PASSWORD};SslMode=Require"
```

**安全設定**:
- ✅ **強制 SSL/TLS 連線**（`SslMode=Require`）
- ✅ **密碼透過 Azure Key Vault / AWS Secrets Manager 管理**（絕不硬編碼）
- ✅ **IP 白名單 / Private Endpoint**（僅允許 API Server 存取）
- ✅ **定期自動備份**（7-30 天保留期）
- ✅ **啟用查詢效能監控**（Azure Query Performance Insight / AWS Performance Insights）

### 部署與資料庫遷移流程

**Code-First 遷移策略**:

1. **開發階段**:
   ```bash
   # 開發者在本地執行
   dotnet ef migrations add AddAuctionImageUrl --project src/AuctionService.Infrastructure --startup-project src/AuctionService.Api
   dotnet ef database update --project src/AuctionService.Infrastructure --startup-project src/AuctionService.Api  # 更新本地資料庫
   git add src/AuctionService.Infrastructure/Migrations/
   git commit -m "feat: add auction image url field"
   ```

2. **CI/CD Pipeline**（Azure DevOps / GitHub Actions）:
   ```yaml
   # 自動化部署流程
   - name: Build Docker Image
     run: docker build -t auctionservice:${{ github.sha }} .
   
   - name: Run Database Migrations
     run: |
       docker run --rm \
         -e DB_CONNECTION_STRING="${{ secrets.PROD_DB_CONNECTION }}" \
         auctionservice:${{ github.sha }} \
         dotnet ef database update --project src/AuctionService.Infrastructure \
                                    --startup-project src/AuctionService.Api
   
   - name: Deploy to Production
     run: kubectl apply -f k8s/deployment.yaml
   ```

3. **正式環境資料庫更新**:
   - ✅ 遷移檔案隨程式碼一起版控（Git）
   - ✅ 部署前自動執行 `dotnet ef database update`
   - ✅ 支援 Blue-Green Deployment（舊版 schema 相容新版程式碼）
   - ✅ 遷移失敗時自動回滾（交易式 migration）

**Zero-Downtime Migration 策略**:
- 新增欄位時設定 `nullable: true`，避免部署中斷
- 刪除欄位前先部署不使用該欄位的程式碼版本
- 使用 `[Obsolete]` 標記待移除的實體屬性

### 環境變數配置對照表

| 環境 | 資料庫來源 | SSL | 備份策略 | 連線池大小 | 效能監控 |
|------|----------|-----|---------|-----------|----------|
| **Local** | Docker/本機 | 不需要 | 手動（開發者自行備份） | 預設 | Console Logs |
| **Staging** | Azure/AWS（小規格） | **必須** | 7 天自動備份 | 20-50 | 啟用（測試） |
| **Production** | Azure/AWS（HA 部署） | **必須** | 30 天自動備份 + 異地備援 | 100+ | **全面監控** |

### 遷移注意事項

⚠️ **破壞性變更檢查清單**:
- [ ] 刪除資料表/欄位前確認無程式碼引用
- [ ] 修改欄位型別前評估資料轉換影響（例如 `string` → `int`）
- [ ] 新增 `NOT NULL` 欄位時必須提供預設值或遷移腳本填充資料
- [ ] 修改主鍵/外鍵前確認無級聯影響

✅ **安全遷移實踐**:
- 在 Staging 環境先執行遷移測試
- 保留遷移回滾腳本（`dotnet ef migrations remove`）
- 大規模資料變更使用批次處理（避免 Lock Table）
- 重要遷移前手動備份資料庫快照

## Bidding Service 整合策略

### 日誌記錄規格（FR-029）

所有對 Bidding Service 的 HTTP 呼叫必須記錄以下資訊：

**必要欄位**:
- `Timestamp` (DateTime UTC, ISO 8601): 請求發起時間
- `CorrelationId` (Guid): 追蹤 ID，與當前請求的 Correlation ID 一致
- `Endpoint` (string): 完整 API 端點，例如 `GET /api/bids/{auctionId}/current`
- `RequestDuration` (int, milliseconds): 請求執行時間（從發送到收到回應）
- `ResponseStatusCode` (int): HTTP 狀態碼（200, 404, 503 等）

**選填欄位**:
- `RequestPayload` (JSON string, truncated to 1000 chars): 請求內容（POST/PUT 時）
- `ResponsePayload` (JSON string, truncated to 1000 chars): 回應內容（成功時）
- `ErrorMessage` (string): 錯誤訊息（失敗時）
- `RetryCount` (int): Polly 重試次數（如有重試）

**記錄等級**:
- `Information`: 成功呼叫（StatusCode 2xx）
- `Warning`: 重試成功或客戶端錯誤（StatusCode 4xx）
- `Error`: 伺服器錯誤或重試失敗（StatusCode 5xx 或 Timeout）

**實作範例**（Serilog 結構化日誌）:
```csharp
_logger.LogInformation(
    "BiddingService call completed: {Endpoint} | Status: {StatusCode} | Duration: {Duration}ms | CorrelationId: {CorrelationId}",
    endpoint, statusCode, duration, correlationId);
```

### 容錯策略

- **Polly Retry Policy**: 3 次指數退避重試（1s, 2s, 4s）
- **Circuit Breaker**: 連續 5 次失敗後開啟，30 秒後半開
- **Timeout**: 單次呼叫 5 秒逾時
- **降級處理**: 若 Bidding Service 無法回應，商品詳細資訊中 `CurrentBid` 欄位回傳 `null`，前端顯示 "目前出價資訊暫時無法取得"

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### I. Code Quality First ✅
- **Status**: PASS
- **Verification**: Controller-based API 支援清晰的職責分離；EF Core Code First 提供明確的資料模型；POCO DTO 避免複雜的映射邏輯；Repository Pattern 將資料存取與業務邏輯分離

### II. Test-Driven Development ✅
- **Status**: PASS
- **Verification**: xUnit 作為測試框架；Testcontainers 提供真實 PostgreSQL 環境進行整合測試；FluentAssertions 提升測試可讀性；Moq 支援單元測試隔離；目標 80%+ 程式碼覆蓋率

### III. User Experience Consistency ✅
- **Status**: PASS
- **Verification**: ResponseCodes 資料表統一管理 API 回應格式；所有 API 包含標準 metadata 區塊（statusCode, statusName, message）；支援多語系（繁體中文/英文）；清晰的錯誤訊息與驗證回應

### IV. Performance Requirements ✅
- **Status**: PASS
- **Verification**: 商品清單查詢 <200ms；商品詳細資訊 <300ms；EndTime 索引優化狀態篩選；分頁限制每頁 20 筆；支援 100+ req/s；被動式狀態計算避免背景任務開銷

### V. Observability and Monitoring ✅
- **Status**: PASS
- **Verification**: 結構化日誌記錄所有 API 請求；記錄所有對 Bidding Service 的呼叫（請求時間、回應時間、狀態碼）；Correlation ID 追蹤跨服務請求；健康檢查端點；異常完整記錄

### Documentation Language ✅
- **Status**: PASS
- **Verification**: 所有規格文件、計畫文件使用繁體中文；程式碼與註解使用英文

**Overall Gate Result**: ✅ PASS - 可進入 Phase 0 研究階段

## Project Structure

### Documentation (this feature)

```text
specs/002-auction-service/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
│   └── openapi.yaml     # OpenAPI 3.0 規格
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code（repository root - 單一資料夾結構）

**專案組織結構**：所有 AuctionService 相關檔案集中在單一根目錄 `AuctionService/` 中，包含：
- 解決方案檔案（.sln）
- 所有專案（API、Core、Infrastructure、Shared、測試專案）
- Docker 相關檔案（Dockerfile、docker-compose.yml）
- 建置與部署文檔（README.md、建置腳本）
- 設定檔與環境變數範本

```text
AuctionService/                              # 服務根目錄（所有內容集中於此）
├── AuctionService.sln                       # Visual Studio 解決方案檔案
├── README.md                                # 專案說明文件
├── .gitignore                               # Git 忽略規則
├── .editorconfig                            # 編輯器設定
├── docker-compose.yml                       # Docker Compose 設定
├── docker-compose.override.yml              # 本地開發覆寫設定
├── Dockerfile                               # 多階段建置 Dockerfile
├── .dockerignore                            # Docker 忽略規則
├── global.json                              # .NET SDK 版本鎖定
│
├── src/                                     # 原始碼目錄
│   ├── AuctionService.Api/                  # API 層（HTTP 端點）
│   │   ├── Controllers/
│   │   │   ├── AuctionsController.cs        # 拍賣商品端點（清單/詳細/建立/更新/刪除）
│   │   │   ├── FollowsController.cs         # 商品追蹤端點（查詢/追蹤/取消追蹤）
│   │   │   └── CategoriesController.cs      # 分類端點（查詢分類清單）
│   │   ├── Middlewares/
│   │   │   ├── ExceptionHandlingMiddleware.cs   # 全域例外處理
│   │   │   ├── CorrelationIdMiddleware.cs       # Correlation ID 追蹤
│   │   │   └── RequestLoggingMiddleware.cs      # 請求日誌
│   │   ├── Filters/
│   │   │   └── ValidationFilter.cs              # 輸入驗證過濾器
│   │   ├── Program.cs                       # 應用程式進入點
│   │   ├── appsettings.json                 # 設定檔
│   │   ├── appsettings.Development.json     # 開發環境設定
│   │   └── AuctionService.Api.csproj        # 專案檔案（目標框架：net10.0）
│   │
│   ├── AuctionService.Core/                 # 核心業務邏輯層
│   │   ├── Entities/
│   │   │   ├── Auction.cs                       # 拍賣商品實體
│   │   │   ├── Category.cs                      # 商品分類實體
│   │   │   ├── Follow.cs                        # 商品追蹤實體
│   │   │   └── ResponseCode.cs                  # API 回應代碼實體
│   │   ├── DTOs/
│   │   │   ├── Requests/
│   │   │   │   ├── CreateAuctionRequest.cs      # 建立拍賣請求 DTO
│   │   │   │   ├── UpdateAuctionRequest.cs      # 更新拍賣請求 DTO
│   │   │   │   └── FollowAuctionRequest.cs      # 追蹤拍賣請求 DTO
│   │   │   └── Responses/
│   │   │       ├── AuctionListItemDto.cs        # 拍賣清單項目回應 DTO
│   │   │       ├── AuctionDetailDto.cs          # 拍賣詳細資訊回應 DTO
│   │   │       ├── CurrentBidDto.cs             # 當前競標價格回應 DTO
│   │   │       ├── FollowDto.cs                 # 追蹤資訊回應 DTO
│   │   │       └── CategoryDto.cs               # 分類資訊回應 DTO
│   │   ├── Interfaces/
│   │   │   ├── IRepository.cs                   # 泛型儲存庫介面
│   │   │   ├── IAuctionRepository.cs            # 拍賣儲存庫介面
│   │   │   ├── IFollowRepository.cs             # 追蹤儲存庫介面
│   │   │   ├── IAuctionService.cs               # 拍賣服務介面
│   │   │   ├── IFollowService.cs                # 追蹤服務介面
│   │   │   └── IBiddingServiceClient.cs         # 競標服務客戶端介面
│   │   ├── Services/
│   │   │   ├── AuctionService.cs                # 拍賣服務實作
│   │   │   ├── FollowService.cs                 # 追蹤服務實作
│   │   │   └── ResponseCodeService.cs           # 回應代碼服務實作
│   │   ├── Validators/
│   │   │   └── AuctionValidator.cs              # 拍賣驗證器
│   │   ├── Exceptions/
│   │   │   ├── AuctionNotFoundException.cs      # 拍賣未找到例外
│   │   │   ├── UnauthorizedException.cs         # 未授權例外
│   │   │   └── ValidationException.cs           # 驗證例外
│   │   └── AuctionService.Core.csproj
│   │
│   ├── AuctionService.Infrastructure/       # 基礎設施層（外部依賴）
│   │   ├── Data/
│   │   │   ├── AuctionDbContext.cs              # EF Core DbContext
│   │   │   └── Configurations/
│   │   │       ├── AuctionConfiguration.cs      # Auction 實體設定
│   │   │       ├── CategoryConfiguration.cs     # Category 實體設定
│   │   │       ├── FollowConfiguration.cs       # Follow 實體設定
│   │   │       └── ResponseCodeConfiguration.cs # ResponseCode 實體設定
│   │   ├── Repositories/
│   │   │   ├── GenericRepository.cs             # 泛型儲存庫實作
│   │   │   ├── AuctionRepository.cs             # 拍賣儲存庫實作
│   │   │   └── FollowRepository.cs              # 追蹤儲存庫實作
│   │   ├── HttpClients/
│   │   │   └── BiddingServiceClient.cs          # 競標服務 HTTP 客戶端
│   │   ├── Migrations/
│   │   │   └── 20251202000000_InitialCreate.cs  # 初始資料庫遷移
│   │   ├── Seed/
│   │   │   ├── CategorySeeder.cs                # 分類初始資料
│   │   │   └── ResponseCodeSeeder.cs            # 回應代碼初始資料
│   │   └── AuctionService.Infrastructure.csproj
│   │
│   └── AuctionService.Shared/               # 共用工具與擴充
│       ├── Constants/
│       │   └── ResponseCodes.cs                 # 回應代碼常數
│       ├── Extensions/
│       │   ├── ServiceCollectionExtensions.cs   # DI 擴充方法
│       │   └── AuctionExtensions.cs             # 拍賣擴充方法（狀態計算）
│       ├── Helpers/
│       │   └── StatusCalculator.cs              # 狀態計算器
│       └── AuctionService.Shared.csproj
│
├── tests/                                   # 測試專案目錄
│   ├── AuctionService.UnitTests/                # 單元測試
│   │   ├── Services/
│   │   │   ├── AuctionServiceTests.cs
│   │   │   ├── FollowServiceTests.cs
│   │   │   └── ResponseCodeServiceTests.cs
│   │   ├── Controllers/
│   │   │   ├── AuctionsControllerTests.cs
│   │   │   ├── FollowsControllerTests.cs
│   │   │   └── CategoriesControllerTests.cs
│   │   ├── Validators/
│   │   │   └── AuctionValidatorTests.cs
│   │   ├── Extensions/
│   │   │   └── AuctionExtensionsTests.cs
│   │   └── AuctionService.UnitTests.csproj
│   │
│   ├── AuctionService.IntegrationTests/         # 整合測試（Testcontainers）
│   │   ├── Controllers/
│   │   │   ├── AuctionsControllerIntegrationTests.cs
│   │   │   ├── FollowsControllerIntegrationTests.cs
│   │   │   └── CategoriesControllerIntegrationTests.cs
│   │   ├── Repositories/
│   │   │   ├── AuctionRepositoryTests.cs
│   │   │   └── FollowRepositoryTests.cs
│   │   ├── Infrastructure/
│   │   │   └── PostgreSqlTestContainer.cs
│   │   └── AuctionService.IntegrationTests.csproj
│   │
│   └── AuctionService.ContractTests/            # 契約測試
│       ├── BiddingServiceContractTests.cs
│       └── AuctionService.ContractTests.csproj
│
├── docs/                                    # 文件資料夾
│   ├── architecture.md                      # 架構說明
│   ├── api-guide.md                         # API 使用指南
│   └── deployment.md                        # 部署指南
│
├── scripts/                                 # 建置與部署腳本
│   ├── build.sh                             # Linux/macOS 建置腳本
│   ├── build.ps1                            # Windows 建置腳本
│   ├── init-db.sql                          # PostgreSQL 初始化腳本
│   └── run-tests.sh                         # 測試執行腳本
│
└── .github/                                 # GitHub 相關設定
    ├── workflows/                           # CI/CD 工作流程
    │   ├── build.yml
    │   └── test.yml
    └── prompts/                             # AI 提示詞
        └── speckit.plan.prompt.md
```

**結構決策**：AuctionService 採用 Clean Architecture 分層結構（4 個主要專案），符合依賴倒置原則：
- **Core**: 核心業務邏輯，無外部依賴（純 C# 類別，包含實體、DTO、介面、服務）
- **Infrastructure**: 外部依賴實作（EF Core、HttpClient、Repositories）
- **Api**: HTTP 端點與中介軟體，依賴 Core 服務介面
- **Shared**: 跨層共用工具（常數、擴充方法、輔助工具）

測試結構鏡像原始碼結構，整合測試使用 Testcontainers 確保與正式環境一致性。所有檔案集中在單一 `AuctionService/` 資料夾中，包含解決方案檔案、Docker 設定、README 等建置文檔。

## Complexity Tracking

> **無需追蹤** - 所有 Constitution Check 項目均通過，無違規需要說明。

---

## Phase 0: Research & Technology Selection ✅

**Status**: COMPLETED

**Deliverables**:
- ✅ [research.md](./research.md) - 完整技術選型研究與最佳實踐

**Key Decisions**:
1. 採用 Clean Architecture 搭配 Controller-based API
2. 使用 EF Core Code First + Fluent API 配置
3. PostgreSQL 搭配索引優化策略
4. POCO 手動映射 DTO (不使用 AutoMapper)
5. 微服務架構：本專案為 Auction Service，未來透過獨立的 API Gateway (YARP) 整合
6. xUnit + Testcontainers 測試策略
7. Serilog 結構化日誌
8. ResponseCode 資料表統一管理 API 回應
9. 被動式商品狀態計算
10. HttpClient + Polly 整合 Bidding Service

**Constitution Re-check**: ✅ PASS - 所有研究決策符合 Constitution 要求

---

## Phase 1: Design & Contracts ✅

**Status**: COMPLETED

**Deliverables**:
- ✅ [data-model.md](./data-model.md) - 完整資料模型設計 (4 個實體、關聯關係、驗證規則、索引策略)
- ✅ [contracts/openapi.yaml](./contracts/openapi.yaml) - OpenAPI 3.0 規格 (11 個 API 端點)
- ✅ [quickstart.md](./quickstart.md) - 快速開始指南
- ✅ [.github/copilot-instructions.md](./.github/copilot-instructions.md) - 更新 Agent 上下文

**Data Model Summary**:
- **Auction** (拍賣商品): 核心實體，包含商品資訊、時間管理、狀態計算
- **Category** (商品分類): 8 個預設分類，支援篩選與組織
- **Follow** (商品追蹤): 使用者追蹤關係，支援追蹤清單功能
- **ResponseCode** (API 回應代碼): 統一管理狀態代碼與多語系訊息

**API Contracts Summary**:
- **Auctions API**: 7 個端點 (查詢清單、查詢詳細、建立、更新、刪除、查詢競標價格、查詢使用者商品)
- **Follows API**: 3 個端點 (查詢追蹤清單、追蹤商品、取消追蹤)
- **Categories API**: 1 個端點 (查詢分類清單)

**Constitution Re-check**: ✅ PASS - 設計符合所有 Constitution 原則

---

## Phase 2: Implementation Tasks

**Status**: PENDING (使用 `/speckit.tasks` 命令生成)

下一步將生成詳細的實作任務清單 (tasks.md)，包含：
- 專案初始化與結構建立
- 實體與 DbContext 實作
- Repository 與 Service 實作
- Controller 與 Middleware 實作
- 測試撰寫 (TDD)
- Docker 容器化
- 文件完善

**命令**: `@workspace /speckit.tasks` (在完成 Phase 1 後執行)

---

## Phase 3: Load Testing & Performance Validation

**Status**: PENDING (在核心功能完成後執行)

### 負載測試策略

**測試環境要求**:
- **API Server**: 2 vCPU, 4GB RAM
- **Database**: PostgreSQL 16, 2 vCPU, 8GB RAM
- **Load Generator**: 獨立機器（避免資源競爭）
- **測試資料**: 10,000+ 商品, 1,000+ 使用者, 20 分類, 5,000+ 追蹤記錄

**效能驗收標準**:
- P50 回應時間 ≤ 100ms
- P95 回應時間 ≤ 200ms
- P99 回應時間 ≤ 500ms
- 吞吐量 ≥ 100 RPS (目標), ≥ 200 RPS (尖峰)
- 成功率 ≥ 99.5%

### P0 關鍵路徑測試（必須執行）

#### 情境 1: 商品列表查詢壓測 ✅
**目標**: 驗證首頁商品列表的負載能力  
**配置**: 100 並發使用者, 60 秒持續時間  
**請求組合**:
- `GET /api/auctions?pageSize=20` (70%)
- `GET /api/auctions?categoryId=1&pageSize=20` (20%)
- `GET /api/auctions?search=iPhone&pageSize=20` (10%)

**驗收**: P95 ≤ 200ms, RPS ≥ 100, 成功率 ≥ 99.5%  
**實作狀態**: ✅ 已實作 (`LoadTest/Program.cs`)

#### 情境 2: 單一商品詳情查詢 🔴
**目標**: 驗證商品詳情頁面效能  
**配置**: 200 並發使用者, 60 秒  
**請求**: `GET /api/auctions/{randomId}`  
**驗收**: P95 ≤ 150ms, RPS ≥ 150, 成功率 ≥ 99.9%  
**實作狀態**: 🔴 待實作

#### 情境 3: 即時出價查詢壓測（核心功能）🔴
**目標**: 模擬競標期間的高頻輪詢壓力  
**配置**: 500 並發使用者, 120 秒, 每 2 秒輪詢一次  
**請求**: `GET /api/auctions/{hotAuctionId}/current-bid`  
**驗收**: P95 ≤ 300ms（含外部服務），RPS ≥ 200, 成功率 ≥ 99%  
**特殊考量**: 
- 測試 BiddingService 不可用時的降級邏輯
- 驗證 Polly Circuit Breaker 機制
- 評估快取策略需求（考慮 5 秒 TTL）

**實作狀態**: 🔴 待實作

### P1 高頻操作測試

#### 情境 4: 商品追蹤功能壓測 🔴
**配置**: 100 並發使用者, 60 秒  
**請求組合**: `POST /api/follows` (60%), `GET /api/follows` (30%), `DELETE /api/follows/{id}` (10%) - 需認證  
**驗收**: P95 ≤ 300ms, 成功率 ≥ 99%, 無重複追蹤記錄, JWT 驗證效能正常

#### 情境 5: 熱門商品壓力測試 🔴
**目標**: 所有使用者查詢同一熱門商品  
**配置**: 1000 並發使用者, 60 秒  
**請求**: `GET /api/auctions/{sameHotId}` (70%), `GET /api/auctions/{sameHotId}/current-bid` (30%)  
**驗收**: P95 ≤ 250ms, RPS ≥ 300, 無 N+1 查詢問題

#### 情境 6: 使用者商品查詢 🔴
**配置**: 50 並發使用者, 60 秒  
**請求**: `GET /api/auctions/user/{randomUserId}?pageSize=20` - 需認證  
**驗收**: P95 ≤ 200ms, 成功率 ≥ 99.9%, 複合索引 (UserId, CreatedAt) 生效

### P2 混合業務流程測試

#### 情境 7: 真實流量模擬（90% 讀 + 10% 寫）🔴
**目標**: 模擬生產環境真實流量組合  
**配置**: 300 並發使用者, 300 秒（5 分鐘）  
**請求組合**:
- `GET /api/auctions` (40%)
- `GET /api/auctions/{id}` (25%)
- `GET /api/auctions/{id}/current-bid` (15%)
- `GET /api/follows` (5%) - 認證
- `POST /api/follows` (5%) - 認證
- `POST /api/auctions` (5%) - 認證
- `PUT /api/auctions/{id}` (3%) - 認證
- `DELETE /api/auctions/{id}` (2%) - 認證

**驗收**: P95 ≤ 300ms, RPS ≥ 150, 成功率 ≥ 99%, 資源使用穩定, 無記憶體洩漏

#### 情境 8: 商品建立壓測 🔴
**配置**: 50 並發使用者, 60 秒  
**請求**: `POST /api/auctions` - 需認證（隨機商品資料含 FluentValidation 驗證）  
**驗收**: P95 ≤ 400ms, 成功率 ≥ 99.5%, 無重複商品 ID, 外鍵約束正確

### P3 容錯與邊界測試

#### 情境 9: BiddingService 不可用情境 🔴
**目標**: 驗證外部服務斷線時的容錯能力  
**配置**: 100 並發使用者, 120 秒  
**模擬**: BiddingService 返回 500 或超時  
**驗收**: 降級回應正常（返回 StartingPrice），P95 ≤ 500ms（含 Polly 重試），Circuit Breaker 在 5 次失敗後打開

#### 情境 10: 大分頁查詢防護 🔴
**配置**: 50 並發使用者, 30 秒  
**請求**: `GET /api/auctions?pageNumber=10000&pageSize=100`  
**驗收**: 回應時間 ≤ 1000ms 或返回 400 Bad Request, 無全表掃描, 記憶體無異常飆升

#### 情境 11: 無效 GUID 攻擊 🔴
**配置**: 100 並發使用者, 30 秒  
**請求**: `GET /api/auctions/invalid-guid-format`  
**驗收**: 返回 400 Bad Request, P95 ≤ 50ms（快速失敗）, 無資料庫查詢執行

#### 情境 12: 並發更新衝突 🔴
**配置**: 20 並發使用者, 30 秒  
**請求**: 同時更新同一 auctionId (`PUT /api/auctions/{sameId}` - 認證)  
**驗收**: 只有一個請求成功, 其他返回 409 Conflict, 資料一致性保持  
**注意**: 可能需要添加並發控制機制（樂觀鎖）

### P4 資料庫與基礎設施測試

#### 情境 13: 資料庫連線池耗盡測試 🔴
**配置**: 150 並發使用者（超過連線池上限）, 60 秒  
**請求**: 複雜查詢（長時間持有連線）  
**驗收**: 連線池滿時返回 503, 連線正常釋放無洩漏, 恢復後服務正常

#### 情境 14: 複雜查詢效能測試 🔴
**配置**: 100 並發使用者, 60 秒  
**請求**: `GET /api/auctions?search=keyword&categoryId=1&minPrice=1000&maxPrice=50000&sortBy=EndTime&sortDirection=asc&pageSize=50`  
**驗收**: P95 ≤ 300ms, 所有查詢使用索引, 無全表掃描

#### 情境 15: JWT 認證效能測試 🔴
**配置**: 200 並發使用者, 60 秒  
**請求**: 需認證端點（隨機組合），每個請求帶不同 JWT  
**驗收**: 認證開銷 ≤ 10ms, P95 ≤ 250ms, 無記憶體洩漏

### 測試實作工具

**推薦測試框架**: NBomber (.NET 原生)  
**替代方案**: Apache JMeter, k6, Gatling

**監控工具**:
- Application Insights (Azure) / Prometheus + Grafana
- Seq (結構化日誌)
- dotnet-counters (效能計數器)

### 效能優化檢查清單

**資料庫優化**:
- [ ] 所有查詢條件欄位都有索引（EndTime, CategoryId, UserId, Status）
- [ ] 複合索引順序正確（UserId + CreatedAt）
- [ ] 使用 `AsNoTracking()` 於唯讀查詢
- [ ] 避免 N+1 查詢（使用 Include/ThenInclude）
- [ ] 分頁查詢使用 Keyset Pagination（大資料量時）

**應用層優化**:
- [ ] 實作回應快取（分類資料、熱門商品）
- [ ] 使用記憶體快取減少資料庫查詢
- [ ] 非同步處理所有 I/O 操作
- [ ] 連線池正確配置（MinPoolSize=5, MaxPoolSize=100）
- [ ] 適當的 HTTP Client 逾時設定

**API 設計優化**:
- [ ] 實作 Conditional Requests (ETag)
- [ ] 壓縮回應內容 (Gzip)
- [ ] 適當的 Rate Limiting
- [ ] 批次 API 支援（減少請求次數）

### 測試執行計畫

**階段 1: 基礎驗證（Week 1）**
- ✅ 情境 1: 商品列表查詢（已實作）
- 🔴 情境 2: 單一商品詳情
- 🔴 情境 3: 即時出價查詢（關鍵）

**階段 2: 高頻操作（Week 2）**
- 情境 4-6: 追蹤、熱門商品、使用者查詢

**階段 3: 混合流程（Week 3）**
- 情境 7-8: 真實流量模擬、商品建立

**階段 4: 容錯與優化（Week 4）**
- 情境 9-15: 容錯測試、基礎設施測試

**Deliverables**:
- 負載測試腳本 (`LoadTest/` 專案擴充)
- 效能測試報告（含指標與瓶頸分析）
- 優化建議清單
- CI/CD 整合設定

**Constitution Re-check**: ✅ PASS - 測試策略符合效能與可觀測性要求

---

## Summary & Next Steps

### ✅ 已完成

1. ✅ Technical Context 填寫
2. ✅ Constitution Check (初次檢查 & 重新檢查)
3. ✅ Phase 0: 技術研究與選型
4. ✅ Phase 1: 資料模型設計
5. ✅ Phase 1: API 契約定義
6. ✅ Phase 1: 快速開始指南
7. ✅ Phase 1: Agent 上下文更新

### 📋 下一步

執行 `/speckit.tasks` 命令生成實作任務清單，開始 TDD 開發流程。

### 📊 專案資訊

- **分支**: `002-auction-service`
- **規格檔案**: [spec.md](./spec.md)
- **實作計畫**: [plan.md](./plan.md) (本檔案)
- **研究報告**: [research.md](./research.md)
- **資料模型**: [data-model.md](./data-model.md)
- **API 契約**: [contracts/openapi.yaml](./contracts/openapi.yaml)
- **快速開始**: [quickstart.md](./quickstart.md)

---

**最後更新**: 2025-12-02  
**計畫版本**: 1.0.0  
**狀態**: Phase 1 完成，等待 Phase 2 任務生成
