# 技術研究: API Gateway

**日期**: 2025-12-04  
**專案**: 004-api-gateway  
**目的**: 解決技術實作中的關鍵決策點，為設計階段提供明確指引

---

## 研究項目總覽

本文件記錄 API Gateway 實作的關鍵技術決策、最佳實踐研究與替代方案評估。所有研究結果將指導 Phase 1 的設計與契約定義。

---

## R-001: YARP 反向代理架構與設定

### 決策
使用 **YARP (Yet Another Reverse Proxy)** 作為反向代理核心框架，透過 `appsettings.json` 設定路由規則與後端服務叢集。

### 理由
1. **微軟官方支援**: YARP 是 .NET Foundation 專案，與 ASP.NET Core 深度整合，長期維護有保障
2. **高效能**: 基於 HttpClient 與 Kestrel，原生支援 HTTP/2、HTTP/3
3. **設定驅動**: 路由規則透過 JSON 設定，無需硬編碼，符合 12-factor app 原則
4. **可擴展性**: 提供 Middleware Pipeline 整合點，可插入自訂邏輯 (認證、限流、日誌)
5. **文件完整**: 官方文件涵蓋路由、負載平衡、健康檢查、轉換等場景

### 評估的替代方案

| 方案 | 優點 | 缺點 | 拒絕原因 |
|------|------|------|----------|
| **Ocelot** | 功能豐富，社群成熟 | 維護緩慢，最後更新 2023 年 | 長期維護風險，YARP 效能更佳 |
| **手寫反向代理** | 完全控制 | 開發成本高，需處理 HTTP 協定細節 | 違反 YAGNI 原則，重複造輪 |
| **Nginx/Envoy** | 成熟穩定 | 需額外運維，難以整合 .NET 邏輯 | 增加架構複雜度，不適合單一團隊 |

### 實作重點
1. **路由設定結構** (`appsettings.json`):
   ```json
   {
     "ReverseProxy": {
       "Routes": {
         "member-route": {
           "ClusterId": "member-cluster",
           "Match": {
             "Path": "/api/members/{**catch-all}"
           }
         }
       },
       "Clusters": {
         "member-cluster": {
           "Destinations": {
             "destination1": {
               "Address": "http://member-service:5001"
             }
           }
         }
       }
     }
   }
   ```

2. **優先級路由**: 使用 `Order` 屬性確保 `/api/me/bids` 優先於 `/api/me/{**catch-all}` 匹配

3. **健康檢查整合**: 啟用 YARP 內建的被動/主動健康檢查
   ```json
   "HealthCheck": {
     "Active": {
       "Enabled": true,
       "Interval": "00:00:30",
       "Timeout": "00:00:10",
       "Path": "/health"
     }
   }
   ```

