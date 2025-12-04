# 快速入門: API Gateway

**目的**: 提供開發者快速建立、執行、測試 API Gateway 專案的步驟指南

---

## 前置需求

### 開發環境
- **.NET SDK**: 10.0 或更高版本
  - 驗證: `dotnet --version`
- **IDE**: Visual Studio 2022 / JetBrains Rider / VS Code
- **Git**: 版本控制
- **Docker Desktop**: 本地 Redis 容器 (可選)

### 外部服務
- **Redis**: Rate Limiting 計數器 (本機或容器)
  - Docker: `docker run -d -p 6379:6379 redis:7-alpine`
- **後端微服務** (Member, Auction, Bidding Service):
  - 可使用模擬服務 (WireMock) 進行開發

---

## 專案初始化

### 1. 建立專案結構

```powershell
# 建立解決方案
dotnet new sln -n AuctionService

# 建立 API Gateway 專案 (Web API)
dotnet new webapi -n ApiGateway -o src/ApiGateway --no-openapi --use-controllers

# 建立測試專案
dotnet new xunit -n ApiGateway.Tests -o tests/ApiGateway.Tests

# 加入專案到解決方案
dotnet sln add src/ApiGateway/ApiGateway.csproj
dotnet sln add tests/ApiGateway.Tests/ApiGateway.Tests.csproj

# 加入專案參考 (測試專案參考主專案)
dotnet add tests/ApiGateway.Tests reference src/ApiGateway
```

### 2. 安裝 NuGet 套件

```powershell
cd src/ApiGateway

# YARP 反向代理
dotnet add package Yarp.ReverseProxy

# JWT 驗證
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer

# Redis
dotnet add package StackExchange.Redis

# 結構化日誌
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File

# 健康檢查 (內建，無需額外安裝)

cd ../../tests/ApiGateway.Tests

# 測試相關套件
dotnet add package Moq
dotnet add package FluentAssertions
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package Testcontainers
dotnet add package WireMock.Net
```

### 3. 設定檔範本

**appsettings.json** (根據 contracts/routes.yaml):

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  
  "Jwt": {
    "Issuer": "AuctionService",
    "Audience": "ApiGateway",
    "SecretKey": "YOUR_SECRET_KEY_HERE_MINIMUM_32_CHARS",
    "ExpirationMinutes": 60
  },
  
  "Redis": {
    "ConnectionString": "localhost:6379",
    "Password": ""
  },
  
  "RateLimit": {
    "RequestsPerMinute": 100,
    "EnableRedisFailover": true
  },
  
  "BackendServices": {
    "MemberService": "http://localhost:5001",
    "AuctionService": "http://localhost:5002",
    "BiddingService": "http://localhost:5003"
  },
  
  "Cors": {
    "AllowedOrigins": "http://localhost:3000,https://auction.example.com"
  },
  
  "ReverseProxy": {
    "Routes": {
      "member-route": {
        "ClusterId": "member-cluster",
        "Match": {
          "Path": "/api/members/{**catch-all}"
        },
        "Order": 100
      },
      "auction-route": {
        "ClusterId": "auction-cluster",
        "Match": {
          "Path": "/api/auctions/{**catch-all}"
        },
        "Order": 100
      },
      "bidding-route": {
        "ClusterId": "bidding-cluster",
        "Match": {
          "Path": "/api/bids/{**catch-all}"
        },
        "Order": 100
      },
      "my-bids-route": {
        "ClusterId": "bidding-cluster",
        "Match": {
          "Path": "/api/me/bids"
        },
        "Order": 1
      },
      "my-follows-route": {
        "ClusterId": "auction-cluster",
        "Match": {
          "Path": "/api/me/follows"
        },
        "Order": 1
      },
      "my-profile-route": {
        "ClusterId": "member-cluster",
        "Match": {
          "Path": "/api/me"
        },
        "Order": 1
      }
    },
    "Clusters": {
      "member-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://localhost:5001"
          }
        },
        "HealthCheck": {
          "Active": {
            "Enabled": true,
            "Interval": "00:00:30",
            "Timeout": "00:00:10",
            "Path": "/health"
          }
        }
      },
      "auction-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://localhost:5002"
          }
        },
        "HealthCheck": {
          "Active": {
            "Enabled": true,
            "Interval": "00:00:30",
            "Timeout": "00:00:10",
            "Path": "/health"
          }
        }
      },
      "bidding-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://localhost:5003"
          }
        },
        "HealthCheck": {
          "Active": {
            "Enabled": true,
            "Interval": "00:00:30",
            "Timeout": "00:00:10",
            "Path": "/health"
          }
        }
      }
    }
  }
}
```

**appsettings.Development.json**:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  },
  
  "Jwt": {
    "SecretKey": "development-secret-key-min-32-chars-long"
  },
  
  "Redis": {
    "ConnectionString": "localhost:6379"
  }
}
```

