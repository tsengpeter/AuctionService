# 資料模型: API Gateway

**日期**: 2025-12-04  
**專案**: 004-api-gateway  
**目的**: 定義 API Gateway 內部使用的資料結構與狀態管理模型

---

## 概述

API Gateway 作為無狀態服務，**不儲存業務資料**於持久化資料庫。本文件定義的資料模型主要用於:
1. **記憶體內狀態管理** (服務健康狀態、連線池)
2. **暫時性快取** (Redis Rate Limiting 計數器)
3. **請求/回應 DTO** (錯誤格式、聚合回應)

---

## 核心實體

### 1. Route (路由規則)

**用途**: 定義 URL 路徑與後端服務的對應關係 (由 YARP 管理)

**來源**: `appsettings.json` (YARP ReverseProxy 設定)

**屬性**:
```csharp
public class RouteConfig
{
    public string RouteId { get; set; }           // 路由唯一識別碼 (如 "member-route")
    public string ClusterId { get; set; }         // 對應的後端服務叢集
    public PathMatch Match { get; set; }          // 路徑匹配規則
    public int? Order { get; set; }               // 優先順序 (數字越小越優先)
    public Dictionary<string, string> Metadata { get; set; }  // 額外屬性 (如 RequireAuth)
}

public class PathMatch
{
    public string Path { get; set; }              // 路徑模式 (如 "/api/members/{**catch-all}")
}
```

**驗證規則**:
- `RouteId` 必須唯一
- `Path` 必須以 `/` 開頭
- `Order` 值用於解決路徑衝突 (如 `/api/me/bids` vs `/api/me/**`)

**狀態轉換**: 無 (靜態設定)

**關聯**:
- 1 Route → 1 Cluster (後端服務叢集)

---

### 2. ServiceHealthStatus (服務健康狀態)

**用途**: 追蹤後端微服務的健康狀態，用於健康檢查端點回應

**儲存位置**: 記憶體內 (IMemoryCache)

**屬性**:
```csharp
public class ServiceHealthStatus
{
    public string ServiceName { get; set; }       // 服務名稱 (如 "MemberService")
    public HealthStatus Status { get; set; }      // 健康狀態 (Healthy/Unhealthy/Degraded)
    public DateTime LastChecked { get; set; }     // 最後檢查時間
    public string Message { get; set; }           // 狀態訊息 (錯誤時說明原因)
    public TimeSpan ResponseTime { get; set; }    // 健康檢查回應時間
}

public enum HealthStatus
{
    Healthy,      // 服務正常運作
    Unhealthy,    // 服務不可用
    Degraded      // 服務可用但效能降級
}
```

**驗證規則**:
- `LastChecked` 不可為未來時間
- `ResponseTime` 必須 >= 0

**狀態轉換**:
```
Healthy → Unhealthy (健康檢查失敗)
Unhealthy → Healthy (健康檢查恢復)
Healthy → Degraded (回應時間過長)
```

**生命週期**:
- 每 30 秒更新一次 (背景服務)
- 快取 TTL: 60 秒

**關聯**: 無

---

### 3. RateLimitCounter (限流計數器)

**用途**: 追蹤每個 IP 的請求次數，實作 Rate Limiting

**儲存位置**: Redis

**屬性**:
```csharp
public class RateLimitCounter
{
    public string ClientIp { get; set; }          // 客戶端 IP (從 X-Forwarded-For 或連線 IP)
    public int CurrentMinute { get; set; }        // 當前分鐘時間戳 (Unix timestamp / 60)
    public int RequestCount { get; set; }         // 當前分鐘的請求次數
    public DateTime ExpiresAt { get; set; }       // 計數器過期時間 (當前分鐘 + 60 秒)
}
```

**Redis Key 格式**:
```
ratelimit:{ClientIp}:{CurrentMinute}
```

**範例**:
```
Key: ratelimit:192.168.1.100:29540190
Value: 45
TTL: 30 seconds
```

**驗證規則**:
- `RequestCount` 必須 >= 0
- `ExpiresAt` 必須為未來時間

**狀態轉換**:
```
INCR → RequestCount + 1
TTL 到期 → 自動刪除
```

**限制**:
- 每個 IP 每分鐘最多 100 次請求 (可設定)
- 超過限制回傳 429 Too Many Requests

**關聯**: 無

---

### 4. ErrorResponse (錯誤回應)

**用途**: 統一所有錯誤回應的 JSON 格式 (符合 FR-004)

**儲存位置**: 不儲存 (每次請求產生)

