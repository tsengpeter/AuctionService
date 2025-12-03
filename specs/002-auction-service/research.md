# 技術研究：商品拍賣服務

**功能分支**: `002-auction-service`  
**建立日期**: 2025-12-02  
**階段**: Phase 0 - Research & Technology Selection

## 研究目標

針對商品拍賣服務的技術選型與架構設計進行研究，解決所有技術未知項目，並制定最佳實踐方案。

## 技術選型研究

### 1. ASP.NET Core 10 Web API 架構模式

**Decision**: 採用 Clean Architecture 搭配 Controller-based API（不使用 Minimal APIs）

**Rationale**:
- **清晰的職責分離**: Core (業務邏輯) → Infrastructure (資料存取) → API (表現層)
- **高可測試性**: 各層獨立，易於單元測試與整合測試
- **符合 SOLID 原則**: 依賴反轉、介面隔離、單一職責
- **傳統 Controller 優勢**: 
  - 成熟的過濾器 (Filters) 與中介軟體 (Middleware) 生態系
  - 清晰的路由與屬性標記
  - 更好的文件生成支援 (Swagger/OpenAPI)
  - 團隊熟悉度高
- **避免 Minimal APIs**: 雖然簡潔，但在複雜業務邏輯場景下可讀性與維護性較差

**Alternatives Considered**:
- **Vertical Slice Architecture**: 功能切片架構，每個功能包含所有層次。優點是高內聚，缺點是跨功能共用邏輯較難管理
- **Minimal APIs**: .NET 6+ 新特性，簡潔但不適合大型專案，團隊明確要求不使用

**Best Practices**:
- 使用 `IServiceCollection` 擴展方法組織 DI 註冊
- 實作 `IRepository<T>` 泛型介面統一資料存取
- 使用 `FluentValidation` 進行輸入驗證（或原生 DataAnnotations）
- 實作全域異常處理中介軟體
- 使用 `IOptions<T>` 管理組態設定

---

### 2. Entity Framework Core Code First 工作流程

**Decision**: 採用 Code First Migrations 搭配 Fluent API 配置

**Rationale**:
- **版本控制友好**: Migration 檔案記錄所有資料庫變更歷史
- **開發效率**: 直接從 C# 類別生成資料庫結構
- **型別安全**: 編譯時期檢查，避免 SQL 語法錯誤
- **Fluent API 優於 Attributes**: 
  - 配置與實體分離，保持 POCO 乾淨
  - 支援更複雜的配置（複合鍵、索引、關聯）
  - 集中管理於 `EntityTypeConfiguration<T>`

**Alternatives Considered**:
- **Database First**: 從現有資料庫生成模型，不適合新專案
- **Data Annotations**: 簡單但功能受限，配置與實體混合

**Best Practices**:
- 為每個實體建立獨立的 `IEntityTypeConfiguration<T>` 類別
- 在 `EndTime` 欄位建立索引（高頻查詢）
- 使用 `[Timestamp]` 或 `RowVersion` 實作樂觀並行控制
- Migration 命名規範: `YYYYMMDDHHMMSS_DescriptiveName`
- 使用 `HasQueryFilter` 實作軟刪除（如需要）
- 種子資料使用 `HasData` 方法（Categories, ResponseCodes）

**範例配置**:
```csharp
public class AuctionConfiguration : IEntityTypeConfiguration<Auction>
{
    public void Configure(EntityTypeBuilder<Auction> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Name).IsRequired().HasMaxLength(200);
        builder.Property(a => a.Description).IsRequired().HasMaxLength(2000);
        builder.HasIndex(a => a.EndTime); // 效能優化
        builder.HasOne(a => a.Category).WithMany().HasForeignKey(a => a.CategoryId);
    }
}
```

---

### 3. PostgreSQL 資料庫最佳化

**Decision**: 使用 Npgsql 提供者搭配連線池、索引優化、查詢效能監控

**Rationale**:
- **Npgsql**: 官方推薦的 PostgreSQL 提供者，效能優異
- **索引策略**: 
  - `EndTime`: 支援狀態篩選（進行中商品查詢）
  - `CategoryId`: 支援分類篩選
  - `UserId` (Auction): 支援查詢特定使用者的商品
  - `UserId` + `AuctionId` (Follow): 複合唯一索引避免重複追蹤
- **連線池**: 預設啟用，調整 `MaxPoolSize` 依據負載
- **查詢優化**: 
  - 使用 `AsNoTracking()` 於唯讀查詢
  - 使用 `Include()` 避免 N+1 問題
  - 使用 `Select()` 投影避免過度查詢

**Alternatives Considered**:
- **SQL Server**: 企業級但授權成本高
- **MySQL**: 開源但 JSON 支援與全文檢索較弱