### 參考資料
- [YARP 官方文件](https://microsoft.github.io/reverse-proxy/)
- [Performance comparison: YARP vs Envoy](https://devblogs.microsoft.com/dotnet/introducing-yarp/)

---

## R-002: JWT 驗證與授權機制

### 決策
使用 **ASP.NET Core Authentication Middleware** 搭配 `Microsoft.AspNetCore.Authentication.JwtBearer`，實作 HS256 對稱金鑰驗證。

### 理由
1. **框架原生支援**: 無需第三方函式庫，與 Authorization Pipeline 完美整合
2. **效能優異**: Middleware 在 YARP 路由前執行，避免轉發無效請求
3. **宣告式授權**: 支援 `[Authorize]` 屬性與 Policy-based authorization
4. **HS256 適用場景**: 微服務內部通訊，共享密鑰可透過 Kubernetes Secrets 安全分發

### 評估的替代方案

| 方案 | 優點 | 缺點 | 拒絕原因 |
|------|------|------|----------|
| **RS256 非對稱金鑰** | 更高安全性，私鑰不需共享 | 驗證效能較慢 (~3x) | 內部微服務間通訊，HS256 已足夠 |
| **手寫 JWT 驗證** | 完全控制 | 需處理過期、簽章驗證、錯誤處理 | 重複造輪，且易出錯 |
| **OAuth2/OIDC** | 完整的授權框架 | 過度設計，需額外認證伺服器 | 當前需求僅需 JWT 驗證 |

### 實作重點
1. **Middleware 註冊順序** (關鍵):
   ```csharp
   app.UseAuthentication();  // 必須在 UseAuthorization 之前
   app.UseAuthorization();
   app.MapReverseProxy();    // YARP 路由最後
   ```

2. **公開端點設定**:
   - 使用 `[AllowAnonymous]` 標記公開端點
   - 或在 YARP Transform 中條件性跳過認證檢查

3. **JWT 參數設定**:
   ```csharp
   .AddJwtBearer(options =>
   {
       options.TokenValidationParameters = new TokenValidationParameters
       {
           ValidateIssuer = true,
           ValidateAudience = true,
           ValidateLifetime = true,
           ValidateIssuerSigningKey = true,
           ClockSkew = TimeSpan.Zero  // 禁用預設 5 分鐘容錯
       };
   });
   ```

4. **X-User-Id 標頭注入**:
   - 實作自訂 Middleware 提取 `ClaimsPrincipal.FindFirst("sub")` 並加入 Request Headers

### 安全考量
- JWT 密鑰透過 **環境變數** 或 **Azure Key Vault** 取得，禁止寫入 `appsettings.json`
- 定期輪換密鑰 (建議每 90 天)，使用雙金鑰過渡期 (rolling update)

### 參考資料
- [ASP.NET Core JWT Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/)
- [JWT Best Practices RFC 8725](https://datatracker.ietf.org/doc/html/rfc8725)

---

## R-003: Redis Rate Limiting 實作模式

### 決策
使用 **Redis INCR + TTL** 模式實作 Sliding Window Rate Limiting，透過 `StackExchange.Redis` 連線。

### 理由
1. **多實例支援**: Redis 集中管理計數器，避免各 Gateway 實例計數不一致
2. **原子操作**: `INCR` 指令保證原子性，避免競態條件 (race condition)
3. **自動過期**: `EXPIRE` 設定 TTL 自動清理過期計數器，無需手動維護
4. **高效能**: Redis 記憶體操作，延遲 < 1ms

### 評估的替代方案

| 方案 | 優點 | 缺點 | 拒絕原因 |
|------|------|------|----------|
| **記憶體內計數器** | 最快 | 無法跨實例共享 | 不支援水平擴展 |
| **Token Bucket 演算法** | 更精確控制爆發流量 | 實作複雜 | Fixed Window 已足夠當前需求 |
| **分散式快取 (IDistributedCache)** | 抽象化介面 | 額外抽象層降低效能 | 直接使用 Redis 更高效 |

### 實作重點
1. **Fixed Window 演算法** (規格要求):
   ```csharp
   var key = $"ratelimit:{clientIp}:{currentMinute}";
   var count = await redis.StringIncrementAsync(key);
   
   if (count == 1) {
       await redis.KeyExpireAsync(key, TimeSpan.FromSeconds(60));
   }
   
   if (count > 100) {
       return 429; // Too Many Requests
   }
   ```

2. **降級策略** (Redis 不可用時):
   ```csharp
   try {
       // 執行 Rate Limiting
   } catch (RedisException) {
       logger.LogWarning("Redis unavailable, bypassing rate limit");
       // 允許請求通過，記錄告警
   }
   ```

3. **連線池管理**:
   - 使用 `ConnectionMultiplexer.Connect()` 建立單例連線
   - 設定 `ConnectRetry`, `ConnectTimeout`, `SyncTimeout` 避免阻塞

4. **IP 識別順序**:
   ```csharp
   var clientIp = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                  ?? httpContext.Connection.RemoteIpAddress?.ToString();
   ```

### 效能優化
- 使用 `StringIncrementAsync` + `KeyExpireAsync` 取代 Lua Script (簡化維護)
- 考慮使用 Redis Cluster 提升可用性 (生產環境)

### 參考資料
- [Redis Rate Limiting Pattern](https://redis.io/docs/manual/patterns/rate-limiter/)
- [StackExchange.Redis Best Practices](https://stackexchange.github.io/StackExchange.Redis/Basics.html)

---

## R-004: 請求聚合 (Request Aggregation) 模式

### 決策
使用 **並行 HTTP 呼叫 (Task.WhenAll)** 模式，透過 `IHttpClientFactory` 建立命名 HttpClient 呼叫後端服務。

### 理由
1. **最小延遲**: 並行呼叫使總延遲等於最慢的服務，而非所有服務延遲總和
2. **彈性處理部分失敗**: `Task.WhenAll` 可配合 `try-catch` 處理單一服務失敗
3. **HttpClient 最佳實踐**: `IHttpClientFactory` 管理連線池，避免 socket 耗盡
4. **符合規格要求**: FR-005 明確要求並行呼叫

### 評估的替代方案

| 方案 | 優點 | 缺點 | 拒絕原因 |
|------|------|------|----------|
| **序列呼叫** | 實作簡單 | 總延遲 = 累加，違反效能要求 | 不符合規格 FR-005 |
| **GraphQL Federation** | 統一查詢語言 | 需後端服務支援 GraphQL | 過度設計，後端為 REST API |
| **BFF (Backend for Frontend)** | 獨立服務處理聚合 | 額外維護成本 | 當前僅 1 個聚合端點，YAGNI |

### 實作重點
1. **並行呼叫範例**:
   ```csharp
   var auctionTask = auctionClient.GetAsync($"/api/auctions/{id}");
   var sellerTask = memberClient.GetAsync($"/api/members/{sellerId}");
   var bidsTask = biddingClient.GetAsync($"/api/bids?auctionId={id}");
   
   await Task.WhenAll(auctionTask, sellerTask, bidsTask);
   
   // 組合回應
   var response = new AggregatedAuctionResponse
   {
       Auction = await auctionTask.Result.Content.ReadFromJsonAsync<Auction>(),
       Seller = await sellerTask.Result.Content.ReadFromJsonAsync<Member>(),
       Bids = await bidsTask.Result.Content.ReadFromJsonAsync<List<Bid>>()
   };
   ```

2. **部分失敗處理** (符合 FR-005 要求):
   ```csharp
   var metadata = new DataAvailabilityMetadata
   {
       Auction = auctionTask.IsCompletedSuccessfully,
       Seller = sellerTask.IsCompletedSuccessfully,
       Bids = bidsTask.IsCompletedSuccessfully
   };
   ```

3. **超時控制**:
   ```csharp
   httpClient.Timeout = TimeSpan.FromSeconds(30); // 所有呼叫共用
   ```

4. **HttpClient 設定**:
   ```csharp
   services.AddHttpClient("MemberService", client =>
   {
       client.BaseAddress = new Uri(config["BackendServices:MemberService"]);
       client.DefaultRequestHeaders.Add("X-Gateway-Request-Id", /* Correlation ID */);
   })
   .AddPolicyHandler(GetRetryPolicy()) // 可選: Polly 重試策略
   .AddPolicyHandler(GetTimeoutPolicy());
   ```

### 錯誤處理策略
- **單一服務失敗**: 回傳部分資料 + metadata 標註
- **關鍵服務失敗 (如 Auction)**: 回傳 503 Service Unavailable
- **所有服務失敗**: 回傳 503 並記錄 Critical 日誌

### 參考資料
- [IHttpClientFactory Best Practices](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests)
- [Parallel Programming in .NET](https://learn.microsoft.com/en-us/dotnet/standard/parallel-programming/)

---

## R-005: 統一錯誤處理 Middleware 設計

### 決策
實作全域 **ExceptionHandlerMiddleware** 攔截所有未處理例外，轉換為符合 FR-004 規格的統一 JSON 格式。

### 理由
1. **單一責任**: 錯誤處理邏輯集中管理，避免散落在各 Controller
2. **一致性**: 保證所有錯誤回應格式一致
3. **安全性**: 避免洩漏內部實作細節 (如堆疊追蹤) 給客戶端
4. **可觀測性**: 集中記錄錯誤日誌，包含 Correlation ID

### 評估的替代方案

| 方案 | 優點 | 缺點 | 拒絕原因 |
|------|------|------|----------|
| **UseExceptionHandler()** | 內建方法 | 客製化程度低 | 需完全控制錯誤格式 |
| **Controller ActionFilter** | 可針對特定 Controller | 無法攔截 Middleware 例外 | 覆蓋範圍不足 |
| **Problem Details (RFC 7807)** | 標準格式 | 與規格 FR-004 不完全匹配 | 需符合既有規格 |

### 實作重點
1. **Middleware Pipeline 順序** (關鍵):
   ```csharp
   app.UseErrorHandling();        // 最外層，攔截所有例外
   app.UseAuthentication();
   app.UseRateLimiting();
   app.UseAuthorization();
   app.MapReverseProxy();
   ```

2. **錯誤分類與狀態碼對應**:
   ```csharp
   var statusCode = exception switch
   {
       UnauthorizedAccessException => 401,
       ForbiddenException => 403,
       NotFoundException => 404,
       RateLimitExceededException => 429,
       HttpRequestException => 503,
       TimeoutException => 504,
       _ => 500
   };
   ```

3. **錯誤回應模型** (符合 FR-004):
   ```csharp
   public class ErrorResponse
   {
       public ErrorDetail Error { get; set; }
   }
   
   public class ErrorDetail
   {
       public string Code { get; set; }          // "UNAUTHORIZED", "RATE_LIMIT_EXCEEDED"
       public string Message { get; set; }       // 使用者友善訊息
       public string Details { get; set; }       // 可選的額外說明
       public DateTime Timestamp { get; set; }   // ISO8601 格式
       public string Path { get; set; }          // 請求路徑
   }
   ```

4. **敏感資訊過濾**:
   - 生產環境: 僅回傳通用訊息 "服務暫時無法使用"
   - 開發環境: 可選擇性包含 StackTrace (透過設定控制)

### 日誌記錄
```csharp
logger.LogError(exception, 
    "Unhandled exception: {ErrorCode} | Path: {Path} | UserId: {UserId} | CorrelationId: {CorrelationId}",
    errorCode, path, userId, correlationId);
```

### 參考資料
- [ASP.NET Core Error Handling](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling)
- [RFC 7807 Problem Details](https://datatracker.ietf.org/doc/html/rfc7807)

---

## R-006: 結構化日誌與 APM 整合

### 決策
使用 **Serilog** 作為結構化日誌框架，整合 **Application Insights** (或 OpenTelemetry) 提供 APM 追蹤。

### 理由
1. **結構化日誌**: JSON 格式便於查詢與分析 (vs. 純文字日誌)
2. **Sink 豐富**: 支援 Console, File, Elasticsearch, Application Insights 等輸出
3. **效能優異**: 非同步寫入，不阻塞請求處理
4. **Context Enrichment**: 自動加入 Correlation ID, UserId, IP 等上下文資訊

### 評估的替代方案

| 方案 | 優點 | 缺點 | 拒絕原因 |
|------|------|------|----------|
| **ILogger (預設)** | 內建，無需額外依賴 | 結構化能力弱 | 需 JSON 格式輸出 |
| **NLog** | 功能豐富 | 設定複雜 | Serilog 社群更活躍 |
| **Log4net** | 成熟穩定 | 老舊架構，效能較差 | 不符合現代 .NET 最佳實踐 |

### 實作重點
1. **Serilog 設定**:
   ```csharp
   Log.Logger = new LoggerConfiguration()
       .MinimumLevel.Information()
       .Enrich.FromLogContext()
       .Enrich.WithProperty("Application", "ApiGateway")
       .Enrich.WithCorrelationId()
       .WriteTo.Console(new JsonFormatter())
       .WriteTo.ApplicationInsights(telemetryConfig, TelemetryConverter.Traces)
       .CreateLogger();
   ```

2. **關鍵記錄點** (符合 FR-012):
   - **請求開始**: Path, Method, IP, UserId
   - **請求結束**: StatusCode, Duration, ResponseSize
   - **Rate Limit 觸發**: IP, Endpoint, CurrentCount
   - **後端服務呼叫**: ServiceName, Duration, StatusCode
   - **錯誤**: Exception, StackTrace, Context

3. **Correlation ID 傳遞**:
   ```csharp
   var correlationId = httpContext.TraceIdentifier; // 或自訂 GUID
   httpContext.Response.Headers.Add("X-Correlation-Id", correlationId);
   // 傳遞到後端服務
   httpRequest.Headers.Add("X-Gateway-Request-Id", correlationId);
   ```

4. **APM 追蹤範圍**:
   ```csharp
   using (var activity = Activity.StartActivity("AggregateAuctionDetail"))
   {
       activity?.SetTag("auction.id", auctionId);
       // 執行聚合邏輯
   }
   ```

### 效能考量
- 使用非同步 Sink 避免 I/O 阻塞
- 限制日誌輸出大小 (避免記錄大型 Payload)
- 生產環境設定 `MinimumLevel.Information` (開發環境可用 Debug)

### 參考資料
- [Serilog Best Practices](https://github.com/serilog/serilog/wiki/Best-Practices)
- [Application Insights for ASP.NET Core](https://learn.microsoft.com/en-us/azure/azure-monitor/app/asp-net-core)

---

## R-007: 健康檢查實作模式

### 決策
使用 **ASP.NET Core Health Checks** 框架，實作自訂 `IHealthCheck` 檢查後端服務與 Redis 健康狀態。

### 理由
1. **框架原生**: 與 ASP.NET Core 整合，提供 `/health` 端點
2. **可擴展**: 可自訂檢查邏輯 (HTTP, Database, Redis)
3. **狀態分級**: Healthy / Degraded / Unhealthy 三種狀態
4. **依賴檢查**: 可個別檢查各依賴服務，提供詳細狀態

### 評估的替代方案

| 方案 | 優點 | 缺點 | 拒絕原因 |
|------|------|------|----------|
| **手寫 /health 端點** | 完全控制 | 需重複實作框架功能 | 重複造輪 |
| **第三方函式庫 (AspNetCore.Diagnostics.HealthChecks)** | 預建常見檢查 | 額外依賴 | 框架內建已足夠 |

### 實作重點
1. **健康檢查註冊**:
   ```csharp
   services.AddHealthChecks()
       .AddCheck<RedisHealthCheck>("redis")
       .AddCheck<BackendServiceHealthCheck>("member-service")
       .AddCheck<BackendServiceHealthCheck>("auction-service")
       .AddCheck<BackendServiceHealthCheck>("bidding-service");
   ```

2. **自訂健康檢查範例**:
   ```csharp
   public class BackendServiceHealthCheck : IHealthCheck
   {
       private readonly HttpClient _httpClient;
       
       public async Task<HealthCheckResult> CheckHealthAsync(
           HealthCheckContext context, 
           CancellationToken cancellationToken = default)
       {
           try
           {
               var response = await _httpClient.GetAsync("/health", cancellationToken);
               return response.IsSuccessStatusCode 
                   ? HealthCheckResult.Healthy() 
                   : HealthCheckResult.Unhealthy();
           }
           catch (Exception ex)
           {
               return HealthCheckResult.Unhealthy("Service unavailable", ex);
           }
       }
   }
   ```

3. **回應格式** (符合 FR-011):
   ```csharp
   app.MapHealthChecks("/health", new HealthCheckOptions
   {
       ResponseWriter = async (context, report) =>
       {
           var result = new
           {
               status = report.Status.ToString().ToLower(),
               timestamp = DateTime.UtcNow,
               services = report.Entries.ToDictionary(
                   e => e.Key,
                   e => e.Value.Status.ToString().ToLower()
               )
           };
           context.Response.ContentType = "application/json";
           await context.Response.WriteAsJsonAsync(result);
       }
   });
   ```

4. **檢查頻率**: 每 30 秒背景執行 (符合 FR-011)

### Kubernetes 整合
- **Liveness Probe**: `/health` (Gateway 本身存活)
- **Readiness Probe**: `/health/ready` (包含依賴服務檢查)

### 參考資料
- [ASP.NET Core Health Checks](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks)

---

## R-008: 測試策略與工具選擇

### 決策
採用 **三層測試金字塔** 策略:
1. **單元測試** (xUnit + Moq): 80%+ 覆蓋率
2. **整合測試** (WebApplicationFactory + Testcontainers): 覆蓋端到端流程
3. **契約測試** (WireMock.Net): 模擬後端服務回應

### 理由
1. **xUnit**: .NET 社群主流測試框架，與 Visual Studio / Rider 整合良好
2. **Moq**: 簡潔的 Mocking 語法，易於測試依賴注入
3. **FluentAssertions**: 提升測試可讀性 (`result.Should().Be(expected)`)
4. **Testcontainers**: 真實 Redis 環境，避免 Mock 行為不一致
5. **WebApplicationFactory**: 啟動完整 API 進行整合測試，無需部署

### 評估的替代方案

| 方案 | 優點 | 缺點 | 拒絕原因 |
|------|------|------|----------|
| **NUnit** | 功能強大 | 語法較複雜 | xUnit 更符合 .NET Core 風格 |
| **FakeItEasy** | 語法友善 | 社群較小 | Moq 社群更成熟 |
| **Docker Compose (測試)** | 真實環境 | 啟動慢，CI 難整合 | Testcontainers 更靈活 |

### 實作重點
1. **單元測試範例**:
   ```csharp
   [Fact]
   public async Task RateLimitService_ShouldReturnTrue_WhenUnderLimit()
   {
       // Arrange
       var redisMock = new Mock<IConnectionMultiplexer>();
       var service = new RateLimitService(redisMock.Object);
       
       // Act
       var result = await service.IsAllowedAsync("127.0.0.1");
       
       // Assert
       result.Should().BeTrue();
   }
   ```

2. **整合測試範例**:
   ```csharp
   public class RoutingTests : IClassFixture<WebApplicationFactory<Program>>
   {
       [Fact]
       public async Task Should_RouteToMemberService()
       {
           // Arrange
           var client = _factory.CreateClient();
           
           // Act
           var response = await client.GetAsync("/api/members/123");
           
           // Assert
           response.StatusCode.Should().Be(HttpStatusCode.OK);
       }
   }
   ```

3. **Testcontainers 設定**:
   ```csharp
   var redisContainer = new ContainerBuilder()
       .WithImage("redis:7-alpine")
       .WithPortBinding(6379, true)
       .Build();
   
   await redisContainer.StartAsync();
   ```

4. **測試覆蓋率目標** (符合 Constitution 原則 II):
   - 單元測試: > 80%
   - 整合測試: 覆蓋所有 User Stories 驗收標準
   - 效能測試: 使用 BenchmarkDotNet 驗證 < 10ms/20ms/300ms 目標

### CI/CD 整合
- 測試在 GitHub Actions / Azure DevOps Pipeline 自動執行
- 失敗測試阻止合併 (Quality Gate)

### 參考資料
- [xUnit Best Practices](https://xunit.net/docs/getting-started/netcore/cmdline)
- [Testcontainers for .NET](https://dotnet.testcontainers.org/)

---

## R-009: 設定管理與環境變數

### 決策
使用 **appsettings.json** 作為基礎設定，敏感資訊 (JWT Secret, Redis 密碼) 透過 **環境變數** 或 **Azure Key Vault** 覆寫。

### 理由
1. **12-Factor App**: 設定與程式碼分離，支援多環境部署
2. **安全性**: 敏感資訊不寫入版控，透過 CI/CD 注入
3. **可維護性**: 透過 `IOptions<T>` 強型別存取設定
4. **熱重載**: ASP.NET Core 支援 `appsettings.json` 熱重載 (開發環境)

### 評估的替代方案

| 方案 | 優點 | 缺點 | 拒絕原因 |
|------|------|------|----------|
| **硬編碼設定** | 簡單 | 不安全，難以維護 | 違反最佳實踐 |
| **資料庫設定** | 集中管理 | 啟動時需資料庫可用 | 過度設計 |
| **Consul / etcd** | 動態設定 | 增加架構複雜度 | YAGNI (未來可透過 IServiceDiscovery 擴展) |

### 實作重點
1. **設定檔結構** (`appsettings.json`):
   ```json
   {
     "Jwt": {
       "Issuer": "AuctionService",
       "Audience": "ApiGateway",
       "SecretKey": "OVERRIDE_WITH_ENV_VAR"  // 生產環境覆寫
     },
     "Redis": {
       "ConnectionString": "localhost:6379",
       "Password": "OVERRIDE_WITH_ENV_VAR"
     },
     "BackendServices": {
       "MemberService": "http://member-service:5001",
       "AuctionService": "http://auction-service:5002",
       "BiddingService": "http://bidding-service:5003"
     },
     "RateLimit": {
       "RequestsPerMinute": 100
     }
   }
   ```

2. **環境變數覆寫** (使用雙底線替換冒號):
   ```bash
   export Jwt__SecretKey="your-secret-key"
   export Redis__Password="redis-password"
   ```

3. **強型別設定類別**:
   ```csharp
   public class JwtSettings
   {
       public string Issuer { get; set; }
       public string Audience { get; set; }
       public string SecretKey { get; set; }
   }
   
   // 註冊
   services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
   
   // 使用
   public MyService(IOptions<JwtSettings> jwtSettings) { }
   ```

4. **Azure Key Vault 整合** (生產環境):
   ```csharp
   builder.Configuration.AddAzureKeyVault(
       new Uri("https://your-keyvault.vault.azure.net/"),
       new DefaultAzureCredential());
   ```

### Docker 環境變數注入
```dockerfile
ENV Jwt__SecretKey=${JWT_SECRET}
ENV Redis__Password=${REDIS_PASSWORD}
```

### 參考資料
- [Configuration in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [Azure Key Vault Configuration Provider](https://learn.microsoft.com/en-us/aspnet/core/security/key-vault-configuration)

---

## R-010: CORS 設定與安全標頭

### 決策
使用 **ASP.NET Core CORS Middleware** 設定允許的來源，並透過自訂 Middleware 加入安全標頭 (HSTS, X-Content-Type-Options 等)。

### 理由
1. **瀏覽器安全**: CORS 防止未授權網域的 JavaScript 存取 API
2. **靈活設定**: 可針對不同環境設定不同 Origins
3. **安全加固**: 安全標頭防護 XSS, Clickjacking, MIME sniffing 攻擊

### 實作重點
1. **CORS 設定**:
   ```csharp
   services.AddCors(options =>
   {
       options.AddDefaultPolicy(builder =>
       {
           builder.WithOrigins(config["Cors:AllowedOrigins"].Split(','))
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
       });
   });
   
   app.UseCors(); // 必須在 UseAuthentication 之前
   ```

2. **安全標頭**:
   ```csharp
   app.Use(async (context, next) =>
   {
       context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
       context.Response.Headers.Add("X-Frame-Options", "DENY");
       context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
       context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000");
       await next();
   });
   ```

### 參考資料
- [CORS in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/cors)

---

## 總結與下一步

### 研究結論
所有關鍵技術決策已完成，具備充分的實作指引:
- ✅ YARP 反向代理架構清晰
- ✅ JWT 驗證機制明確
- ✅ Redis Rate Limiting 演算法確定
- ✅ 請求聚合模式定義完整
- ✅ 錯誤處理、日誌、健康檢查策略明確
- ✅ 測試策略與工具選擇完成
- ✅ 設定管理與安全考量涵蓋

### 未解決的技術問題
無 (所有 NEEDS CLARIFICATION 已解決)

### Phase 1 準備就緒
可進入設計階段 (data-model.md, contracts/, quickstart.md)。

---

**產生日期**: 2025-12-04  
**版本**: 1.0  
**狀態**: ✅ 完成