**屬性**:
```csharp
public class ErrorResponse
{
    public ErrorDetail Error { get; set; }
}

public class ErrorDetail
{
    public string Code { get; set; }              // 錯誤代碼 (如 "UNAUTHORIZED", "RATE_LIMIT_EXCEEDED")
    public string Message { get; set; }           // 使用者友善訊息
    public string Details { get; set; }           // 選用的額外說明
    public DateTime Timestamp { get; set; }       // ISO8601 格式
    public string Path { get; set; }              // 請求路徑
}
```

**範例**:
```json
{
  "error": {
    "code": "RATE_LIMIT_EXCEEDED",
    "message": "請求次數超過限制，請稍後再試",
    "details": "每分鐘最多 100 次請求",
    "timestamp": "2025-12-04T10:30:00Z",
    "path": "/api/auctions"
  }
}
```

**驗證規則**:
- `Code` 必須為大寫英文與底線
- `Message` 必須為繁體中文 (使用者友善)
- `Timestamp` 必須為 UTC 時間

**錯誤代碼對應**:

| HTTP Status | Code | Message |
|-------------|------|---------|
| 401 | UNAUTHORIZED | 未授權，請提供有效的認證資訊 |
| 401 | TOKEN_EXPIRED | JWT 已過期 |
| 401 | INVALID_TOKEN | JWT 無效或簽章錯誤 |
| 401 | MALFORMED_TOKEN | JWT 格式錯誤 |
| 404 | NOT_FOUND | 請求的資源不存在 |
| 413 | PAYLOAD_TOO_LARGE | 請求內容過大 (最大 10MB) |
| 429 | RATE_LIMIT_EXCEEDED | 請求次數超過限制 |
| 500 | INTERNAL_SERVER_ERROR | 服務暫時無法使用 |
| 503 | SERVICE_UNAVAILABLE | 後端服務暫時無法使用 |
| 504 | GATEWAY_TIMEOUT | 後端服務回應超時 |

**關聯**: 無

---

### 5. AggregatedAuctionResponse (商品聚合回應)

**用途**: 組合多個後端服務的資料，提供完整的商品詳細資訊

**儲存位置**: 不儲存 (每次請求產生)

**屬性**:
```csharp
public class AggregatedAuctionResponse
{
    public AuctionDetail Auction { get; set; }         // 商品資訊 (來自 Auction Service)
    public SellerProfile Seller { get; set; }          // 賣家資訊 (來自 Member Service)
    public List<BidRecord> Bids { get; set; }          // 出價歷史 (來自 Bidding Service，最近 10 筆)
    public DataAvailabilityMetadata Metadata { get; set; }  // 資料可用性狀態
}

public class AuctionDetail
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public decimal StartingPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int SellerId { get; set; }
    public string Status { get; set; }                 // "Active", "Ended", "Cancelled"
}

public class SellerProfile
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public double Rating { get; set; }
    public int TotalSales { get; set; }
}

public class BidRecord
{
    public int Id { get; set; }
    public int BidderId { get; set; }
    public string BidderName { get; set; }
    public decimal Amount { get; set; }
    public DateTime BidTime { get; set; }
}

public class DataAvailabilityMetadata
{
    public bool Auction { get; set; }
    public bool Seller { get; set; }
    public bool Bids { get; set; }
}
```

**範例**:
```json
{
  "auction": {
    "id": 123,
    "title": "古董花瓶",
    "currentPrice": 5000,
    "status": "Active"
  },
  "seller": {
    "id": 456,
    "username": "antique_seller",
    "rating": 4.8
  },
  "bids": [
    {
      "id": 789,
      "bidderName": "buyer1",
      "amount": 5000,
      "bidTime": "2025-12-04T10:00:00Z"
    }
  ],
  "metadata": {
    "dataAvailability": {
      "auction": true,
      "seller": true,
      "bids": true
    }
  }
}
```

**驗證規則**:
- `Auction` 為必要欄位 (若 Auction Service 失敗則整個請求失敗)
- `Seller` 與 `Bids` 可為 null (部分失敗時)
- `Metadata` 必須反映實際資料可用性

**錯誤處理**:
- Auction Service 失敗 → 回傳 503 Service Unavailable
- Seller Service 失敗 → `Seller` = null, `Metadata.Seller` = false
- Bidding Service 失敗 → `Bids` = [], `Metadata.Bids` = false

**關聯**:
- 依賴 Auction Service, Member Service, Bidding Service

---

### 6. HealthCheckResponse (健康檢查回應)

**用途**: 提供 Gateway 與後端服務的健康狀態 (符合 FR-011)

**儲存位置**: 不儲存 (每次請求產生)