**Best Practices**:
```csharp
// 連線字串配置
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Database=auction;Username=user;Password=pass;Pooling=true;MinPoolSize=5;MaxPoolSize=100"
}

// 查詢優化範例
var auctions = await _context.Auctions
    .AsNoTracking()
    .Include(a => a.Category)
    .Where(a => a.EndTime > DateTime.UtcNow)
    .OrderBy(a => a.EndTime)
    .Skip((pageNumber - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

---

### 4. POCO DTO 映射策略（不使用 AutoMapper）

**Decision**: 使用手動映射搭配擴展方法或靜態映射類別

**Rationale**:
- **效能**: 無反射開銷，編譯時期檢查
- **明確性**: 映射邏輯清晰可見，易於除錯
- **型別安全**: 重構時編譯器會提示錯誤
- **簡單性**: 無需學習 AutoMapper 配置，減少依賴

**Alternatives Considered**:
- **AutoMapper**: 功能強大但增加複雜度，團隊明確要求不使用
- **Mapster**: 效能優於 AutoMapper 但仍是額外依賴

**Best Practices**:
```csharp
// 方案 1: 擴展方法
public static class AuctionExtensions
{
    public static AuctionResponseDto ToResponseDto(this Auction auction)
    {
        return new AuctionResponseDto
        {
            Id = auction.Id,
            Name = auction.Name,
            Description = auction.Description,
            StartingPrice = auction.StartingPrice,
            CategoryId = auction.CategoryId,
            StartTime = auction.StartTime,
            EndTime = auction.EndTime,
            UserId = auction.UserId,
            CreatedAt = auction.CreatedAt,
            UpdatedAt = auction.UpdatedAt
        };
    }
}

// 方案 2: 靜態映射類別
public static class AuctionMapper
{
    public static AuctionResponseDto ToDto(Auction auction) { /* ... */ }
    public static Auction ToEntity(CreateAuctionRequestDto dto) { /* ... */ }
}

// 使用
var dto = auction.ToResponseDto();
```

---

### 5. YARP API Gateway 配置與路由

**Decision**: 使用 YARP (Yet Another Reverse Proxy) 作為 API Gateway 單一進入點

**Rationale**:
- **微軟官方**: .NET Foundation 專案，與 ASP.NET Core 深度整合
- **高效能**: 基於 Kestrel，支援 HTTP/2、HTTP/3
- **靈活配置**: 支援檔案配置與程式碼配置
- **中介軟體整合**: 可插入自訂中介軟體（日誌、驗證、限流）
- **動態更新**: 支援熱重載配置無需重啟

**Alternatives Considered**:
- **Ocelot**: 較舊的 .NET Gateway，功能較少
- **Kong/Nginx**: 非 .NET 生態，整合複雜

**Best Practices**:
```json
// appsettings.json
{
  "ReverseProxy": {
    "Routes": {
      "auction-route": {
        "ClusterId": "auction-cluster",
        "Match": {
          "Path": "/api/auctions/{**catch-all}"
        },
        "Transforms": [
          { "PathPattern": "/api/auctions/{**catch-all}" }
        ]
      },
      "bidding-route": {
        "ClusterId": "bidding-cluster",
        "Match": {
          "Path": "/api/bids/{**catch-all}"
        }
      }
    },
    "Clusters": {
      "auction-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://localhost:5001/"
          }
        }
      },
      "bidding-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://localhost:5002/"
          }
        }
      }
    }
  }
}
```

```csharp
// Program.cs
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

app.MapReverseProxy();
```

---

### 6. 測試策略與 Testcontainers

**Decision**: 
- 單元測試: xUnit + Moq + FluentAssertions
- 整合測試: Testcontainers (PostgreSQL) + WebApplicationFactory

**Rationale**:
- **xUnit**: .NET 生態主流，支援並行測試
- **Moq**: 成熟的 Mocking 框架，易於隔離依賴
- **FluentAssertions**: 提升測試可讀性
- **Testcontainers**: 
  - 真實 PostgreSQL 環境，避免 InMemory 資料庫差異
  - Docker 容器自動管理生命週期
  - 完整測試資料庫行為（索引、交易、並行）

**Alternatives Considered**:
- **InMemory Database**: 簡單但行為與真實資料庫有差異
- **共用測試資料庫**: 測試間可能互相影響

**Best Practices**:
```csharp
// 整合測試基礎類別
public class IntegrationTestBase : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .WithDatabase("auction_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    protected WebApplicationFactory<Program> Factory { get; private set; }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        
        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // 替換 DbContext 使用測試資料庫
                    services.RemoveAll<DbContextOptions<AuctionDbContext>>();
                    services.AddDbContext<AuctionDbContext>(options =>
                        options.UseNpgsql(_postgres.GetConnectionString()));
                });
            });
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await Factory.DisposeAsync();
    }
}

