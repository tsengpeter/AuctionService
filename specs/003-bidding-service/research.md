# 研究與技術決策 (Research & Technical Decisions)

**功能**: 競標服務 (Bidding Service)  
**日期**: 2025-12-03  
**狀態**: Phase 0 Complete

---

## 執行摘要 (Executive Summary)

本文檔記錄 Bidding Service 實作前的技術研究與決策過程。所有關鍵技術選擇皆基於效能目標、可維護性和團隊技術棧進行評估。

**關鍵決策**:
1. ✅ 採用 Redis Write-Behind Cache 架構達成 < 10ms 寫入目標
2. ✅ 使用 IdGen/Snowflake.Core 套件生成雪花 ID
3. ✅ EF Core Value Converter 實作 AES-256-GCM 欄位加密
4. ✅ StackExchange.Redis + Lua Script 確保併發安全
5. ✅ Serilog 結構化日誌 + Correlation ID Middleware
6. ✅ Testcontainers 提供真實整合測試環境

---

## R-001: 雪花 ID 生成套件選擇

### 決策 (Decision)
**選用**: **IdGen** (主要) 或 **Snowflake.Core** (備選)

### 理由 (Rationale)

**IdGen 優勢**:
- .NET 原生實作，效能優異 (單執行緒 1M+ IDs/秒)
- 支援自定義 Epoch、Worker ID、Datacenter ID
- NuGet 套件穩定，社群活躍
- 輕量級，無外部依賴
- 提供執行緒安全的 ID 生成器

**Snowflake.Core 優勢**:
- Twitter 官方演算法移植
- 更詳細的配置選項
- 支援分散式時鐘漂移偵測

**實作細節**:
```csharp
// Program.cs - DI 註冊
services.AddSingleton<IIdGenerator>(_ =>
{
    var epoch = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    var structure = new IdStructure(41, 5, 5, 12); // timestamp, datacenter, worker, sequence
    var options = new IdGeneratorOptions(structure, new IdGeneratorOptions.DefaultTimeSource(epoch));
    return new IdGenerator(1, options); // Worker ID = 1
});
```

**配置要求**:
- Worker ID: 1 (固定，單一實例)
- Datacenter ID: 1 (固定，單一資料中心)
- Epoch: 2024-01-01 (自定義起始時間)

### 考慮的替代方案 (Alternatives Considered)

| 方案 | 優點 | 缺點 | 為何拒絕 |
|-----|------|------|---------|
| **GUID/UUID** | 簡單、內建 | 16 bytes (vs 8 bytes)、無時間排序 | 空間效率差 50%，索引效能差 |
| **資料庫自增 ID** | 簡單、順序性 | 分散式不友善、寫入瓶頸 | 不適合 Redis + PostgreSQL 雙寫 |
| **自行實作 Snowflake** | 完全控制 | 需要測試、維護成本高 | 成熟套件已滿足需求 |
| **NanoID/ShortID** | 字串格式、URL 友善 | 需要額外解析、非數值 | 規格要求 64-bit Long |

---

## R-002: Redis Lua Script 併發控制實作

### 決策 (Decision)
**選用**: **StackExchange.Redis 2.7+ 的 ScriptEvaluate API**

### 理由 (Rationale)

Redis Lua Script 保證原子性執行，避免 WATCH/MULTI/EXEC 的競態條件。

**Lua Script 範例**:
```lua
-- place-bid.lua
local auction_key = KEYS[1]                     -- auction:{auctionId}:highest_bid
local bids_key = KEYS[2]                        -- auction:{auctionId}:bids
local pending_key = KEYS[3]                     -- pending_bids

local bid_id = ARGV[1]
local bidder_id = ARGV[2]
local amount = tonumber(ARGV[3])
local timestamp = ARGV[4]
local starting_price = tonumber(ARGV[5])
local ttl_seconds = tonumber(ARGV[6])

-- 取得當前最高出價
local current_highest = redis.call('HGET', auction_key, 'amount')

-- 檢查金額
local required_amount = starting_price
if current_highest then
    required_amount = tonumber(current_highest)
end

if amount <= required_amount then
    return {err = 'AMOUNT_TOO_LOW'}
end

-- 原子操作：更新 Sorted Set, Hash, Pending Set
redis.call('ZADD', bids_key, amount, bid_id .. ':' .. timestamp .. ':' .. bidder_id)
redis.call('HMSET', auction_key, 'bidId', bid_id, 'bidderId', bidder_id, 'amount', amount, 'bidAt', timestamp)
redis.call('SADD', pending_key, bid_id)
redis.call('EXPIRE', bids_key, ttl_seconds)
redis.call('EXPIRE', auction_key, ttl_seconds)

return {ok = 'SUCCESS'}
```

