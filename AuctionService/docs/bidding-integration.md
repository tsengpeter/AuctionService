# 出價功能集成文檔

## 概述

AuctionService 與 BiddingService 通過 HTTP API 進行集成，實現拍賣商品的出價功能。本文檔詳細說明 AuctionService 對 BiddingService 的期望 API 契約、錯誤處理策略以及相關業務邏輯。

> **重要說明**: 本文檔描述的是 AuctionService 期望的 BiddingService API 契約。目前 BiddingService 服務尚未實現，AuctionService 使用容錯設計，當 BiddingService 不可用時返回預設值以確保系統正常運作。

## 架構概覽

```
┌─────────────────┐    HTTP API    ┌─────────────────┐
│  AuctionService │◄─────────────►│  BiddingService │
│                 │                │  (計劃中)      │
│ - 商品管理      │                │                 │
│ - 出價查詢      │                │ - 出價處理      │
│ - 出價檢查      │                │ - 出價記錄      │
└─────────────────┘                └─────────────────┘
```

## 集成點

### 1. 獲取當前出價 (GetCurrentBid)

**用途**: 查詢商品的當前最高出價信息

**調用場景**:
- 用戶查看商品詳細信息時
- 追蹤清單顯示時
- 輕量化輪詢接口

**API 端點**: `GET /api/auctions/{id}/current-bid`

**期望的 BiddingService 響應格式**:
```json
{
  "success": true,
  "data": {
    "auctionId": "uuid",
    "currentPrice": 150.00,
    "bidCount": 5,
    "highestBidderUserId": "user123"
  },
  "message": null,
  "localizedMessage": null
}
```

**當 BiddingService 不可用時的處理**:
- AuctionService 返回預設值 (起標價，0 出價)
- 記錄警告日誌但不阻斷主要功能

### 2. 檢查商品是否有出價 (CheckAuctionHasBids)

**用途**: 在更新或刪除商品前檢查是否已有出價

**調用場景**:
- 用戶嘗試更新商品信息時
- 用戶嘗試刪除商品時

**期望的 BiddingService 響應格式**:
```json
{
  "success": true,
  "data": {
    "auctionId": "uuid",
    "hasBids": true,
    "bidCount": 3
  },
  "message": null,
  "localizedMessage": null
}
```

**當 BiddingService 不可用時的處理**:
- AuctionService 假設沒有出價 (返回 false)
- 允許業務邏輯繼續運作

## 當前實現狀態

### AuctionService 端
- ✅ 已實現 `IBiddingServiceClient` 接口
- ✅ 已實現 `BiddingServiceClient` 類別，包含彈性模式
- ✅ 已集成到業務邏輯中 (AuctionService, FollowService)
- ✅ 已配置 HttpClient 和 Polly 策略
- ✅ 已實現容錯設計 (當 BiddingService 不可用時返回預設值)

### BiddingService 端
- ❌ 服務尚未實現
- 📋 API 契約已定義 (本文檔)
- 📋 預期功能: 出價處理、出價記錄、出價歷史查詢

### 測試策略
- ✅ 單元測試使用 Mock 模擬 BiddingService
- ✅ 集成測試在 BiddingService 不可用時驗證容錯行為
- ❌ 端到端測試需要等到 BiddingService 實現後

## 彈性模式 (Resilience Patterns)

### 重試策略 (Retry Policy)

```csharp
// 指數退避重試: 2^retryAttempt 秒
private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}
```

### 電路斷路器 (Circuit Breaker)

```csharp
// 5 次連續失敗後開路，30 秒後半開
private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
}
```

### 超時設定

```csharp
// 請求超時: 10 秒
services.AddHttpClient<IBiddingServiceClient, BiddingServiceClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
})
```

## 錯誤處理策略

### 降級機制 (Graceful Degradation)

當 BiddingService 不可用時：

1. **商品詳細信息**: 顯示起標價作為當前價格，出價次數為 0
2. **商品更新檢查**: 假設無出價，允許更新（業務風險較低）
3. **記錄警告**: 記錄服務不可用但不阻斷主要功能

### 日誌記錄

所有 BiddingService 調用都會記錄詳細信息：

```csharp
_logger.LogInformation(
    "BiddingService call: {Endpoint} | Status: {StatusCode} | Duration: {Duration}ms | CorrelationId: {CorrelationId}",
    endpoint, statusCode, duration, correlationId);
```

## 配置設定

### appsettings.json

```json
{
  "BiddingService": {
    "BaseUrl": "http://localhost:5002"
  }
}
```

### 環境變數

```bash
# 開發環境
BiddingService__BaseUrl=http://localhost:5002

# 生產環境
BiddingService__BaseUrl=https://api.biddingservice.com
```