// 單元測試範例
public class AuctionServiceTests
{
    [Fact]
    public async Task CreateAuction_WithValidData_ReturnsAuction()
    {
        // Arrange
        var mockRepo = new Mock<IAuctionRepository>();
        var service = new AuctionService(mockRepo.Object);
        
        // Act & Assert
        var result = await service.CreateAuctionAsync(/* ... */);
        result.Should().NotBeNull();
        result.Name.Should().Be("Test Auction");
    }
}
```

---

### 7. 結構化日誌與監控

**Decision**: 使用 Serilog 搭配結構化日誌、Correlation ID 追蹤

**Rationale**:
- **Serilog**: .NET 最受歡迎的結構化日誌框架
- **結構化日誌**: 便於查詢與分析（JSON 格式）
- **Correlation ID**: 追蹤跨服務請求鏈
- **Sink 彈性**: 支援 Console、File、Elasticsearch、Seq 等

**Best Practices**:
```csharp
// Program.cs
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithCorrelationId()
    .WriteTo.Console(new JsonFormatter())
    .WriteTo.File("logs/auction-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// 使用範例
_logger.LogInformation(
    "Creating auction {AuctionName} for user {UserId}", 
    request.Name, 
    userId
);

// 記錄 Bidding Service 呼叫
_logger.LogInformation(
    "Calling Bidding Service for auction {AuctionId} - RequestTime: {RequestTime}",
    auctionId,
    DateTime.UtcNow
);
```

---

### 8. API 回應標準化與 ResponseCode 管理

**Decision**: 使用 ResponseCode 資料表統一管理狀態代碼，搭配標準 API 回應包裝器

**Rationale**:
- **一致性**: 所有 API 回應格式統一
- **多語系**: 支援繁體中文與英文訊息
- **可維護性**: 集中管理錯誤代碼，避免硬編碼
- **擴展性**: 新增錯誤類型無需修改程式碼

**Best Practices**:
```csharp
// ResponseCode 實體
public class ResponseCode
{
    public string Code { get; set; } // 主鍵，例如 "AUCTION_NOT_FOUND"
    public string Name { get; set; }
    public string MessageZhTw { get; set; }
    public string MessageEn { get; set; }
    public string Category { get; set; } // Success/ClientError/ServerError
    public string Severity { get; set; } // Info/Warning/Error
}

// 標準 API 回應包裝器
public class ApiResponse<T>
{
    public ResponseMetadata Metadata { get; set; }
    public T Data { get; set; }
}

public class ResponseMetadata
{
    public string StatusCode { get; set; }
    public string StatusName { get; set; }
    public string Message { get; set; }
    public DateTime Timestamp { get; set; }
}

// 使用範例
var responseCode = await _responseCodeService.GetAsync("AUCTION_CREATED");
return Ok(new ApiResponse<AuctionResponseDto>
{
    Metadata = new ResponseMetadata
    {
        StatusCode = responseCode.Code,
        StatusName = responseCode.Name,
        Message = language == "zh-TW" ? responseCode.MessageZhTw : responseCode.MessageEn,
        Timestamp = DateTime.UtcNow
    },
    Data = auctionDto
});
```

---

### 9. 商品狀態被動式計算策略

**Decision**: 不儲存 Status 欄位，透過查詢時根據 StartTime 與 EndTime 即時計算

**Rationale**:
- **100% 準確性**: 狀態永遠即時反映真實時間
- **無延遲**: 不需等待背景任務更新
- **簡化架構**: 避免複雜的排程機制
- **效能優異**: PostgreSQL 時間比較運算極快，索引支援
- **避免競態條件**: 無需處理大量商品同時結束的並行更新

**Implementation**:
```csharp
// 在服務層計算狀態
public string CalculateStatus(DateTime startTime, DateTime endTime)
{
    var now = DateTime.UtcNow;
    if (now < startTime) return "Pending";
    if (now >= startTime && now < endTime) return "Active";
    return "Ended";
}

// 或在資料庫層使用 SQL CASE
// SELECT *, 
//   CASE 
//     WHEN NOW() < StartTime THEN 'Pending'
//     WHEN NOW() >= StartTime AND NOW() < EndTime THEN 'Active'
//     ELSE 'Ended'
//   END AS Status
// FROM Auctions
```

**Alternatives Considered**:
- **Hangfire 背景任務**: 增加複雜度，可能有延遲
- **儲存 Status 欄位**: 需要更新機制，可能不同步

---

### 10. Bidding Service 整合與重試策略

**Decision**: 使用 HttpClient + Polly 實作重試、斷路器、超時策略

**Rationale**:
- **Polly**: .NET 彈性與暫時性錯誤處理標準庫
- **重試策略**: 處理網路暫時性錯誤
- **斷路器**: 避免對故障服務持續呼叫
- **超時控制**: 防止請求無限等待
- **日誌記錄**: 所有呼叫記錄請求/回應時間與狀態

**Best Practices**:
```csharp
// Program.cs 註冊
builder.Services.AddHttpClient<IBiddingServiceClient, BiddingServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["BiddingService:BaseUrl"]);
    client.Timeout = TimeSpan.FromSeconds(5);
})
.AddTransientHttpErrorPolicy(policy => 
    policy.WaitAndRetryAsync(3, retryAttempt => 
        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))))
