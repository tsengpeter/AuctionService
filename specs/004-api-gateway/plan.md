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

### Source Code (repository root)

```text
src/ApiGateway/
├── ApiGateway.csproj
├── Program.cs                    # 應用程式進入點與 DI 設定
├── appsettings.json             # 設定檔 (路由、Redis、JWT)
├── appsettings.Development.json
│
├── Controllers/                  # 傳統 Controller (非 Minimal APIs)
│   ├── HealthController.cs      # 健康檢查端點
│   └── AggregationController.cs # 請求聚合端點
│
├── Middleware/                   # 自訂 Middleware
│   ├── ErrorHandlingMiddleware.cs      # 統一錯誤處理
│   ├── RequestLoggingMiddleware.cs     # 請求日誌記錄
│   ├── RateLimitingMiddleware.cs       # Rate Limiting
│   └── JwtEnrichmentMiddleware.cs      # 加入 X-User-Id 標頭
│
├── Services/                     # 業務邏輯服務
│   ├── IAggregationService.cs
│   ├── AggregationService.cs           # 請求聚合邏輯
│   ├── IServiceHealthChecker.cs
│   ├── ServiceHealthChecker.cs         # 後端服務健康檢查
│   ├── IRateLimitService.cs
│   ├── RateLimitService.cs             # Rate Limiting 邏輯 (Redis)
│   └── IServiceDiscovery.cs
│       └── StaticServiceDiscovery.cs   # 靜態服務發現 (appsettings.json)
│
├── Models/                       # POCO 模型
│   ├── ErrorResponse.cs                # 錯誤回應模型
│   ├── HealthCheckResponse.cs          # 健康檢查回應
│   ├── AggregatedAuctionResponse.cs    # 商品聚合回應
│   └── ServiceHealthStatus.cs          # 服務健康狀態
│
├── Configuration/                # 設定類別
│   ├── YarpRouteConfig.cs              # YARP 路由設定模型
│   ├── RateLimitConfig.cs              # Rate Limit 設定
│   └── BackendServiceConfig.cs         # 後端服務設定
│
└── Extensions/                   # 擴充方法
    ├── ServiceCollectionExtensions.cs  # DI 註冊擴充
    └── HttpContextExtensions.cs        # HttpContext 輔助方法

tests/ApiGateway.Tests/
├── Unit/                         # 單元測試
│   ├── Services/
│   │   ├── RateLimitServiceTests.cs
│   │   ├── AggregationServiceTests.cs
│   │   └── ServiceHealthCheckerTests.cs
│   └── Middleware/
│       ├── ErrorHandlingMiddlewareTests.cs
│       └── RateLimitingMiddlewareTests.cs
│
├── Integration/                  # 整合測試
│   ├── RoutingTests.cs                 # 路由規則測試
│   ├── JwtAuthenticationTests.cs       # JWT 驗證測試
│   ├── RateLimitingTests.cs            # Rate Limiting 整合測試
│   ├── AggregationTests.cs             # 請求聚合測試
│   └── HealthCheckTests.cs             # 健康檢查測試
│
└── Fixtures/                     # 測試基礎設施
    ├── ApiGatewayWebApplicationFactory.cs  # WebApplicationFactory
    ├── RedisTestContainer.cs                # Testcontainers Redis
    └── MockBackendServices.cs               # 模擬後端服務

docker/
├── Dockerfile                    # API Gateway 容器映像
└── docker-compose.yml           # 本地開發環境 (Gateway + Redis)
```

**Structure Decision**: 
選用 **單一專案結構** (Option 1)，因為 API Gateway 是獨立的後端微服務，不包含前端或移動應用。使用傳統 Controller-based 架構而非 Minimal APIs，提供更好的組織性與測試性。YARP 透過 appsettings.json 設定路由規則，自訂邏輯 (聚合、限流) 實作為 Middleware 與 Services。

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