**屬性**:
```csharp
public class HealthCheckResponse
{
    public string Status { get; set; }                 // "healthy", "degraded", "unhealthy"
    public DateTime Timestamp { get; set; }            // ISO8601 格式
    public Dictionary<string, string> Services { get; set; }  // 各服務健康狀態
}
```

**範例**:
```json
{
  "status": "healthy",
  "timestamp": "2025-12-04T10:30:00Z",
  "services": {
    "memberService": "healthy",
    "auctionService": "healthy",
    "biddingService": "healthy"
  }
}
```

**狀態判定邏輯**:
```
所有服務 Healthy → "healthy"
部分服務 Unhealthy → "degraded"
所有服務 Unhealthy → "unhealthy"
```

**關聯**:
- 使用 `ServiceHealthStatus` 資料

---

## 暫時性資料 (Redis)

### Rate Limiting 計數器

**Key 格式**: `ratelimit:{ip}:{minute}`  
**Value 類型**: Integer (請求次數)  
**TTL**: 60 秒  

**操作**:
```redis
INCR ratelimit:192.168.1.100:29540190
EXPIRE ratelimit:192.168.1.100:29540190 60
```

### 健康檢查快取 (可選)

**Key 格式**: `health:{serviceName}`  
**Value 類型**: JSON (ServiceHealthStatus)  
**TTL**: 30 秒  

---

## 記憶體內狀態

### HttpClient 連線池

**管理者**: IHttpClientFactory  
**生命週期**: Singleton (連線重用)  
**設定**:
- PooledConnectionLifetime: 5 分鐘
- MaxConnectionsPerServer: 100

### YARP 路由快取

**管理者**: YARP InMemoryConfigProvider  
**生命週期**: Application lifetime  
**更新機制**: 熱重載 (appsettings.json 變更時)

---

## 資料流向

### 請求處理流程
```
1. 客戶端請求 → Gateway
2. RateLimitingMiddleware 檢查 Redis (RateLimitCounter)
3. JwtAuthenticationMiddleware 驗證 JWT
4. YARP 根據 Route 轉發到後端服務
5. 後端服務回應 → Gateway
6. ErrorHandlingMiddleware 轉換錯誤 (ErrorResponse)
7. Gateway → 客戶端
```

### 請求聚合流程
```
1. 客戶端請求 /api/aggregated/auctions/{id}
2. AggregationController 並行呼叫:
   - Auction Service → AuctionDetail
   - Member Service → SellerProfile
   - Bidding Service → BidRecord[]
3. 組合為 AggregatedAuctionResponse
4. Gateway → 客戶端
```

### 健康檢查流程
```
1. 背景服務每 30 秒執行
2. 並行呼叫各後端服務 /health
3. 更新 ServiceHealthStatus (記憶體)
4. 客戶端請求 /health → HealthCheckResponse
```

---

## 資料完整性與驗證

### 輸入驗證
- **JWT**: 使用 ASP.NET Core Authentication Middleware 驗證
- **路由參數**: YARP 自動驗證路徑格式
- **請求大小**: Kestrel MaxRequestBodySize 設定為 10MB

### 輸出驗證
- **錯誤回應**: 所有錯誤必須符合 ErrorResponse 格式
- **健康檢查**: 所有狀態值必須為 "healthy", "degraded", "unhealthy"

---

## 效能考量

### Redis 連線池
- 使用 `ConnectionMultiplexer` 單例模式
- 連線重用避免 TCP 握手開銷

### 記憶體快取
- `ServiceHealthStatus` 使用 `IMemoryCache`，避免每次請求呼叫後端

### 無狀態設計
- Gateway 不儲存 Session，支援水平擴展
- 所有狀態 (Rate Limit) 儲存於 Redis (共享)

---

## 安全性考量

### 敏感資料保護
- **JWT Secret**: 不儲存於程式碼或資料模型，透過環境變數注入
- **Redis 密碼**: 同上

### 資料暴露防護
- **錯誤訊息**: 生產環境不暴露堆疊追蹤或內部路徑
- **健康檢查**: 不暴露後端服務的實際 IP 或內部網址

---

## 未來擴展

### 動態服務發現
目前使用靜態設定 (`appsettings.json`)，未來可擴展:
- 實作 `IServiceDiscovery` 介面的 Consul/Eureka Provider
- 動態更新 YARP Cluster 設定

### 分散式追蹤
擴展 `CorrelationId` 模型，整合 OpenTelemetry:
```csharp
public class TraceContext
{
    public string TraceId { get; set; }
    public string SpanId { get; set; }
    public string ParentSpanId { get; set; }
}
```

---

**產生日期**: 2025-12-04  
**版本**: 1.0  
**狀態**: ✅ 完成