---

## 執行專案

### 本地開發 (單機)

```powershell
# 1. 啟動 Redis (Docker)
docker run -d -p 6379:6379 --name redis-dev redis:7-alpine

# 2. 啟動 API Gateway
cd src/ApiGateway
dotnet run

# 預設監聽: http://localhost:5000 (或 https://localhost:5001)
```

### 使用 Docker Compose (建議)

**docker-compose.yml**:

```yaml
version: '3.8'

services:
  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      timeout: 5s
      retries: 3

  api-gateway:
    build:
      context: .
      dockerfile: docker/Dockerfile
    ports:
      - "5000:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - Redis__ConnectionString=redis:6379
      - Jwt__SecretKey=${JWT_SECRET}
    depends_on:
      redis:
        condition: service_healthy
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3

  # 模擬後端服務 (使用 WireMock)
  wiremock:
    image: wiremock/wiremock:latest
    ports:
      - "5001:8080"  # Member Service mock
      - "5002:8081"  # Auction Service mock
      - "5003:8082"  # Bidding Service mock
    command: --port 8080 --verbose
```

執行:

```powershell
docker-compose up -d
```

---

## 測試指南

### 手動測試 (使用 curl)

#### 1. 健康檢查

```powershell
# 檢查 Gateway 健康狀態
curl http://localhost:5000/health

# 預期回應:
# {
#   "status": "healthy",
#   "timestamp": "2025-12-04T10:30:00Z",
#   "services": {
#     "memberService": "healthy",
#     "auctionService": "healthy",
#     "biddingService": "healthy"
#   }
# }
```

#### 2. 公開端點 (無需 JWT)

```powershell
# 取得商品清單
curl http://localhost:5000/api/auctions

# 取得商品詳細
curl http://localhost:5000/api/auctions/123

# 商品詳細聚合端點
curl http://localhost:5000/api/aggregated/auctions/123
```

#### 3. 私有端點 (需要 JWT)

```powershell
# 設定 JWT Token (從登入端點取得)
$TOKEN = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."

# 取得個人資料
curl -H "Authorization: Bearer $TOKEN" http://localhost:5000/api/me

# 取得個人出價記錄
curl -H "Authorization: Bearer $TOKEN" http://localhost:5000/api/me/bids
```

#### 4. Rate Limiting 測試

```powershell
# 快速發送 101 次請求 (超過限制)
for ($i=1; $i -le 101; $i++) {
    curl http://localhost:5000/api/auctions
}

# 第 101 次請求預期回應 429 Too Many Requests:
# {
#   "error": {
#     "code": "RATE_LIMIT_EXCEEDED",
#     "message": "請求次數超過限制，請稍後再試",
#     "details": "每分鐘最多 100 次請求",
#     "timestamp": "2025-12-04T10:30:00Z",
#     "path": "/api/auctions"
#   }
# }
```

### 自動化測試

```powershell
# 執行所有測試
dotnet test

# 執行特定測試類別
dotnet test --filter "FullyQualifiedName~RateLimitingTests"

# 產生覆蓋率報告
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### 整合測試範例

**RoutingTests.cs**:

```csharp
public class RoutingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public RoutingTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GET_ApiMembers_Should_RouteToMemberService()
    {
        // Act
        var response = await _client.GetAsync("/api/members/123");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // 驗證請求已轉發到 Member Service (使用 WireMock 驗證)
    }
}
```

---

## 效能測試

### 使用 BenchmarkDotNet (微基準測試)

```csharp
[MemoryDiagnoser]
public class RateLimitBenchmark
{
    private RateLimitService _service;

    [GlobalSetup]
    public void Setup()
    {
        var redis = ConnectionMultiplexer.Connect("localhost");
        _service = new RateLimitService(redis);
    }

    [Benchmark]
    public async Task<bool> CheckRateLimit()
    {
        return await _service.IsAllowedAsync("127.0.0.1");
    }
}

// 執行: dotnet run -c Release
```

### 負載測試 (使用 Apache Bench)

```powershell
# 測試路由延遲 (1000 請求, 10 併發)
ab -n 1000 -c 10 http://localhost:5000/api/auctions

# 預期結果:
# - 95% 請求 < 10ms
# - 無錯誤
```

---

## 常見問題排解

### 問題 1: Redis 連線失敗

**錯誤訊息**: `It was not possible to connect to the redis server(s)`

**解決方法**:
```powershell
# 檢查 Redis 是否運行
docker ps | grep redis

# 重啟 Redis
docker restart redis-dev