## 業務邏輯集成

### 商品狀態計算

商品狀態不僅依賴時間，還會考慮出價情況：

```csharp
public AuctionStatus CalculateStatus(this Auction auction)
{
    var now = DateTime.UtcNow;

    if (now < auction.StartTime)
        return AuctionStatus.Pending;

    if (now >= auction.StartTime && now < auction.EndTime)
        return AuctionStatus.Active;

    return AuctionStatus.Ended;
}
```

### 出價檢查業務規則

1. **商品更新**: 只有在無出價時才能更新商品信息
2. **商品刪除**: 只有在無出價時才能刪除商品
3. **出價顯示**: 總是顯示最新的出價信息（降級時顯示預設值）

## 測試策略

### 單元測試
- 模擬 BiddingServiceClient 的各種響應
- 測試降級機制和錯誤處理

### 集成測試
- 使用 Testcontainers 模擬完整的服務棧
- 測試實際的 HTTP 調用和錯誤場景

### 契約測試
- 驗證與 BiddingService 的 API 契約
- 確保接口變更時及時發現

## 監控和觀察性

### 健康檢查

健康檢查端點會驗證 BiddingService 的可用性：

```csharp
public async Task<IActionResult> Get()
{
    // 檢查資料庫連線
    var dbHealthy = await CheckDatabaseHealth();

    // 檢查 BiddingService 連線
    var biddingServiceHealthy = await CheckBiddingServiceHealth();

    return Ok(new {
        status = dbHealthy && biddingServiceHealthy ? "healthy" : "degraded",
        checks = new[] {
            new { name = "database", status = dbHealthy ? "healthy" : "unhealthy" },
            new { name = "biddingService", status = biddingServiceHealthy ? "healthy" : "unhealthy" }
        }
    });
}
```

### 指標收集

建議收集以下指標：
- BiddingService 調用成功率
- 平均響應時間
- 電路斷路器狀態
- 降級機制觸發次數

## 部署考慮

### 服務發現

在微服務環境中，應使用服務發現機制：

```csharp
// 使用 Consul 或 Eureka
services.AddConsulClient();
services.AddServiceDiscovery();
```

### API Gateway 路由

通過 API Gateway 統一路由：

```yaml
# YARP 配置示例
routes:
  bidding:
    clusterId: bidding-cluster
    match:
      path: "/api/bids/{**remainder}"
    transforms:
      - pathPattern: "/api/bids/{**remainder}"

clusters:
  bidding-cluster:
    destinations:
      - address: "http://biddingservice"
```

## 實現 BiddingService

當準備實現 BiddingService 時，請遵循以下規範：

### 1. API 端點實現

**必需端點**:
```csharp
// 獲取當前出價
[HttpGet("api/auctions/{auctionId}/current-bid")]
public async Task<IActionResult> GetCurrentBid(Guid auctionId)

// 檢查是否有出價
[HttpGet("api/auctions/{auctionId}/has-bids")]
public async Task<IActionResult> CheckAuctionHasBids(Guid auctionId)
```

**響應格式**:
所有端點應返回統一的 `ApiResponse<T>` 格式，與 AuctionService 保持一致。

### 2. 資料模型

```csharp
public class Bid
{
    public Guid Id { get; set; }
    public Guid AuctionId { get; set; }
    public string UserId { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### 3. 業務邏輯

- 出價驗證 (金額必須高於當前最高價)
- 競拍狀態檢查
- 用戶餘額驗證
- 並發控制 (防止同時出價衝突)

### 4. 測試建議

- 實現合約測試以驗證與 AuctionService 的集成
- 添加負載測試以驗證並發出價處理
- 實現故障注入測試以驗證彈性模式

## 故障排除

### 常見問題

1. **BiddingService 連線超時**
   - 檢查網路連線
   - 驗證服務發現配置
   - 查看電路斷路器狀態

2. **出價數據不一致**
   - 檢查兩服務間的時鐘同步
   - 驗證 API 契約版本
   - 查看相關日誌

3. **性能問題**
   - 監控 BiddingService 響應時間
   - 調整重試和超時設定
   - 考慮添加快取機制

### 調試技巧

1. 使用相關 ID 追蹤請求
2. 查看詳細的 HTTP 日誌
3. 檢查健康檢查端點狀態
4. 監控電路斷路器指標

## 總結

AuctionService 與 BiddingService 的集成採用了多種彈性模式，確保系統在部分故障時仍能正常運行。通過適當的錯誤處理和降級機制，提供了良好的用戶體驗和系統可靠性。</content>
<parameter name="filePath">c:\Users\peter\Desktop\project\AuctionService\AuctionService\docs\bidding-integration.md