**C# 呼叫方式**:
```csharp
var script = LuaScript.Prepare(@"
    -- Lua script content here
");

var result = await _redisDb.ScriptEvaluateAsync(script,
    keys: new RedisKey[] { highestBidKey, bidsKey, pendingKey },
    values: new RedisValue[] { bidId, bidderId, amount, timestamp, startingPrice, ttlSeconds }
);
```

**效能優勢**:
- 單次網路往返完成所有操作
- Redis 伺服器端原子執行，無競態條件
- 避免 WATCH/MULTI/EXEC 的樂觀鎖重試開銷

### 考慮的替代方案 (Alternatives Considered)

| 方案 | 優點 | 缺點 | 為何拒絕 |
|-----|------|------|---------|
| **WATCH/MULTI/EXEC** | 無需 Lua | 樂觀鎖需重試、高併發效能差 | 1000 次/秒併發場景不適用 |
| **RedLock** | 分散式鎖 | 複雜度高、需要多個 Redis 節點 | 單一 Redis 實例足夠 |
| **應用層悲觀鎖** | 簡單 | 效能差、可能死鎖 | 無法達成 < 10ms 目標 |

---

## R-003: PostgreSQL 欄位加密實作策略

### 決策 (Decision)
**選用**: **EF Core Value Converter + AES-256-GCM**

### 理由 (Rationale)

EF Core Value Converter 在 ORM 層自動加解密，對業務邏輯透明。

**實作範例**:
```csharp
// EncryptionValueConverter.cs
public class EncryptionValueConverter : ValueConverter<string, string>
{
    public EncryptionValueConverter(IEncryptionService encryption)
        : base(
            plaintext => encryption.Encrypt(plaintext),
            ciphertext => encryption.Decrypt(ciphertext))
    {
    }
}

// BiddingDbContext.cs
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    var encryptionConverter = new EncryptionValueConverter(_encryptionService);
    
    modelBuilder.Entity<Bid>()
        .Property(b => b.Amount)
        .HasConversion(encryptionConverter)
        .HasColumnType("text"); // 儲存 Base64 編碼的密文
    
    modelBuilder.Entity<Bid>()
        .Property(b => b.BidderId)
        .HasConversion(encryptionConverter)
        .HasColumnType("text");
}

// EncryptionService.cs
public class EncryptionService : IEncryptionService
{
    private readonly byte[] _key; // 從 Azure Key Vault 取得
    
    public string Encrypt(string plaintext)
    {
        using var aes = new AesGcm(_key);
        var nonce = new byte[AesGcm.NonceByteSizes.MaxSize]; // 12 bytes
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize]; // 16 bytes
        
        RandomNumberGenerator.Fill(nonce);
        aes.Encrypt(nonce, Encoding.UTF8.GetBytes(plaintext), ciphertext, tag);
        
        // Format: nonce + tag + ciphertext (Base64)
        return Convert.ToBase64String(nonce.Concat(tag).Concat(ciphertext).ToArray());
    }
    
    public string Decrypt(string base64Ciphertext)
    {
        var data = Convert.FromBase64String(base64Ciphertext);
        var nonce = data[..12];
        var tag = data[12..28];
        var ciphertext = data[28..];
        
        using var aes = new AesGcm(_key);
        var plaintext = new byte[ciphertext.Length];
        aes.Decrypt(nonce, ciphertext, tag, plaintext);
        
        return Encoding.UTF8.GetString(plaintext);
    }
}
```

**金鑰管理 (Azure Key Vault)**:
```csharp
// Program.cs
var keyVaultUrl = configuration["KeyVault:Url"];
var credential = new DefaultAzureCredential();
var client = new SecretClient(new Uri(keyVaultUrl), credential);

var encryptionKeySecret = await client.GetSecretAsync("BiddingService-EncryptionKey");
var encryptionKey = Convert.FromBase64String(encryptionKeySecret.Value.Value);

services.AddSingleton<IEncryptionService>(new EncryptionService(encryptionKey));
```

