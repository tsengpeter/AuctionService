# 研究報告：後端模組化單體骨架

**功能分支**: `001-backend-scaffold`  
**建立日期**: 2026-03-22  
**狀態**: 完成

---

## 研究摘要

本報告解決規格澄清中已確定但尚未對應具體實作細節的技術問題，以及 Phase 1 設計所需的最佳實踐依據。所有 NEEDS CLARIFICATION 項目均已解決。

---

## 1. MediatR 作為模組間事件匯流排

### 決策
使用 **MediatR 12.x** 的 `INotification` / `INotificationHandler<T>` 介面實作領域事件發布/訂閱。

### 理由
- `INotificationHandler<T>` 透過 DI 自動解析所有訂閱者，發布者不需知道訂閱者存在（符合 FR-006）
- `IPublisher.Publish(notification)` 預設同步呼叫所有 Handler，可透過 `PublishStrategy` 改為並行
- 使用 `IPipelineBehavior<TRequest, TResponse>` 注入 `ValidationBehavior` 統一處理 FluentValidation（符合 FR-007）
- 骨架階段採 In-Memory，不引入 Message Broker；介面設計保持中立，未來可替換

### 已考慮的替代方案
| 方案 | 拒絕原因 |
|------|---------|
| 自行實作 EventBus | 重複造輪子，MediatR 成熟且有完整 pipeline |
| MassTransit In-Memory | 依賴過重，骨架階段殺雞用牛刀 |
| .NET Channel + BackgroundService | 工程複雜度高，骨架不需要非同步佇列語意 |

### 實作模式
```csharp
// 領域事件介面 (AuctionService.Shared)
public interface IDomainEvent : INotification { }

// 事件定義 (Auction module)
public record AuctionWonEvent(Guid AuctionId, Guid WinnerId) : IDomainEvent;

// Handler (Ordering module)
public class AuctionWonHandler : INotificationHandler<AuctionWonEvent>
{
    public Task Handle(AuctionWonEvent notification, CancellationToken ct) { ... }
}

// ValidationBehavior (Shared)
public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
```

---

## 2. EF Core 多 DbContext 與 PostgreSQL Schema 隔離

### 決策
每個模組擁有獨立的 **DbContext**，並透過 `HasDefaultSchema()` 將資料表隔離至各自的 PostgreSQL schema。

### 理由
- Schema 隔離確保模組間資料不可直接存取（符合 FR-002, SC-005）
- 每個模組維護獨立的 Migration 歷史，互不干擾
- 使用 `Npgsql.EntityFrameworkCore.PostgreSQL`（EF Core 10 Provider）

### Schema 對應
| 模組 | Schema | DbContext 類別 |
|------|--------|---------------|
| Member | `member` | `MemberDbContext` |
| Auction | `auction` | `AuctionDbContext` |
| Bidding | `bidding` | `BiddingDbContext` |
| Ordering | `ordering` | `OrderingDbContext` |
| Notification | `notification` | `NotificationDbContext` |

### DbContext 設定模式
```csharp
// Member Module
public class MemberDbContext(DbContextOptions<MemberDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("member");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MemberDbContext).Assembly);
    }
}
```

### Migration 執行指令
```bash
# 開發環境（手動）
dotnet ef migrations add InitialCreate --project src/Modules/Member/Member.csproj --startup-project src/AuctionService.Api/AuctionService.Api.csproj

# 套用 migration
dotnet ef database update --project src/Modules/Member/Member.csproj --startup-project src/AuctionService.Api/AuctionService.Api.csproj
```

### 已考慮的替代方案
| 方案 | 拒絕原因 |
|------|---------|
| 單一 DbContext | 違反模組邊界，所有模組實體暴露給彼此 |
| 分離資料庫（各模組獨立 DB） | 骨架階段過度複雜，分散式事務問題 |
| Database per schema（自動切換） | EF Core Provider 不支援跨 DbContext transaction，無需此複雜度 |

---

## 3. ASP.NET Core 10 模組化單體專案結構

