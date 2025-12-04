# Implementation Plan: API 閘道器 (API Gateway)

**Branch**: `004-api-gateway` | **Date**: 2025-12-04 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/004-api-gateway/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

建立 API Gateway 作為所有客戶端請求的統一入口點，使用 YARP (Yet Another Reverse Proxy) 處理請求路由、JWT 身份驗證、請求聚合、統一錯誤處理與限流防護。此 Gateway 將簡化客戶端與後端微服務 (Member Service、Auction Service、Bidding Service) 的互動，並提供集中的安全防護與監控。

## Technical Context

**Language/Version**: C# / .NET 10.0 (ASP.NET Core 10)  
**Primary Dependencies**: 
- YARP (Yarp.ReverseProxy) - 反向代理與路由
- Microsoft.AspNetCore.Authentication.JwtBearer - JWT 驗證
- StackExchange.Redis - Rate Limiting 計數器
- Serilog - 結構化日誌
- System.Text.Json - JSON 序列化

**Storage**: 
- Redis - Rate Limiting 計數器與分散式快取
- 無需持久化資料庫 (Gateway 本身不儲存業務資料)

**Testing**: 
- xUnit - 單元測試框架
- Moq - Mocking 框架
- FluentAssertions - 斷言函式庫
- WebApplicationFactory - 整合測試
- Testcontainers - Redis 容器化測試環境

**Target Platform**: Linux 容器 (Docker)，可部署至 Kubernetes  

**Project Type**: Web API (Backend only - 單一專案)

**Performance Goals**: 
- 路由延遲 < 10ms (P95)
- JWT 驗證延遲 < 20ms (P95)
- 請求聚合回應時間 < 300ms (P95)
- 系統可用性 > 99.9%

**Constraints**: 
- 不使用 Minimal APIs (使用傳統 Controller-based 架構)
- 不使用 AutoMapper (使用 POCO 直接對應)
- 後端服務呼叫超時 30 秒
- 請求大小限制 10MB
- Rate Limit: 100 requests/minute/IP

**Scale/Scope**: 
- 支援 3 個後端微服務路由
- 預期同時處理 1000+ 併發請求
- 支援水平擴展 (多實例部署)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### 原則 I: Code Quality First ✅
- **狀態**: 通過
- **檢查點**:
  - YARP 提供清晰的路由抽象，避免複雜的手寫路由邏輯
  - JWT 驗證透過 ASP.NET Core Authentication Middleware 處理，符合依賴注入原則
  - 業務邏輯 (請求聚合、錯誤處理) 與基礎設施 (YARP、Redis) 分離
  - Rate Limiting 邏輯封裝為獨立服務，可注入與測試
- **行動**: 無需額外行動

### 原則 II: Test-Driven Development (NON-NEGOTIABLE) ✅
- **狀態**: 通過
- **檢查點**:
  - 計畫包含單元測試覆蓋率 > 80% 要求 (來自 spec.md SC-006)
  - 整合測試覆蓋所有路由規則、JWT 驗證、Rate Limiting
  - 使用 Testcontainers 提供隔離的 Redis 測試環境
  - 測試分層: 單元測試 (服務邏輯) / 整合測試 (API 端點) / 契約測試 (後端服務模擬)
- **行動**: tasks.md 必須包含「先寫測試」的任務順序

### 原則 III: User Experience Consistency ✅
- **狀態**: 通過
- **檢查點**:
  - 統一錯誤格式 (FR-004): JSON 結構包含 code, message, details, timestamp, path
  - HTTP 狀態碼明確定義 (401 未授權, 429 限流, 503 服務不可用, 504 超時)
  - 驗證訊息清楚 ("Token expired", "Invalid token", "Malformed token")
  - Rate Limit 回應包含 Retry-After 標頭
- **行動**: 實作統一的錯誤處理 Middleware

### 原則 IV: Performance Requirements ✅
- **狀態**: 通過
- **檢查點**:
  - 明確的效能目標: 路由 < 10ms, JWT < 20ms, 聚合 < 300ms (P95)
  - 請求聚合使用 Task.WhenAll 並行呼叫，避免序列延遲
  - Redis 使用連線池避免連線開銷
  - 非同步處理所有 I/O 操作 (HttpClient, Redis)
- **行動**: tasks.md 包含效能測試任務 (使用 BenchmarkDotNet 或負載測試工具)

### 原則 V: Observability and Monitoring ✅
- **狀態**: 通過
- **檢查點**:
  - 結構化日誌使用 Serilog (JSON 格式)
  - 記錄所有請求: 路徑、方法、狀態碼、延遲、使用者 ID、IP (FR-012)
  - Correlation ID (X-Gateway-Request-Id) 追蹤請求
  - 健康檢查端點 /health 監控後端服務狀態
  - 記錄 Rate Limit 觸發與 Redis 降級事件