**索引限制處理**:
- 加密欄位 (`amount`, `bidderId`) 無法建立索引
- 查詢主要依賴 `auctionId` 和 `bidAt` (未加密) 索引
- 影響有限，因為不會直接用 `amount` 或 `bidderId` 進行範圍查詢

### 考慮的替代方案 (Alternatives Considered)

| 方案 | 優點 | 缺點 | 為何拒絕 |
|-----|------|------|---------|
| **Transparent Data Encryption (TDE)** | 對應用透明、整個資料庫加密 | 需要 PostgreSQL 企業版或擴充套件 | 成本高、過度工程 |
| **pgcrypto 擴充套件** | 資料庫層級加密 | 需要改寫 SQL、無法用 EF Core | 破壞 ORM 抽象 |
| **應用層手動加解密** | 完全控制 | 容易遺漏、測試困難 | Value Converter 更安全 |
| **不加密** | 最簡單 | 不符合規格要求 | 規格明確要求加密 |

---

## R-004: Correlation ID 追蹤實作

### 決策 (Decision)
**選用**: **ASP.NET Core Middleware + Serilog Enricher**

### 理由 (Rationale)

Middleware 自動注入 Correlation ID，Serilog Enricher 自動記錄到所有日誌。

**實作範例**:
```csharp
// CorrelationIdMiddleware.cs
public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeaderName = "X-Correlation-ID";
    private readonly RequestDelegate _next;
    
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeaderName].FirstOrDefault()
                           ?? Guid.NewGuid().ToString();
        
        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers.Add(CorrelationIdHeaderName, correlationId);
        
        using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}

// Program.cs
app.UseMiddleware<CorrelationIdMiddleware>();

// Serilog 配置
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(new JsonFormatter())
    .CreateLogger();

// HttpClient 配置 (跨服務呼叫)
services.AddHttpClient<IAuctionServiceClient, AuctionServiceClient>()
    .AddHttpMessageHandler<CorrelationIdDelegatingHandler>();

// CorrelationIdDelegatingHandler.cs
public class CorrelationIdDelegatingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var correlationId = _httpContextAccessor.HttpContext?.Items["CorrelationId"] as string;
        if (!string.IsNullOrEmpty(correlationId))
        {
            request.Headers.Add("X-Correlation-ID", correlationId);
        }
        
        return await base.SendAsync(request, cancellationToken);
    }
}
```

**日誌範例**:
```json
{
  "timestamp": "2025-12-03T10:15:30.123Z",
  "level": "Information",
  "correlationId": "abc-123-def-456",
  "message": "Bid created successfully",
  "properties": {
    "bidId": 123456789,
    "auctionId": 987654321,
    "amount": 1500.00
  }
}
```

### 考慮的替代方案 (Alternatives Considered)

| 方案 | 優點 | 缺點 | 為何拒絕 |
|-----|------|------|---------|
| **OpenTelemetry** | 完整分散式追蹤、視覺化 | 複雜度高、需要 Jaeger/Zipkin | 規格選擇輕量級方案 |
| **Application Insights** | Azure 原生、自動追蹤 | 需要 Azure 訂閱、廠商綁定 | 保持彈性 |
| **手動傳遞 TraceId** | 簡單 | 容易遺漏、不一致 | Middleware 更可靠 |

---

## R-005: 背景 Worker 實作策略

### 決策 (Decision)
**選用**: **IHostedService + BackgroundService**

### 理由 (Rationale)

.NET 內建的背景服務框架，與 DI 容器完美整合。

**實作範例**:
```csharp
// RedisSyncWorker.cs
public class RedisSyncWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RedisSyncWorker> _logger;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var redisRepo = scope.ServiceProvider.GetRequiredService<IRedisRepository>();
                var bidRepo = scope.ServiceProvider.GetRequiredService<IBidRepository>();
                
                // 1. 從 pending_bids Set 取得待同步的 bidId (批次 1000 筆)
                var pendingBidIds = await redisRepo.GetPendingBidsAsync(1000);
                
                if (pendingBidIds.Any())
                {
                    // 2. 從 Redis 取得出價詳細資料
                    var bids = await redisRepo.GetBidsByIdsAsync(pendingBidIds);
                    
                    // 3. 批次寫入 PostgreSQL
                    await bidRepo.BulkInsertAsync(bids);
                    
                    // 4. 從 pending_bids Set 移除已同步的 bidId
                    await redisRepo.RemovePendingBidsAsync(pendingBidIds);
                    
                    _logger.LogInformation("Synced {Count} bids to PostgreSQL", pendingBidIds.Count);
                }
                
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken); // 每秒執行一次
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing bids from Redis to PostgreSQL");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); // 錯誤時延遲 5 秒
            }
        }
    }
}

// Program.cs
services.AddHostedService<RedisSyncWorker>();
services.AddHostedService<RedisHealthCheckService>();
```

