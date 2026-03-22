# Architecture / 系統架構

## 架構概覽 / Overview

AuctionService 是基於 ASP.NET Core 10 的**模組化單體（Modular Monolith）**。所有模組編譯為單一可部署單元，但每個模組各自擁有獨立的命名空間、EF Core `DbContext` 以及 PostgreSQL Schema，嚴格維護模組邊界。

AuctionService is a **Modular Monolith** built on ASP.NET Core 10. All modules are compiled into a single deployable unit, but each module enforces strict boundaries: its own namespace, its own EF Core `DbContext`, and its own PostgreSQL schema.

```
┌─────────────────────────────────────────────────────────┐
│                   AuctionService.Api                    │
│   (HTTP host, JWT middleware, Swagger, Health check)    │
└──────┬──────────┬──────────┬──────────┬──────────┬──────┘
       │          │          │          │          │
  ┌────▼────┐ ┌───▼────┐ ┌──▼──────┐ ┌─▼───────┐ ┌▼──────────────┐
  │ Member  │ │Auction │ │ Bidding │ │Ordering │ │ Notification  │
  │ Module  │ │ Module │ │ Module  │ │ Module  │ │    Module     │
  └────┬────┘ └───┬────┘ └──┬──────┘ └─┬───────┘ └┬──────────────┘
       │          │          │          │           │
  ┌────▼──────────▼──────────▼──────────▼───────────▼────┐
  │              AuctionService.Shared                    │
  │   ApiResponse<T>, BaseEntity, IDomainEvent,           │
  │   ValidationBehavior, IModule (future)                │
  └───────────────────────────────────────────────────────┘
```

## 模組邊界 / Module Boundaries

每個模組遵循三層結構 / Each module follows a three-layer structure:

```
Modules/<Name>/
├── Domain/
│   ├── Entities/          # 聚合根與實體 / Aggregate roots + entities
│   └── Events/            # IDomainEvent 實作 / IDomainEvent implementations
├── Application/
│   ├── Commands/          # MediatR IRequest<T> + IRequestHandler<T>
│   ├── Queries/           # MediatR IRequest<T> + IRequestHandler<T>
│   └── DependencyInjection.cs   # AddXxxModule(IServiceCollection, IConfiguration)
└── Infrastructure/
    ├── Persistence/
    │   ├── XxxDbContext.cs         # EF Core DbContext（Schema 隔離）/ schema isolated
    │   ├── Configurations/         # IEntityTypeConfiguration<T>
    │   └── Migrations/             # EF Core 遷移 / EF Core migrations
    └── (future: external services, repositories)
```

**模組隔離規則 / Module isolation rules**:
- 任何模組不得讀寫其他模組的資料庫資料表。No module reads or writes another module's database tables.
- 不允許跨模組 EF Core Foreign Key，僅使用邏輯 `Guid` 引用。No cross-module EF Core foreign keys — only logical `Guid` references.
- 模組間通訊透過 MediatR 進程內 `INotification` 事件。Inter-module communication is via in-process MediatR `INotification` events.

## 資料庫 Schema 隔離 / Database Schema Isolation

每個模組各自擁有獨立的 PostgreSQL Schema / Each module owns a dedicated PostgreSQL schema:

| 模組 / Module | Schema         |
|-------------|----------------|
| Member      | `member`       |
| Auction     | `auction`      |
| Bidding     | `bidding`      |
| Ordering    | `ordering`     |
| Notification| `notification` |

EF Core `ModelBuilder` 透過 `HasDefaultSchema("xxx")` 設定每個 DbContext 的 Schema，Schema 之間沒有 Foreign Key 約束。

The EF Core `ModelBuilder` sets `HasDefaultSchema("xxx")` per DbContext. There are no cross-schema foreign key constraints.

## API 回應格式 / API Response Format

所有端點回傳統一的回應格式 / All endpoints return a uniform envelope:

```json
// 成功 / Success
{ "success": true, "data": { ... }, "statusCode": 200 }

// 驗證錯誤 / Validation error (HTTP 422)
{ "success": false, "errors": [{ "field": "email", "message": "..." }], "statusCode": 422 }

// 未授權 / Unauthorized (HTTP 401)
{ "success": false, "message": "Authentication required.", "statusCode": 401 }
```

`ApiResponse<T>` 定義於 `AuctionService.Shared`，所有端點共用。

`ApiResponse<T>` is defined in `AuctionService.Shared` and used by all endpoints.

## 身份驗證 / Authentication