### 決策
採用 **3 個 csproj 類型**：API Host、Shared Library、Module csproj（×5），加上 2 個測試 csproj。

### 理由
- 每模組一個 csproj（非 3 層各一個 csproj），符合澄清決策
- `Domain/`, `Application/`, `Infrastructure/` 為同一 csproj 內的資料夾而非獨立專案
- 減少 csproj 數量（8 個 vs 15+ 個），降低維護負擔，同時保持邏輯邊界清晰

### csproj 清單
```
AuctionService.sln
├── src/AuctionService.Api/AuctionService.Api.csproj          # API Host
├── src/AuctionService.Shared/AuctionService.Shared.csproj    # Shared Library
├── src/Modules/Member/Member.csproj                          # Member 模組
├── src/Modules/Auction/Auction.csproj                        # Auction 模組
├── src/Modules/Bidding/Bidding.csproj                        # Bidding 模組
├── src/Modules/Ordering/Ordering.csproj                      # Ordering 模組
├── src/Modules/Notification/Notification.csproj              # Notification 模組
├── tests/AuctionService.UnitTests/AuctionService.UnitTests.csproj
└── tests/AuctionService.IntegrationTests/AuctionService.IntegrationTests.csproj
```

---

## 4. JWT HS256 自訂 401/403 回應格式

### 決策
使用 JwtBearer 的 **`OnChallenge`** 和 **`OnForbidden`** 事件攔截 HTTP 認證/授權失敗，回寫統一 `ApiResponse` 格式。

### 理由
- ASP.NET Core 預設 401/403 回傳空 Body，`OnChallenge` 事件提供最早的攔截點（符合 FR-005）
- 設定方式無需自定義 Middleware，使用框架原生機制
- `GlobalExceptionMiddleware` 作為最後防線處理未預期例外

### 實作模式
```csharp
.AddJwtBearer(options =>
{
    options.Events = new JwtBearerEvents
    {
        OnChallenge = async ctx =>
        {
            ctx.HandleResponse(); // 防止預設回應
            ctx.Response.StatusCode = 401;
            ctx.Response.ContentType = "application/json";
            var body = ApiResponse.Fail("Unauthorized", 401);
            await ctx.Response.WriteAsJsonAsync(body);
        },
        OnForbidden = async ctx =>
        {
            ctx.Response.StatusCode = 403;
            ctx.Response.ContentType = "application/json";
            var body = ApiResponse.Fail("Forbidden", 403);
            await ctx.Response.WriteAsJsonAsync(body);
        }
    };
})
```

---

## 5. FluentValidation 整合 ASP.NET Core MediatR Pipeline

### 決策
使用 **`ValidationBehavior<TRequest, TResponse>`** 作為 MediatR `IPipelineBehavior`，在 Command/Query Handler 執行前自動驗證，拋出自訂 `ValidationException`，再由 `GlobalExceptionMiddleware` 轉換為 422 回應。

### 理由
- 預設 FluentValidation ASP.NET Integration (`AddFluentValidationAutoValidation`) 僅對 Controller 動作有效，不適用於 MediatR 流程
- Pipeline Behavior 確保所有 Command/Query 皆受驗證，無需在 Handler 裡手動呼叫 validator
- `GlobalExceptionMiddleware` 集中處理 `ValidationException` → 422 `ApiResponse` 轉換

### NuGet 套件
- `FluentValidation` 12.x（核心，不含 ASP.NET 整合）
- `FluentValidation.DependencyInjectionExtensions` 12.x（DI 自動掃描 Validator）

### 已考慮的替代方案
| 方案 | 拒絕原因 |
|------|---------|
| FluentValidation.AspNetCore 自動整合 | 僅限 ModelState，不與 MediatR pipeline 整合 |
| DataAnnotations | 無法表達複雜業務規則，可測試性差 |

---

## 6. Testcontainers for xUnit：PostgreSQL 容器生命週期

### 決策
使用 **`Testcontainers.PostgreSql` 4.x** 配合 xUnit **`IAsyncLifetime`** 集合共享容器（`[Collection]` 組合），每個測試類別使用獨立 Schema 或在測試後回滾事務。