**重試機制 (指數退避)**:
```csharp
public async Task<bool> TrySyncBidWithRetryAsync(long bidId)
{
    var delays = new[] { 1000, 2000, 4000 }; // 1秒、2秒、4秒
    
    for (int attempt = 0; attempt < delays.Length; attempt++)
    {
        try
        {
            var bid = await _redisRepo.GetBidByIdAsync(bidId);
            await _bidRepo.InsertAsync(bid);
            await _redisRepo.RemovePendingBidAsync(bidId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Retry {Attempt} failed for bidId {BidId}", attempt + 1, bidId);
            
            if (attempt < delays.Length - 1)
            {
                await Task.Delay(delays[attempt]);
            }
        }
    }
    
    // 3 次重試失敗，移入死信佇列
    await _redisRepo.AddToDeadLetterQueueAsync(bidId, "Max retries exceeded");
    _logger.LogError("BidId {BidId} moved to dead letter queue after 3 retries", bidId);
    return false;
}
```

### 考慮的替代方案 (Alternatives Considered)

| 方案 | 優點 | 缺點 | 為何拒絕 |
|-----|------|------|---------|
| **Hangfire** | 功能豐富、Dashboard UI | 額外依賴、資料庫儲存 job | 簡單場景過度工程 |
| **Quartz.NET** | 強大的排程功能 | 複雜度高、需要配置 | 不需要複雜排程 |
| **Azure Functions Timer Trigger** | Serverless、自動擴展 | 廠商綁定、冷啟動問題 | 保持平台中立 |
| **Timer + Task.Run** | 最簡單 | 不受 DI 管理、測試困難 | IHostedService 更標準 |

---

## R-006: 整合測試環境 (Testcontainers)

### 決策 (Decision)
**選用**: **Testcontainers for .NET**

### 理由 (Rationale)

提供真實的 PostgreSQL 和 Redis 容器，避免 In-Memory 模擬的不一致性。

**實作範例**:
```csharp
// PostgreSqlFixture.cs
public class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:14")
        .WithDatabase("biddingservice_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();
    
    public string ConnectionString => _container.GetConnectionString();
    
    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        
        // 執行 EF Core Migrations
        var options = new DbContextOptionsBuilder<BiddingDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        
        using var context = new BiddingDbContext(options);
        await context.Database.MigrateAsync();
    }
    
    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}

// RedisFixture.cs
public class RedisFixture : IAsyncLifetime
{
    private readonly RedisContainer _container = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();
    
    public string ConnectionString => _container.GetConnectionString();
    
    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }
    
    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}

// 使用範例
public class BidsControllerTests : IClassFixture<PostgreSqlFixture>, IClassFixture<RedisFixture>
{
    private readonly PostgreSqlFixture _dbFixture;
    private readonly RedisFixture _redisFixture;
    
    public BidsControllerTests(PostgreSqlFixture dbFixture, RedisFixture redisFixture)
    {
        _dbFixture = dbFixture;
        _redisFixture = redisFixture;
    }
    
    [Fact]
    public async Task CreateBid_WhenValid_Returns201()
    {
        // Arrange: 使用真實的 PostgreSQL 和 Redis
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // 替換連線字串
                    services.Configure<ConnectionStrings>(options =>
                    {
                        options.PostgreSQL = _dbFixture.ConnectionString;
                        options.Redis = _redisFixture.ConnectionString;
                    });
                });
            });
        
        var client = factory.CreateClient();
        
        // Act & Assert
        var response = await client.PostAsJsonAsync("/api/bids", new CreateBidRequest
        {
            AuctionId = 123,
            Amount = 1500.00m
        });
        
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
```

### 考慮的替代方案 (Alternatives Considered)

| 方案 | 優點 | 缺點 | 為何拒絕 |
|-----|------|------|---------|
| **In-Memory Database (EF Core)** | 快速、無需 Docker | 行為與 PostgreSQL 不一致 | 無法測試 PostgreSQL 特定功能 |
| **Moq + Repository 模擬** | 完全隔離、快速 | 無法測試真實資料庫互動 | 整合測試需要真實環境 |
| **Docker Compose** | 完全控制環境 | 需要手動管理、CI/CD 複雜 | Testcontainers 自動管理生命週期 |
| **共享測試資料庫** | 簡單 | 測試間互相影響、難以並行 | 每個測試需要獨立環境 |