.AddTransientHttpErrorPolicy(policy => 
    policy.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));

// BiddingServiceClient 實作
public class BiddingServiceClient : IBiddingServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BiddingServiceClient> _logger;

    public async Task<CurrentBidResponse> GetCurrentBidAsync(Guid auctionId)
    {
        var requestTime = DateTime.UtcNow;
        
        try
        {
            var response = await _httpClient.GetAsync($"/api/bids/{auctionId}/current");
            var responseTime = DateTime.UtcNow;
            
            _logger.LogInformation(
                "Bidding Service call - AuctionId: {AuctionId}, RequestTime: {RequestTime}, ResponseTime: {ResponseTime}, StatusCode: {StatusCode}",
                auctionId, requestTime, responseTime, response.StatusCode
            );
            
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<CurrentBidResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call Bidding Service for auction {AuctionId}", auctionId);
            throw;
        }
    }
}
```

---

## 效能優化策略

### 資料庫層優化
1. **索引策略**: EndTime (狀態篩選), CategoryId (分類篩選), UserId (使用者商品查詢)
2. **查詢優化**: AsNoTracking(), Include() 避免 N+1, Select() 投影
3. **連線池**: 調整 MinPoolSize/MaxPoolSize 依據負載

### 應用層優化
1. **分頁**: 每頁限制 20 筆，避免大量資料傳輸
2. **快取**: 考慮使用 IMemoryCache 快取 Categories 與 ResponseCodes
3. **非同步**: 所有 I/O 操作使用 async/await

### API Gateway 優化
1. **HTTP/2**: YARP 預設支援
2. **壓縮**: 啟用 Response Compression Middleware
3. **限流**: 使用 AspNetCoreRateLimit 防止濫用

---

## 安全性考量

### 輸入驗證
- 商品名稱: 3-200 字元
- 商品描述: 10-2000 字元
- 起標價: 正數，最大 10,000,000
- 結束時間: 至少當前時間 + 1 小時

### SQL 注入防護
- 使用 EF Core 參數化查詢（預設安全）
- 搜尋關鍵字使用 `.Contains()` 或 `.Like()`，避免字串拼接

### 授權
- 使用者只能編輯/刪除自己的商品
- 中介軟體驗證 JWT Token（假設由 Auth Service 提供）

---

## 部署與 DevOps

### Docker 容器化
```dockerfile
# Dockerfile (位於專案根目錄)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/AuctionService.Api/AuctionService.Api.csproj", "AuctionService.Api/"]
COPY ["src/AuctionService.Core/AuctionService.Core.csproj", "AuctionService.Core/"]
COPY ["src/AuctionService.Infrastructure/AuctionService.Infrastructure.csproj", "AuctionService.Infrastructure/"]
COPY ["src/AuctionService.Shared/AuctionService.Shared.csproj", "AuctionService.Shared/"]
RUN dotnet restore "AuctionService.Api/AuctionService.Api.csproj"
COPY src/ .
WORKDIR "/src/AuctionService.Api"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AuctionService.Api.dll"]
```

### Docker Compose (本地開發，位於專案根目錄)
```yaml
version: '3.8'
services:
  postgres:
    image: postgres:16
    environment:
      POSTGRES_DB: auction
      POSTGRES_USER: admin
      POSTGRES_PASSWORD: admin123
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

  auction-api:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "5001:80"
    depends_on:
      - postgres
    environment:
      ConnectionStrings__DefaultConnection: "Host=postgres;Database=auction;Username=admin;Password=admin123"

volumes:
  postgres_data:
```

---

## 總結

所有技術選型已完成研究，無 NEEDS CLARIFICATION 項目。採用 ASP.NET Core 10 + EF Core + PostgreSQL + YARP 的技術棧，搭配 Clean Architecture 架構模式，能夠滿足所有功能需求與效能目標。後續進入 Phase 1 進行資料模型設計與 API 契約定義。