- **行動**: 實作健康檢查與日誌 Middleware

### 原則 VI: Documentation Language ✅
- **狀態**: 通過
- **檢查點**:
  - spec.md, plan.md, tasks.md 使用繁體中文撰寫
  - 程式碼、註解、commit messages 使用英文
  - API 文件 (contracts/) 使用繁體中文描述，JSON schema 使用英文
- **行動**: 確保所有文件遵守語言要求

### 整體評估
**狀態**: ✅ 全部通過，可進入 Phase 0 研究

無憲法違規需要記錄在 Complexity Tracking。

## Project Structure

### Documentation (this feature)

```text
specs/004-api-gateway/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
│   ├── routes.yaml      # YARP 路由設定
│   ├── aggregation-endpoints.yaml  # 聚合端點定義
│   └── error-responses.json        # 錯誤回應格式範例
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (single root directory)

**專案配置**: 所有原始碼、測試、建置文檔、Docker 配置均位於單一 `ApiGateway/` 資料夾中,採用自包含 (self-contained) 結構,便於獨立開發與部署。

```text
ApiGateway/                                  # 專案根目錄 (所有內容在此資料夾)
├── ApiGateway.sln                           # Visual Studio 解決方案檔
├── README.md                                # 專案說明文檔
├── .gitignore                               # Git 忽略規則
├── .editorconfig                            # 程式碼風格配置
├── global.json                              # .NET SDK 版本鎖定 (10.0)
│
├── docker-compose.yml                       # 本地開發環境 (Redis + Backend Mocks)
├── docker-compose.override.yml              # 本地環境覆寫配置
├── Dockerfile                               # 生產環境多階段建置
├── .dockerignore                            # Docker 建置忽略規則
│
├── src/                                     # 原始碼目錄
│   ├── ApiGateway.Api/                      # ASP.NET Core Web API 專案
│   │   ├── Controllers/
│   │   │   ├── AggregationController.cs     # 請求聚合端點
│   │   │   └── HealthController.cs          # 健康檢查端點
│   │   ├── Middlewares/
│   │   │   ├── JwtAuthMiddleware.cs         # JWT 驗證中介軟體
│   │   │   ├── RateLimitMiddleware.cs       # 速率限制中介軟體
│   │   │   ├── RequestLoggingMiddleware.cs  # 請求日誌記錄
│   │   │   ├── CorrelationIdMiddleware.cs   # Correlation ID 追蹤
│   │   │   └── ExceptionHandlingMiddleware.cs  # 全域錯誤處理
│   │   ├── Filters/
│   │   │   └── ValidationFilter.cs          # 模型驗證過濾器
│   │   ├── Program.cs                       # 應用程式進入點
│   │   ├── appsettings.json                 # 基礎配置 (YARP routes + JWT)
│   │   ├── appsettings.Development.json     # 開發環境配置
│   │   └── ApiGateway.Api.csproj            # 專案檔 (net10.0)
│   │
│   ├── ApiGateway.Core/                     # 核心業務邏輯層 (不依賴基礎設施)
│   │   ├── Services/
│   │   │   ├── RateLimitService.cs          # 速率限制邏輯 (Redis INCR)
│   │   │   ├── AggregationService.cs        # 並行請求聚合 (Task.WhenAll)
│   │   │   └── HealthCheckService.cs        # 後端健康監控
│   │   ├── Interfaces/
│   │   │   ├── IRateLimitService.cs         # 速率限制服務介面
│   │   │   ├── IAggregationService.cs       # 聚合服務介面
│   │   │   └── IHealthCheckService.cs       # 健康檢查服務介面
│   │   ├── DTOs/
│   │   │   ├── Requests/
│   │   │   │   └── AggregationRequest.cs    # 聚合請求 DTO
│   │   │   └── Responses/
│   │   │       ├── AggregatedAuctionResponse.cs  # 聚合拍賣回應 DTO
│   │   │       ├── ErrorResponse.cs         # 統一錯誤回應 (RFC 7807)
│   │   │       └── HealthCheckResponse.cs   # 健康檢查回應 DTO
│   │   ├── ValueObjects/
│   │   │   └── RateLimitKey.cs              # 速率限制鍵值物件 (ratelimit:{ip}:{minute})
│   │   ├── Exceptions/
│   │   │   ├── RateLimitExceededException.cs    # 速率超限例外
│   │   │   ├── UnauthorizedException.cs     # 未授權例外
│   │   │   └── ServiceUnavailableException.cs   # 服務不可用例外
│   │   └── ApiGateway.Core.csproj
│   │
│   ├── ApiGateway.Infrastructure/           # 基礎設施層 (Redis 與 HTTP 客戶端)
│   │   ├── Redis/
│   │   │   ├── RedisConnection.cs           # Redis 連線管理
│   │   │   └── RateLimitRepository.cs       # Redis INCR + EXPIRE 操作
│   │   ├── HttpClients/
│   │   │   ├── AuctionServiceClient.cs      # Auction Service HTTP 客戶端 (Polly 重試)
│   │   │   ├── BiddingServiceClient.cs      # Bidding Service HTTP 客戶端
│   │   │   └── UserServiceClient.cs         # User Service HTTP 客戶端
│   │   └── ApiGateway.Infrastructure.csproj
│   │
│   └── ApiGateway.Shared/                   # 共用元件庫 (常數/擴充/輔助工具)
│       ├── Constants/
│       │   └── ErrorCodes.cs                # 錯誤代碼常數 (UNAUTHORIZED, RATE_LIMIT_EXCEEDED...)
│       ├── Extensions/
│       │   ├── ServiceCollectionExtensions.cs   # DI 擴充方法
│       │   └── HttpContextExtensions.cs     # HttpContext 擴充 (GetClientIp)
│       └── ApiGateway.Shared.csproj
│
├── tests/                                   # 測試專案目錄
│   ├── ApiGateway.UnitTests/                # 單元測試
│   │   ├── Services/
│   │   │   ├── RateLimitServiceTests.cs
│   │   │   ├── AggregationServiceTests.cs
│   │   │   └── HealthCheckServiceTests.cs
│   │   ├── Middlewares/
│   │   │   ├── JwtAuthMiddlewareTests.cs
│   │   │   ├── RateLimitMiddlewareTests.cs
│   │   │   └── ExceptionHandlingMiddlewareTests.cs
│   │   ├── Controllers/
│   │   │   └── AggregationControllerTests.cs
│   │   └── ApiGateway.UnitTests.csproj
│   │
│   ├── ApiGateway.IntegrationTests/         # 整合測試 (Testcontainers)
│   │   ├── Controllers/
│   │   │   └── AggregationControllerIntegrationTests.cs
│   │   ├── Routing/
│   │   │   └── YarpRoutingTests.cs          # YARP 路由驗證測試
│   │   ├── Infrastructure/
│   │   │   └── RedisTestContainer.cs        # Testcontainers Redis Fixture
│   │   └── ApiGateway.IntegrationTests.csproj
│   │
│   └── ApiGateway.LoadTests/                # 負載測試 (NBomber/K6)
│       ├── GatewayLoadTests.cs              # 吞吐量與延遲測試
│       └── ApiGateway.LoadTests.csproj
│
├── scripts/                                 # 輔助建置腳本
│   ├── build.sh                             # Linux/macOS 建置腳本
│   ├── build.ps1                            # Windows 建置腳本 (PowerShell)
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
        ├── ci.yml                           # 持續整合流程 (build + test)
        └── cd.yml                           # 持續部署流程 (Docker push + K8s deploy)
