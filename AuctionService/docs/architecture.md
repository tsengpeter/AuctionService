# AuctionService 架構文件

## 概述

AuctionService 採用 Clean Architecture 設計模式，實現商品拍賣服務的後端 REST API。系統包含商品管理、使用者追蹤、分類查詢等核心功能，支援微服務架構下的水平擴展。

## 架構原則

### Clean Architecture 分層

```
┌─────────────────────────────────────┐
│           Presentation Layer        │  ← API Controllers, DTOs
│         (AuctionService.Api)        │
├─────────────────────────────────────┤
│          Application Layer          │  ← Services, Interfaces
│        (AuctionService.Core)        │
├─────────────────────────────────────┤
│        Infrastructure Layer         │  ← Repositories, External Services
│    (AuctionService.Infrastructure)  │
├─────────────────────────────────────┤
│          Domain Layer               │  ← Entities, Business Rules
│       (AuctionService.Shared)       │
└─────────────────────────────────────┘
```

### 依賴方向

- **外層依賴內層**: Infrastructure → Core → Shared
- **內層不依賴外層**: Core 不直接引用 Infrastructure
- **依賴反轉**: 透過介面隔離實作細節

## 專案結構

### AuctionService.Api (Presentation Layer)

**責任**: HTTP 請求處理、回應格式化、驗證中介軟體