---

## R-007: HTTP Client 設定 (Auction/Member Service)

### 決策 (Decision)
**選用**: **IHttpClientFactory + Polly (Resilience)**

### 理由 (Rationale)

IHttpClientFactory 管理 HttpClient 生命週期，Polly 提供重試、超時、熔斷機制。

**實作範例**:
```csharp
// Program.cs
services.AddHttpClient<IAuctionServiceClient, AuctionServiceClient>(client =>
{
    client.BaseAddress = new Uri(configuration["Services:AuctionService:Url"]);
    client.Timeout = TimeSpan.FromMilliseconds(200); // 總超時 200ms
})
.AddTransientHttpErrorPolicy(policy => 
    policy.WaitAndRetryAsync(1, _ => TimeSpan.FromMilliseconds(50))) // 重試 1 次，延遲 50ms
.AddHttpMessageHandler<CorrelationIdDelegatingHandler>();

services.AddHttpClient<IMemberServiceClient, MemberServiceClient>(client =>
{
    client.BaseAddress = new Uri(configuration["Services:MemberService:Url"]);
    client.Timeout = TimeSpan.FromMilliseconds(200);
})
.AddTransientHttpErrorPolicy(policy => 
    policy.WaitAndRetryAsync(1, _ => TimeSpan.FromMilliseconds(50)))
.AddHttpMessageHandler<CorrelationIdDelegatingHandler>();

// AuctionServiceClient.cs
public class AuctionServiceClient : IAuctionServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AuctionServiceClient> _logger;
    
    public async Task<AuctionBasicInfo> GetAuctionBasicAsync(long auctionId)
    {
        var cacheKey = $"auction:basic:{auctionId}";
        
        // 1. 先檢查快取
        if (_cache.TryGetValue<AuctionBasicInfo>(cacheKey, out var cached))
        {
            _logger.LogDebug("Cache hit for auction {AuctionId}", auctionId);
            return cached;
        }
        
        try
        {
            // 2. 呼叫 Auction Service API
            var response = await _httpClient.GetAsync($"/api/auctions/{auctionId}/basic");
            response.EnsureSuccessStatusCode();
            
            var auction = await response.Content.ReadFromJsonAsync<AuctionBasicInfo>();
            
            // 3. 快取 5 分鐘
            _cache.Set(cacheKey, auction, TimeSpan.FromMinutes(5));
            
            return auction;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to fetch auction {AuctionId} from Auction Service", auctionId);
            
            // 4. 降級：使用過期快取
            if (_cache.TryGetValue<AuctionBasicInfo>($"{cacheKey}:stale", out var stale))
            {
                _logger.LogWarning("Using stale cache for auction {AuctionId}", auctionId);
                return stale with { IsStale = true };
            }
            
            throw;
        }
    }
}
```

### 考慮的替代方案 (Alternatives Considered)

| 方案 | 優點 | 缺點 | 為何拒絕 |
|-----|------|------|---------|
| **RestSharp** | 功能豐富、簡單易用 | 額外依賴、不受 DI 管理 | IHttpClientFactory 更標準 |
| **Refit** | 強型別、自動生成 | 需要定義介面、學習曲線 | 簡單 HTTP 呼叫不需要 |
| **gRPC** | 效能優異、強型別 | 需要 proto 定義、複雜度高 | 規格要求 REST API |
| **直接 new HttpClient** | 最簡單 | Socket 耗盡問題、測試困難 | 反模式 |

---

## 未解決問題 (Outstanding Questions)

目前所有技術決策已完成，無未解決問題。

---

## 下一步 (Next Steps)

1. ✅ Phase 0 完成 - 所有技術研究與決策已記錄
2. 🔜 Phase 1: 生成資料模型 (data-model.md)
3. 🔜 Phase 1: 生成 API 契約 (contracts/openapi.yaml)
4. 🔜 Phase 1: 生成快速開始指南 (quickstart.md)
5. 🔜 Phase 2: 生成任務清單 (tasks.md) - 由 `/speckit.tasks` 指令執行

---

**版本**: 1.0  
**狀態**: Phase 0 Complete  
**作者**: AI Assistant  
**審核**: Pending