JWT HS256 在 `AuctionService.Api` 全域設定。自訂 `OnChallenge` 和 `OnForbidden` 事件處理器覆寫預設的空 Body 401/403，改回傳標準 `ApiResponse` 格式。

JWT HS256 is configured globally in `AuctionService.Api`. Custom `OnChallenge` and `OnForbidden` handlers override the default empty-body 401/403 responses with the standard `ApiResponse` format.

```
Client → Bearer token → JwtBearer middleware → [Authorize] endpoint
                              │
                    過期/無效 expired/invalid → OnChallenge → ApiResponse 401
                    權限不足 insufficient role → OnForbidden → ApiResponse 403
```

## 進程內事件流程 / In-Process Event Flow

領域事件實作 `IDomainEvent : INotification`，由 MediatR 進程內分派 / Domain events implement `IDomainEvent : INotification`. MediatR dispatches them in-process:

```
Auction Module                    MediatR IMediator
─────────────────                ─────────────────────────────────
AuctionItem.EndAuction()
  → raises AuctionEndedEvent
  → IMediator.Publish(event)  ──► Ordering.HandleAuctionEnded
                                  Notification.HandleAuctionEnded
```

每個 Handler 彼此獨立——某個 Handler 失敗不影響其他 Handler。發布者的交易在事件分派前完成。

Each handler is independent — a failure in one handler does not affect another. The publisher's transaction completes before events are dispatched.

## 驗證管線 / Validation Pipeline

FluentValidation 驗證器以 MediatR `IPipelineBehavior<TRequest, TResponse>` 形式運行，在每個 Handler 執行前觸發 / FluentValidation validators run as an MediatR `IPipelineBehavior<TRequest, TResponse>`, executed before every handler:

```
HTTP Request → Controller → MediatR.Send(command)
                                │
                         ValidationBehavior
                                │
                    ┌── 有效 valid ──┴── 無效 invalid ──┐
                    │                                   │
              IRequestHandler                  throw ValidationException
                    │                                   │
              ApiResponse 200             GlobalExceptionMiddleware
                                                       │
                                               ApiResponse 422
```

## 容器化與部署 / Containerization & Deployment

應用程式透過 `Dockerfile` 打包為 Docker Image，採用**多階段建置**以最小化最終 image 大小。

The application is packaged as a Docker image via `Dockerfile`, using **multi-stage build** to minimize the final image size.

```
┌─────────────────────────────────────────────────────────┐
│  Stage 1: Build (mcr.microsoft.com/dotnet/sdk:10.0)     │
│   dotnet restore → dotnet publish -c Release            │
└────────────────────────┬────────────────────────────────┘
                         │ COPY /app/publish
┌────────────────────────▼────────────────────────────────┐
│  Stage 2: Runtime (mcr.microsoft.com/dotnet/aspnet:10.0)│
│   Non-root user (appuser) · EXPOSE 8080                 │
└─────────────────────────────────────────────────────────┘
```

**設計決策 / Design decisions**:
- 執行階段 image 不含 .NET SDK，僅含 ASP.NET Core Runtime（image 較小、攻擊面較小）。  
  Runtime image excludes the .NET SDK; only ASP.NET Core Runtime is included (smaller image, smaller attack surface).
- 所有敏感設定（`JWT_SECRET`、connection string）透過環境變數注入，不寫入 image。  
  All sensitive config (`JWT_SECRET`, connection string) is injected via environment variables, never baked into the image.
- 資料庫 Schema 遷移需在容器啟動**前**於外部執行（例如 CI/CD pipeline）。  
  Database schema migrations must be run **before** container startup (e.g., in CI/CD pipeline).

## 技術堆疊 / Technology Stack

| 用途 / Concern | 技術 / Technology |
|---------|-----------|
| 框架 / Framework | ASP.NET Core 10 |
| ORM | EF Core 10 + Npgsql |
| 資料庫 / Database | PostgreSQL 16 |
| CQRS / 事件 / Events | MediatR 12 |
| 驗證 / Validation | FluentValidation 12 |
| 身份驗證 / Auth | JWT HS256 (Microsoft.AspNetCore.Authentication.JwtBearer) |
| API 文件 / API Docs | Swashbuckle / OpenAPI 3.1 |
| 單元測試 / Unit Tests | xUnit 2.9 + NSubstitute 5 + FluentAssertions 8 |
| 整合測試 / Integration Tests | xUnit + Testcontainers.PostgreSql 4 |
| 開發基礎設施 / Dev Infrastructure | Docker Compose (PostgreSQL 16 + pgAdmin 4) |
