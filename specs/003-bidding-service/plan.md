# 實作計畫: 競標服務 (Bidding Service)

**Branch**: `003-bidding-service` | **Date**: 2025-12-03 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/003-bidding-service/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## 摘要

競標服務 (Bidding Service) 負責處理所有出價相關的邏輯與歷史記錄，確保競標過程的公平性、即時性與一致性。採用 Redis 作為主要寫入層搭配 PostgreSQL 持久化存儲的架構，使用 Lua Script 確保併發安全，背景 Worker 進行非同步批次同步。系統設計為無狀態服務，支援單一實例部署，通過 YARP API Gateway 作為統一入口。

**核心技術決策**:
- Redis Write-Behind Cache 架構實現 < 10ms 出價回應
- PostgreSQL 欄位層級加密保護敏感資料 (AES-256-GCM)
- Correlation ID 實現分散式請求追蹤
- 雪花 ID (Snowflake ID) 統一主鍵生成策略
- EF Core Code First 資料庫管理
- 明確的 Auction Service API 契約與降級策略

## 技術上下文

**Language/Version**: C# 13 / .NET 10 (ASP.NET Core 10 Web API)  
**Primary Dependencies**: 
- **Framework**: ASP.NET Core 10 Web API (Controller-based, 不使用 Minimal APIs)
- **ORM**: Entity Framework Core 10 (Code First)
- **Redis Client**: StackExchange.Redis 2.7+
- **Snowflake ID**: IdGen 或 Snowflake.Core (64-bit Long ID 生成)
- **Logging**: Serilog (結構化日誌，JSON 格式)
- **API Gateway**: YARP (統一入口點)
- **密碼學**: System.Security.Cryptography (AES-256-GCM)
- **Key Management**: Azure.Security.KeyVault.Secrets
- **測試**: xUnit, Moq, FluentAssertions, Testcontainers (PostgreSQL, Redis)
- **監控**: Prometheus.NET (metrics), OpenTelemetry (optional 未來升級)

**Storage**: 
- **Primary**: PostgreSQL 14+ (持久化存儲，加密敏感欄位)
- **Cache/Write Layer**: Redis 7+ (AOF 持久化，寫入層)
- **Key Vault**: Azure Key Vault (加密金鑰管理)

**Testing**: 
- **Unit Tests**: xUnit + Moq (覆蓋率 > 80%)
- **Integration Tests**: Testcontainers (PostgreSQL + Redis 容器)
- **Contract Tests**: 跨服務 API 契約驗證
- **Load Tests**: K6 或 NBomber (併發測試)

**Target Platform**: Linux container (Docker)  
**Project Type**: Backend REST API 微服務 (無前端)  

**Performance Goals**: 
- 出價 API: < 100ms (P95), Redis 寫入 < 10ms
- 最高出價查詢: < 50ms (P95)
- 歷史查詢: < 200ms (P95)
- 併發支援: 1000 次出價/秒 (單一商品)
- Redis 快取命中率: > 90%

**Constraints**: 
- 單一實例部署 (固定 Worker ID = 1, Datacenter ID = 1)
- Redis AOF 持久化 (appendfsync everysec)
- PostgreSQL 連線池: min 10, max 50
- Redis 連線池: min 10, max 100
- 跨服務呼叫超時: 100ms (重試 1 次，總超時 200ms)
- HTTPS 強制 (TLS 1.2+)
- 不使用 AutoMapper (使用 POCO 手動映射)

**Scale/Scope**: 
- 預期用戶: 10,000+ 並發買家
- 商品規模: 100,000+ 活躍拍賣
- 出價歷史: 數百萬筆記錄
- API 端點: ~15 個 (CRUD + 查詢 + 統計)
- 資料表: 1 個主表 (Bids) + Redis 資料結構

## 憲法檢查 (Constitution Check)

*GATE: 必須在 Phase 0 研究前通過。Phase 1 設計後重新檢查。*

### ✅ 原則 I: 程式碼品質優先
- **狀態**: 符合
- **證據**: 
  - 採用 Controller-based API (非 Minimal APIs)，清晰的職責分離
  - Repository Pattern + Service Layer 分離業務邏輯與基礎設施
  - 依賴注入 (DI) 管理所有依賴
  - 不使用 AutoMapper，手動映射確保明確性
  - SOLID 原則應用於所有層級