# 驗證連線
redis-cli -h localhost -p 6379 ping
# 預期回應: PONG
```

### 問題 2: JWT 驗證失敗

**錯誤訊息**: `401 Unauthorized`

**解決方法**:
1. 檢查 JWT Secret 是否設定 (長度 >= 32 字元)
2. 驗證 Token 格式: `Authorization: Bearer <token>`
3. 檢查 Token 是否過期 (使用 https://jwt.io/ 解碼)

### 問題 3: 後端服務路由失敗

**錯誤訊息**: `503 Service Unavailable`

**解決方法**:
1. 檢查後端服務是否運行
2. 驗證 `appsettings.json` 中的服務網址
3. 檢查健康檢查端點: `curl http://localhost:5001/health`

### 問題 4: Rate Limiting 無效

**解決方法**:
1. 檢查 Redis 是否正常運作
2. 驗證 IP 識別邏輯 (X-Forwarded-For vs RemoteIpAddress)
3. 檢查降級策略是否觸發 (Redis 不可用時允許所有請求)

---

## 開發工作流程

### TDD 循環 (Red-Green-Refactor)

1. **Red**: 先寫失敗的測試
   ```csharp
   [Fact]
   public async Task RateLimiting_Should_Block_After_100_Requests()
   {
       // Arrange
       var service = CreateRateLimitService();
       
       // Act
       for (int i = 0; i < 100; i++)
           await service.IsAllowedAsync("127.0.0.1");
       
       var result = await service.IsAllowedAsync("127.0.0.1");
       
       // Assert
       result.Should().BeFalse(); // 第 101 次請求應被阻擋
   }
   ```

2. **Green**: 實作最小可行程式碼
   ```csharp
   public async Task<bool> IsAllowedAsync(string clientIp)
   {
       var key = $"ratelimit:{clientIp}:{GetCurrentMinute()}";
       var count = await _redis.StringIncrementAsync(key);
       
       if (count == 1)
           await _redis.KeyExpireAsync(key, TimeSpan.FromSeconds(60));
       
       return count <= 100;
   }
   ```

3. **Refactor**: 重構提升品質
   - 提取魔術數字 (100) 為設定參數
   - 加入日誌記錄
   - 優化錯誤處理

### Git 提交訊息範例

```
feat(rate-limiting): implement Redis-based rate limiter

- Add RateLimitService with INCR + TTL pattern
- Integrate with RateLimitingMiddleware
- Add unit tests with 90% coverage
- Add integration tests with Testcontainers

Relates to: #004-api-gateway
```

---

## 部署檢查清單

### 環境變數設定

```bash
# 生產環境必須覆寫
export Jwt__SecretKey="YOUR_PRODUCTION_SECRET_KEY_MIN_32_CHARS"
export Redis__ConnectionString="redis-cluster.prod.example.com:6379"
export Redis__Password="YOUR_REDIS_PASSWORD"

# 後端服務網址
export BackendServices__MemberService="http://member-service.prod:5001"
export BackendServices__AuctionService="http://auction-service.prod:5002"
export BackendServices__BiddingService="http://bidding-service.prod:5003"

# CORS 設定
export Cors__AllowedOrigins="https://auction.example.com,https://www.auction.example.com"
```

### Kubernetes 部署範例

**deployment.yaml**:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: api-gateway
spec:
  replicas: 3  # 水平擴展
  selector:
    matchLabels:
      app: api-gateway
  template:
    metadata:
      labels:
        app: api-gateway
    spec:
      containers:
      - name: api-gateway
        image: auctionservice/api-gateway:latest
        ports:
        - containerPort: 8080
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: Jwt__SecretKey
          valueFrom:
            secretKeyRef:
              name: jwt-secret
              key: secret-key
        - name: Redis__ConnectionString
          value: "redis-service:6379"
        livenessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 30
        readinessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 10
          periodSeconds: 10
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
---
apiVersion: v1
kind: Service
metadata:
  name: api-gateway-service
spec:
  type: LoadBalancer
  ports:
  - port: 80
    targetPort: 8080
  selector:
    app: api-gateway
```

---

## 監控與日誌

### Serilog 設定 (Program.cs)

```csharp
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "ApiGateway")
    .WriteTo.Console(new JsonFormatter())
    .WriteTo.File(
        path: "logs/api-gateway-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();
```

### Application Insights 整合

```csharp
builder.Services.AddApplicationInsightsTelemetry(
    builder.Configuration["ApplicationInsights:ConnectionString"]);
```

---

## 下一步

1. **Phase 2**: 執行 `/speckit.tasks` 產生任務清單 (tasks.md)
2. **開發**: 按照 TDD 流程實作功能
3. **測試**: 確保測試覆蓋率 > 80%
4. **部署**: 使用 Docker Compose 或 Kubernetes
5. **監控**: 設定 Application Insights 與告警

---

**文件版本**: 1.0  
**最後更新**: 2025-12-04  
**狀態**: ✅ 完成