### 理由
- `PostgreSqlContainer` 自動下載 `postgres:latest` 映像並啟動，測試結束後清除（符合 FR-010, SC-003）
- 整合測試透過 `WebApplicationFactory<Program>` 建立 ASP.NET Core 測試伺服器
- 使用 `ICollectionFixture<IntegrationTestFixture>` 在同一測試 Assembly 中共享一個容器，加快測試速度

### 實作骨架
```csharp
public class IntegrationTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithDatabase("auctionservice_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    public string ConnectionString => _postgres.GetConnectionString();

    public async Task InitializeAsync() => await _postgres.StartAsync();
    public async Task DisposeAsync() => await _postgres.DisposeAsync();
}

[CollectionDefinition("Integration")]
public class IntegrationCollection : ICollectionFixture<IntegrationTestFixture> { }
```

---

## 7. NuGet 套件版本清單

> 適用 .NET 10 / ASP.NET Core 10（2025 年 11 月 GA）

| 套件 | 版本 | 用途 |
|------|------|------|
| `MediatR` | 12.4.1 | CQRS + 事件匯流排 |
| `FluentValidation` | 12.0.0 | 輸入驗證 |
| `FluentValidation.DependencyInjectionExtensions` | 12.0.0 | DI 自動掃描 Validator |
| `Microsoft.EntityFrameworkCore` | 10.0.0 | ORM |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.0 | PostgreSQL Provider |
| `Microsoft.EntityFrameworkCore.Design` | 10.0.0 | Migration CLI 支援 |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 10.0.0 | JWT 認證 |
| `Swashbuckle.AspNetCore` | 8.1.0 | Swagger UI + OpenAPI |
| `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` | 10.0.0 | EF Core 健康檢查 |
| `xunit` | 2.9.3 | 單元/整合測試框架 |
| `xunit.runner.visualstudio` | 3.0.1 | VS 測試探索 |
| `Testcontainers.PostgreSql` | 4.2.0 | 整合測試 PostgreSQL 容器 |
| `FluentAssertions` | 8.1.0 | 測試斷言 DSL |
| `NSubstitute` | 5.3.0 | Mock 框架 |
| `Microsoft.AspNetCore.Mvc.Testing` | 10.0.0 | WebApplicationFactory |
| `coverlet.collector` | 6.0.3 | 測試覆蓋率收集 |

---

## 8. Docker Compose 服務設計

### 決策
`docker-compose.yml` 包含兩個服務：**PostgreSQL 16** 和 **pgAdmin 4**。

### 服務設定
```yaml
services:
  postgres:
    image: postgres:16-alpine
    ports:
      - "5432:5432"
    environment:
      POSTGRES_DB: auctionservice
      POSTGRES_USER: ${POSTGRES_USER}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
    volumes:
      - postgres_data:/var/lib/postgresql/data

  pgadmin:
    image: dpage/pgadmin4:latest
    ports:
      - "5050:80"
    environment:
      PGADMIN_DEFAULT_EMAIL: ${PGADMIN_EMAIL}
      PGADMIN_DEFAULT_PASSWORD: ${PGADMIN_PASSWORD}
    depends_on:
      - postgres
```

---

## 9. GlobalExceptionMiddleware 設計

### 決策
單一 Middleware 集中處理所有未捕捉例外，轉換為統一 `ApiResponse` 回應格式。

### 例外對應規則
| 例外類型 | HTTP Status | 回應格式 |
|---------|------------|---------|
| `ValidationException` (FluentValidation) | 422 | `{ success: false, errors: [...], statusCode: 422 }` |
| `UnauthorizedAccessException` | 401 | `{ success: false, error: "Unauthorized", statusCode: 401 }` |
| `KeyNotFoundException` | 404 | `{ success: false, error: "Not Found", statusCode: 404 }` |
| 其他未知例外 | 500 | `{ success: false, error: "Internal Server Error", statusCode: 500 }` |

---

*所有 NEEDS CLARIFICATION 項目已解決。Phase 1 設計可開始執行。*