### ✅ 原則 II: 測試驅動開發 (TDD)
- **狀態**: 符合
- **證據**:
  - 單元測試覆蓋率目標 > 80%
  - xUnit + Moq + FluentAssertions 完整測試框架
  - Testcontainers 提供真實 PostgreSQL + Redis 環境
  - 併發測試驗證 Lua Script 原子性
  - 所有 API 端點整合測試
  - TDD 流程: 先寫測試 → 實作 → 重構

### ✅ 原則 III: 使用者體驗一致性
- **狀態**: 符合
- **證據**:
  - 統一的 REST API 回應格式
  - 明確的 HTTP 狀態碼使用 (201, 400, 401, 403, 404, 409, 503)
  - 錯誤訊息清晰可執行 (例: "出價金額必須大於當前最高出價 $100")
  - Correlation ID 追蹤提供完整請求鏈
  - OpenAPI/Swagger 文件自動生成

### ✅ 原則 IV: 效能要求
- **狀態**: 符合
- **證據**:
  - 明確效能目標: 出價 < 100ms (P95), 查詢 < 200ms (P95)
  - Redis Write-Behind Cache 優化寫入效能
  - PostgreSQL 索引策略: (auctionId, bidAt DESC), (bidderId, bidAt DESC)
  - 批次查詢 API 避免 N+1 問題
  - 分頁機制控制回應大小
  - 非同步背景 Worker 處理批次寫入
  - 效能測試納入 CI/CD

### ✅ 原則 V: 可觀測性與監控
- **狀態**: 符合
- **證據**:
  - Serilog 結構化日誌 (JSON 格式)
  - 所有日誌包含 correlationId 欄位
  - X-Correlation-ID Header 跨服務傳遞
  - Prometheus metrics: bid_requests_total, bid_latency_seconds, redis_fallback_active
  - Health Check 端點監控服務狀態
  - 所有跨服務呼叫記錄延遲
  - Redis 降級/恢復事件告警
  - 死信佇列監控與告警

### ✅ 文檔語言要求
- **狀態**: 符合
- **證據**:
  - spec.md, plan.md, tasks.md 全部使用繁體中文
  - 程式碼、註解、commit message 使用英文
  - API 文檔使用繁體中文描述

### 🟢 憲法檢查結果: **通過 (PASS)**

無違反憲法原則的設計決策。所有核心原則皆符合要求。

## 專案結構 (Project Structure)

### 文檔 (本功能特性)

```text
specs/003-bidding-service/
├── spec.md              # 功能規格 (已完成)
├── plan.md              # 本檔案 (/speckit.plan 輸出)
├── research.md          # Phase 0 輸出 (研究決策)
├── data-model.md        # Phase 1 輸出 (資料模型)
├── quickstart.md        # Phase 1 輸出 (快速開始指南)
├── contracts/           # Phase 1 輸出 (API 契約)
│   ├── openapi.yaml    # OpenAPI 3.0 規格
│   └── schemas/        # JSON Schema 定義
└── tasks.md             # Phase 2 輸出 (/speckit.tasks 指令 - 本指令不產生)
```

### 原始碼 (單一專案資料夾結構)

**專案配置**: 所有原始碼、測試、建置文檔、Docker 配置均位於單一 `BiddingService/` 資料夾中，採用自包含 (self-contained) 結構，便於獨立開發與部署。