```

**Structure Decision**: 
選用 **單一根目錄結構**，所有專案相關檔案（解決方案、原始碼、測試、Docker、文檔）都在同一個 `ApiGateway/` 資料夾中。這種結構便於：
1. **獨立部署**: 整個 API Gateway 可作為獨立微服務部署
2. **版本控制**: 單一資料夾便於追蹤變更
3. **CI/CD 整合**: 建置與部署腳本集中管理
4. **文檔完整性**: README.md 與技術文檔就近存放

使用傳統 Controller-based 架構而非 Minimal APIs，提供更好的組織性與測試性。YARP 透過 appsettings.json 設定路由規則，自訂邏輯 (聚合、限流) 實作為 Middleware 與 Services。

## Complexity Tracking

> **填寫說明**: 僅在 Constitution Check 發現需要辯護的違規時填寫

無憲法違規需要記錄。此專案架構遵循標準 ASP.NET Core Web API 模式，使用業界認可的 YARP 反向代理框架，未引入不必要的複雜性。

---

## Phase 0 & Phase 1 完成總結

### ✅ Phase 0: Research (已完成)

**輸出文件**: [research.md](./research.md)

**研究項目** (共 10 項):
1. YARP 反向代理架構與設定
2. JWT 驗證與授權機制 (HS256)
3. Redis Rate Limiting 實作模式 (INCR + TTL)
4. 請求聚合並行呼叫模式 (Task.WhenAll)
5. 統一錯誤處理 Middleware 設計
6. 結構化日誌與 APM 整合 (Serilog + Application Insights)
7. 健康檢查實作模式 (ASP.NET Core Health Checks)
8. 測試策略與工具選擇 (xUnit + Moq + Testcontainers)
9. 設定管理與環境變數 (appsettings.json + Azure Key Vault)
10. CORS 設定與安全標頭

**關鍵決策**:
- ✅ 使用 YARP 替代 Ocelot (效能更佳、官方支援)
- ✅ JWT 使用 HS256 對稱金鑰 (內部微服務適用)
- ✅ Redis Fixed Window Rate Limiting (符合規格要求)
- ✅ 請求聚合並行執行 (最小化延遲)
- ✅ Serilog 結構化日誌 (JSON 格式)

### ✅ Phase 1: Design & Contracts (已完成)

**輸出文件**:
1. [data-model.md](./data-model.md) - 資料模型定義
2. [contracts/routes.yaml](./contracts/routes.yaml) - YARP 路由設定
3. [contracts/aggregation-endpoints.yaml](./contracts/aggregation-endpoints.yaml) - 聚合端點規格
4. [contracts/error-responses.json](./contracts/error-responses.json) - 錯誤回應格式
5. [quickstart.md](./quickstart.md) - 快速入門指南

**核心實體** (6 個):
1. `Route` - 路由規則 (YARP 設定)
2. `ServiceHealthStatus` - 服務健康狀態 (記憶體快取)
3. `RateLimitCounter` - 限流計數器 (Redis)
4. `ErrorResponse` - 統一錯誤格式
5. `AggregatedAuctionResponse` - 商品聚合回應
6. `HealthCheckResponse` - 健康檢查回應

**API 契約**:
- **路由規則**: 6 條路由 (會員、商品、出價 + 3 條優先路由)
- **聚合端點**: 1 個 (商品詳細頁聚合)
- **錯誤格式**: 10 種錯誤代碼 (401/404/413/429/500/503/504)
- **健康檢查**: `/health` 端點 (監控 3 個後端服務)

**開發環境準備**:
- 專案結構定義完成
- NuGet 套件清單確認
- appsettings.json 範本提供
- Docker Compose 設定完成
- 測試環境設定 (Testcontainers)

### ✅ Agent Context 更新

GitHub Copilot 指令檔已更新，包含:
- 技術堆疊: C# / .NET 10.0 (ASP.NET Core 10)
- 專案類型: Web API (Backend only)
- 專案結構: src/ApiGateway, tests/ApiGateway.Tests

---

## 下一步: Phase 2 - Tasks Generation

**命令**: `/speckit.tasks`

**預期輸出**: `tasks.md` - 按優先級排序的實作任務清單

**任務分類**:
1. **P1 (高優先級)**: 路由、JWT 驗證、Rate Limiting、錯誤處理
2. **P2 (中優先級)**: 請求聚合、健康檢查、日誌記錄
3. **P3 (低優先級)**: 效能優化、監控整合、文件完善

**TDD 流程**:
- 每個任務遵循 Red-Green-Refactor 循環
- 先寫測試，後寫實作
- 重構確保程式碼品質

**預估工作量**:
- 核心功能 (P1): 5-7 個工作日
- 次要功能 (P2): 3-4 個工作日
- 優化與文件 (P3): 2-3 個工作日
- **總計**: 10-14 個工作日 (單人全職)

---

## 附錄: 關鍵文件索引

| 文件 | 用途 | 狀態 |
|------|------|------|
| [spec.md](./spec.md) | 功能規格 | ✅ 完成 |
| [plan.md](./plan.md) | 實作計畫 (本文件) | ✅ 完成 |
| [research.md](./research.md) | 技術研究 | ✅ 完成 |
| [data-model.md](./data-model.md) | 資料模型 | ✅ 完成 |
| [contracts/routes.yaml](./contracts/routes.yaml) | 路由設定 | ✅ 完成 |
| [contracts/aggregation-endpoints.yaml](./contracts/aggregation-endpoints.yaml) | 聚合端點 | ✅ 完成 |
| [contracts/error-responses.json](./contracts/error-responses.json) | 錯誤格式 | ✅ 完成 |
| [quickstart.md](./quickstart.md) | 快速入門 | ✅ 完成 |
| [tasks.md](./tasks.md) | 任務清單 | ⏳ 待產生 (Phase 2) |

---

**計畫版本**: 1.0  
**產生日期**: 2025-12-04  
**狀態**: ✅ Phase 0 & Phase 1 完成，準備進入 Phase 2  
**下一步**: 執行 `/speckit.tasks` 產生實作任務清單