**主要元件**:
- **Controllers/**: REST API 端點實作
  - `AuctionsController`: 商品 CRUD 操作
  - `CategoriesController`: 分類查詢
  - `FollowsController`: 追蹤管理
  - `HealthController`: 健康檢查
- **Middlewares/**: 請求處理管線
  - `ExceptionHandlingMiddleware`: 全域異常處理
  - `CorrelationIdMiddleware`: 請求追蹤
  - `RequestLoggingMiddleware`: 結構化日誌
- **Program.cs**: 應用程式啟動與配置

**技術**: ASP.NET Core Web API, YARP Reverse Proxy, Scalar OpenAPI

### AuctionService.Core (Application Layer)

**責任**: 業務邏輯實作、領域服務、資料傳輸物件

**主要元件**:
- **Services/**: 業務服務實作
  - `AuctionService`: 商品業務邏輯
  - `CategoryService`: 分類管理
  - `ResponseCodeService`: 回應代碼本地化
- **Interfaces/**: 契約定義
  - `IAuctionRepository`: 商品資料存取
  - `IBiddingServiceClient`: 出價服務整合
  - `IResponseCodeService`: 回應代碼服務
- **DTOs/**: 資料傳輸物件
  - `Requests/`: 請求資料結構
  - `Responses/`: 回應資料結構
  - `Common/`: 共用資料結構
- **Validators/**: 請求驗證規則
- **Exceptions/**: 自訂異常類型

**技術**: FluentValidation, 自訂服務介面

### AuctionService.Infrastructure (Infrastructure Layer)

**責任**: 外部依賴實作、資料存取、第三方服務整合

**主要元件**:
- **Data/**: Entity Framework Core 配置
  - `AuctionDbContext`: 資料庫上下文
  - `Configurations/`: 實體配置
- **Repositories/**: 資料存取實作
  - `AuctionRepository`: 商品資料操作
  - `GenericRepository`: 通用 CRUD 操作
- **Clients/**: 外部服務客戶端
  - `BiddingServiceClient`: 出價服務 HTTP 客戶端
- **Migrations/**: 資料庫結構遷移

**技術**: Entity Framework Core, PostgreSQL, HttpClient, Polly

### AuctionService.Shared (Domain Layer)

**責任**: 共用工具、擴充方法、跨層依賴

**主要元件**:
- **Extensions/**: 擴充方法
  - `AuctionExtensions`: 商品狀態計算
  - `MappingExtensions`: DTO 映射
- **Middleware/**: 共用中介軟體
- **Entities/**: 領域實體
  - `Auction`: 商品實體
  - `Category`: 分類實體
  - `Follow`: 追蹤實體
  - `ResponseCode`: 回應代碼實體

**技術**: POCO 實體, 擴充方法

## 資料流程

### 商品查詢流程

```
1. HTTP Request → AuctionsController.GetAuctions()
2. 參數驗證 → AuctionQueryParametersValidator
3. 業務邏輯 → AuctionService.GetAuctionsAsync()
4. 資料存取 → AuctionRepository.GetPagedAsync()
5. 外部服務 → BiddingServiceClient.GetCurrentBidAsync()
6. DTO 映射 → AuctionExtensions.ToListItemDto()
7. 回應封裝 → ApiResponse<PagedResult<AuctionListItemDto>>
8. HTTP Response ← JSON 序列化
```

### 商品建立流程

```
1. HTTP Request → AuctionsController.CreateAuction()
2. 請求驗證 → CreateAuctionRequestValidator
3. 業務邏輯 → AuctionService.CreateAuctionAsync()
4. 權限檢查 → 使用者身份驗證
5. 資料存取 → AuctionRepository.CreateAsync()
6. 回應代碼 → ResponseCodeService.GetLocalizedMessageAsync()
7. 回應封裝 → ApiResponse<AuctionDetailDto>
8. HTTP Response ← JSON 序列化
```

## 外部依賴

### 資料庫 (PostgreSQL)

- **連線**: Npgsql Entity Framework Core Provider
- **策略**: Code First with Migrations
- **索引**: EndTime, CategoryId, UserId+CreatedAt
- **查詢**: AsNoTracking for read-only operations

### 外部服務 (BiddingService)

- **通訊**: HTTP/JSON via HttpClient
- **彈性**: Polly Circuit Breaker + Retry policies
- **監控**: 結構化日誌記錄所有請求
- **合約**: OpenAPI 規範定義介面

### API Gateway (YARP)

- **路由**: 基於路徑的請求轉發
- **配置**: appsettings.json 動態配置
- **中介軟體**: 支援自訂請求處理
- **效能**: 原生 .NET 高效能代理

## 安全性考量

### 驗證與授權

- **JWT Bearer Token**: 無狀態驗證
- **角色基礎**: 賣家 vs 買家權限
- **資源擁有權**: 使用者只能操作自己的商品

### 資料保護

- **參數化查詢**: 防止 SQL 注入
- **輸入驗證**: FluentValidation 規則
- **錯誤處理**: 不洩漏內部實作細節

### 網路安全

- **CORS**: 設定允許的來源
- **HTTPS**: 生產環境強制使用
- **Rate Limiting**: 透過中介軟體實作

## 效能優化

### 資料庫查詢

- **索引策略**: EndTime 作為主要查詢索引
- **分頁**: 限制結果集大小
- **唯讀查詢**: 使用 AsNoTracking()

### 快取策略

- **應用層快取**: 分類資料記憶體快取
- **HTTP 快取**: 適當的 Cache-Control 標頭

### 非同步處理

- **全異步**: 所有 I/O 操作使用 async/await
- **平行處理**: 支援多個外部服務呼叫

## 監控與可觀測性

### 日誌記錄

- **結構化日誌**: Serilog JSON 格式
- **關聯 ID**: 請求追蹤識別符
- **效能監控**: 慢查詢警告 (>1000ms)

### 健康檢查

- **資料庫連線**: CanConnectAsync() 驗證
- **外部服務**: BiddingService 可用性檢查
- **端點**: `/api/health` 統一健康狀態

### 指標收集

- **回應時間**: 各端點效能監控
- **錯誤率**: 異常發生統計
- **資源使用**: 記憶體和 CPU 使用率

## 部署架構

### 開發環境

```
┌─────────────────┐    ┌─────────────────┐
│   AuctionService │    │   PostgreSQL    │
│   (localhost:5000)│    │  (localhost:5432)│
└─────────────────┘    └─────────────────┘
         │                       │
         └───────────────────────┘
              Docker Compose
```

### 生產環境

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   API Gateway   │    │ AuctionService  │    │   PostgreSQL    │
│     (YARP)      │────│   (Pod)         │────│    (RDS)        │
│                 │    │                 │    │                 │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │                       │
         │              ┌─────────────────┐              │
         │              │  BiddingService │              │
         │              │     (Pod)       │              │
         └─────────────▶│                 │◀─────────────┘
                        └─────────────────┘
```

## 擴展性考量

### 水平擴展

- **無狀態設計**: 所有實例獨立運作
- **外部狀態**: 資料庫和快取集中管理
- **負載平衡**: Kubernetes Service 或 API Gateway

### 垂直擴展

- **資料庫優化**: 讀寫分離架構
- **快取層**: Redis 分散式快取
- **非同步處理**: 訊息佇列解耦

### 功能擴展

- **模組化設計**: 新功能可獨立開發
- **介面契約**: 鬆耦合的服務整合
- **測試覆蓋**: 確保重構安全性