```text
BiddingService/                              # 專案根目錄 (所有內容在此資料夾)
├── BiddingService.sln                       # Visual Studio 解決方案檔
├── README.md                                # 專案說明文檔
├── .gitignore                               # Git 忽略規則
├── .editorconfig                            # 程式碼風格配置
├── global.json                              # .NET SDK 版本鎖定 (10.0)
│
├── docker-compose.yml                       # 本地開發環境 (PostgreSQL + Redis)
├── docker-compose.override.yml              # 本地環境覆寫配置
├── Dockerfile                               # 生產環境多階段建置
├── .dockerignore                            # Docker 建置忽略規則
│
├── src/                                     # 原始碼目錄
│   ├── BiddingService.Api/                  # ASP.NET Core Web API 專案
│   │   ├── Controllers/
│   │   │   ├── BidsController.cs            # 出價端點 (POST/GET)
│   │   │   └── HealthController.cs          # 健康檢查端點
│   │   ├── Middlewares/
│   │   │   ├── ExceptionHandlingMiddleware.cs   # 全域錯誤處理
│   │   │   ├── CorrelationIdMiddleware.cs       # Correlation ID 追蹤
│   │   │   └── RequestLoggingMiddleware.cs      # 請求日誌記錄
│   │   ├── Filters/
│   │   │   └── ValidationFilter.cs              # 模型驗證過濾器
│   │   ├── Program.cs                       # 應用程式進入點
│   │   ├── appsettings.json                 # 基礎配置
│   │   ├── appsettings.Development.json     # 開發環境配置
│   │   └── BiddingService.Api.csproj        # 專案檔 (net10.0)
│   │
│   ├── BiddingService.Core/                 # 核心業務邏輯層 (不依賴基礎設施)
│   │   ├── Entities/
│   │   │   └── Bid.cs                       # 出價實體
│   │   ├── DTOs/
│   │   │   ├── Requests/
│   │   │   │   └── CreateBidRequest.cs      # 新增出價請求 DTO
│   │   │   └── Responses/
│   │   │       ├── BidResponse.cs           # 出價回應 DTO
│   │   │       ├── BidHistoryResponse.cs    # 出價歷史清單 DTO
│   │   │       └── HighestBidResponse.cs    # 最高出價 DTO
│   │   ├── ValueObjects/
│   │   │   └── BidAmount.cs                 # 出價金額值物件 (含驗證)
│   │   ├── Interfaces/
│   │   │   ├── IRepository.cs               # 通用儲存庫介面
│   │   │   ├── IBidRepository.cs            # 出價儲存庫介面
│   │   │   ├── IRedisRepository.cs          # Redis 快取介面
│   │   │   ├── IBiddingService.cs           # 出價服務介面
│   │   │   ├── IAuctionServiceClient.cs     # Auction Service HTTP 客戶端介面
│   │   │   ├── IEncryptionService.cs        # 加密服務介面
│   │   │   └── ISnowflakeIdGenerator.cs     # 雪花 ID 生成器介面
│   │   ├── Services/
│   │   │   └── BiddingService.cs            # 核心出價邏輯實作
│   │   ├── Validators/
│   │   │   └── BidValidator.cs              # 出價驗證規則
│   │   ├── Exceptions/
│   │   │   ├── BidTooLowException.cs        # 出價過低例外
│   │   │   ├── AuctionNotFoundException.cs  # 商品不存在例外
│   │   │   └── UnauthorizedException.cs     # 未授權例外
│   │   └── BiddingService.Core.csproj
│   │
│   ├── BiddingService.Infrastructure/       # 基礎設施層 (資料存取與外部服務)
│   │   ├── Data/
│   │   │   ├── BiddingDbContext.cs          # EF Core DbContext
│   │   │   └── Configurations/
│   │   │       └── BidConfiguration.cs      # Bid 實體配置 (含加密 Value Converter)
│   │   ├── Repositories/
│   │   │   ├── GenericRepository.cs         # 通用儲存庫基礎實作
│   │   │   ├── BidRepository.cs             # PostgreSQL 出價儲存庫
│   │   │   └── RedisRepository.cs           # Redis 快取儲存庫 (Lua Script)
│   │   ├── Redis/
│   │   │   ├── RedisConnection.cs           # Redis 連線管理
│   │   │   └── Scripts/
│   │   │       └── place-bid.lua            # 出價原子操作 Lua Script
│   │   ├── HttpClients/
│   │   │   └── AuctionServiceClient.cs      # Auction Service HTTP 客戶端 (Polly 重試)
│   │   ├── BackgroundServices/
│   │   │   ├── RedisSyncWorker.cs           # Redis → PostgreSQL 背景同步
│   │   │   └── RedisHealthCheckService.cs   # Redis 健康檢查服務
│   │   ├── Encryption/
│   │   │   ├── EncryptionService.cs         # AES-256-GCM 加密實作
│   │   │   └── EncryptionValueConverter.cs  # EF Core 加密 Value Converter
│   │   ├── IdGeneration/
│   │   │   └── SnowflakeIdGenerator.cs      # 雪花 ID 生成器 (IdGen)
│   │   ├── Migrations/
│   │   │   └── 20251204000000_InitialCreate.cs  # 初始資料庫遷移
│   │   └── BiddingService.Infrastructure.csproj
│   │
│   └── BiddingService.Shared/               # 共用元件庫 (常數/擴充/輔助工具)
│       ├── Constants/
│       │   └── ErrorCodes.cs                # 錯誤代碼常數
│       ├── Extensions/
│       │   ├── ServiceCollectionExtensions.cs   # DI 擴充方法
│       │   └── BidExtensions.cs             # 出價擴充方法 (POCO 映射)
│       ├── Helpers/
│       │   └── HashHelper.cs                # SHA-256 雜湊輔助 (BidderIdHash)
│       └── BiddingService.Shared.csproj
│
├── tests/                                   # 測試專案目錄
│   ├── BiddingService.UnitTests/            # 單元測試
│   │   ├── Services/
│   │   │   └── BiddingServiceTests.cs
│   │   ├── Repositories/
│   │   │   └── BidRepositoryTests.cs
│   │   ├── Controllers/
│   │   │   └── BidsControllerTests.cs
│   │   ├── Validators/
│   │   │   └── BidValidatorTests.cs
│   │   ├── Infrastructure/
│   │   │   ├── SnowflakeIdGeneratorTests.cs
│   │   │   └── EncryptionServiceTests.cs
│   │   └── BiddingService.UnitTests.csproj
│   │
│   ├── BiddingService.IntegrationTests/     # 整合測試 (Testcontainers)
│   │   ├── Controllers/
│   │   │   └── BidsControllerIntegrationTests.cs
│   │   ├── Repositories/
│   │   │   ├── BidRepositoryTests.cs
│   │   │   └── RedisRepositoryTests.cs
│   │   ├── Infrastructure/
│   │   │   ├── PostgreSqlTestContainer.cs   # Testcontainers PostgreSQL Fixture
│   │   │   └── RedisTestContainer.cs        # Testcontainers Redis Fixture
│   │   ├── BackgroundServices/
│   │   │   └── RedisSyncWorkerTests.cs
│   │   └── BiddingService.IntegrationTests.csproj
│   │
│   └── BiddingService.LoadTests/            # 負載測試 (NBomber/K6)
│       ├── ConcurrentBiddingTests.cs        # 併發出價測試
│       └── BiddingService.LoadTests.csproj
│
├── scripts/                                 # 輔助建置腳本
│   ├── build.sh                             # Linux/macOS 建置腳本
│   ├── build.ps1                            # Windows 建置腳本 (PowerShell)
│   ├── init-db.sql                          # PostgreSQL 初始化 SQL
│   ├── run-tests.sh                         # 測試執行腳本
│   └── deploy.sh                            # 部署腳本
│
├── docs/                                    # 專案文檔
│   ├── architecture.md                      # 架構設計說明
│   ├── api-guide.md                         # API 使用指南
│   └── deployment.md                        # 部署指南
│
└── .github/                                 # GitHub Actions CI/CD
    └── workflows/
        ├── build.yml                        # 建置工作流程
        ├── test.yml                         # 測試工作流程
        └── deploy.yml                       # 部署工作流程
```

**結構決策**: 
採用 **單一資料夾自包含結構**，所有專案相關檔案均位於 `BiddingService/` 目錄下，理由如下：
1. ✅ **自包含性**: 解決方案 (.sln)、Docker 配置、README、建置腳本等所有檔案集中管理
2. ✅ **獨立部署**: 整個資料夾可獨立 clone、建置、測試、部署，無外部依賴
3. ✅ **清晰分層**: 採用 Clean Architecture 分層 (Api/Core/Infrastructure/Shared)
4. ✅ **測試分離**: 單元測試、整合測試、負載測試各自獨立專案，使用 Testcontainers 確保真實環境
5. ✅ **標準慣例**: 符合 .NET 微服務專案標準結構，便於團隊協作與維護

## 複雜度追蹤 (Complexity Tracking)

> 本專案無違反憲法的複雜度設計

**評估結果**: 所有設計決策符合憲法原則，無需額外複雜度說明。

| 設計面向 | 決策 | 是否符合 |
|---------|------|---------|
| 專案數量 | 1 個主專案 + 3 個測試專案 | ✅ 合理分離 |
| 架構模式 | Repository + Service Layer | ✅ 標準實踐 |
| 資料存取 | EF Core + Redis | ✅ 適合讀寫分離場景 |
| 測試策略 | TDD + 80% 覆蓋率 | ✅ 符合憲法 II |
| 效能優化 | Redis Write-Behind Cache | ✅ 有明確效能目標 |
| 可觀測性 | Serilog + Prometheus | ✅ 符合憲法 V |
