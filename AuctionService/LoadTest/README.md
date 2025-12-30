# AuctionService Load Test

使用 NBomber 6.1.0 進行 AuctionService 的壓力測試。

## 前置條件

1. **AuctionService API 必須正在運行**
   - 預設 URL: `http://localhost:5106`
   - 可透過環境變數 `AUCTION_SERVICE_URL` 自訂

2. **資料庫需要有測試資料**
   - 建議先執行應用程式的 seed 功能

## 快速開始

### 方法 1：使用自動化腳本（推薦）

```powershell
# 在 LoadTest 目錄下執行
.\run-loadtest.ps1
```

腳本會自動：
1. 檢查 API 是否運行
2. 如果沒有運行，提供選項啟動 API
3. 執行壓力測試

### 方法 2：手動執行

#### 步驟 1：啟動 AuctionService API

在第一個終端機視窗：
```powershell
cd ..\..\src\AuctionService.Api
dotnet run
```

等待看到 "Now listening on: http://localhost:5106"

#### 步驟 2：執行壓力測試

在第二個終端機視窗：
```powershell
cd AuctionService\LoadTest
dotnet run
```

## 查看測試結果

測試完成後，報告會自動生成在 `./reports/` 目錄：

### 打開 HTML 報告
```powershell
# 在 LoadTest 目錄下
start ./reports/auction_load_test_[timestamp].html
```

### 查看文字報告
```powershell
cat ./reports/auction_load_test_[timestamp].txt
```

報告格式：
- **HTML 報告**: 視覺化圖表和詳細統計
- **TXT 報告**: 純文字摘要
- **CSV 數據**: 原始數據用於進一步分析

## 測試場景

### 執行的場景

1. **Auction List** - 拍賣列表查詢
   - 目標：50 RPS，60秒
   - P95 延遲目標：≤200ms

2. **Auction Detail** - 拍賣詳情查詢
   - 目標：150 RPS，60秒
   - P95 延遲目標：≤150ms

3. **Current Bid** - 當前出價查詢
   - 目標：200 RPS，120秒
   - P95 延遲目標：≤100ms

4. **Hot Auction** - 熱門拍賣壓力測試
   - 目標：1000 RPS，60秒
   - P95 延遲目標：≤50ms

### 可選場景（需要認證）

以下場景預設為註解狀態，需要時可在 `Program.cs` 中啟用：

- **Follow Operations** - 關注操作
- **User Auctions** - 用戶拍賣列表

## 測試報告

測試完成後會生成以下報告：

- **HTML 報告**: `./reports/auction_load_test_[timestamp].html`
- **TXT 報告**: `./reports/auction_load_test_[timestamp].txt`
- **CSV 數據**: `./reports/auction_load_test_[timestamp].csv`

## 自訂配置

### 修改目標 URL

```powershell
$env:AUCTION_SERVICE_URL = "http://your-server:port"
dotnet run
```

### 選擇要執行的場景

編輯 `Program.cs`，註解掉不需要的場景：

```csharp
// 1. 基礎場景（建議保留）
scenarios.Add(new AuctionListLoadTest(baseUrl, metricsCollector).CreateScenario());
scenarios.Add(new AuctionDetailLoadTest(baseUrl, metricsCollector, auctionIds).CreateScenario());

// 2. 高壓場景（可選）
// scenarios.Add(new HotAuctionLoadTest(baseUrl, metricsCollector, hotAuctionId).CreateScenario());

// 3. 需要認證的場景（預設關閉）
// scenarios.Add(new FollowLoadTest(baseUrl, metricsCollector, userTokens, auctionIds).CreateScenario());
```

### 調整負載參數

編輯 `Scenarios/` 目錄下的各個測試場景檔案，修改：

```csharp
.WithLoadSimulations(
    Simulation.Inject(
        rate: 50,                              // 調整：每秒請求數
        interval: TimeSpan.FromSeconds(1),     // 注入間隔
        during: TimeSpan.FromSeconds(60)       // 調整：測試持續時間
    )
)
```

## 效能指標說明

### 關鍵指標

- **P95 Latency**: 95% 的請求延遲時間
- **RPS**: 每秒請求數 (Requests Per Second)
- **Success Rate**: 成功請求的百分比

### 效能目標

| 場景 | RPS 目標 | P95 延遲 | 成功率 |
|------|---------|---------|--------|
| Auction List | ≥100 | ≤200ms | ≥99.5% |
| Auction Detail | ≥150 | ≤150ms | ≥99.9% |
| Current Bid | ≥200 | ≤100ms | ≥99.9% |
| Follow Operations | ≥50 | ≤300ms | ≥99.0% |
| Hot Auction | ≥1000 | ≤50ms | ≥99.9% |
| User Auctions | ≥25 | ≤250ms | ≥99.5% |

## 常見問題

### Q: 測試失敗，顯示連接錯誤

**A**: 確認 AuctionService API 正在運行：
```powershell
curl http://localhost:5106/health
```

### Q: 測試結果不理想

**A**: 可能原因：
1. 資料庫沒有足夠的測試資料
2. API 服務資源不足
3. 網路延遲問題

建議先從較小的負載開始測試。

### Q: 如何只測試特定場景？

**A**: 編輯 `Program.cs`，只保留需要的場景，註解其他。

### Q: 報告在哪裡？

**A**: 所有報告都在 `./reports/` 目錄，檔名包含時間戳記。

## 進階使用

### 自訂環境變數

```powershell
# 自訂 API URL
$env:AUCTION_SERVICE_URL = "http://localhost:8080"
dotnet run
```

### 分散式負載測試

NBomber 支援分散式測試，詳見 [NBomber 文檔](https://nbomber.com/docs/nbomber/cluster)

## 專案結構

```
LoadTest/
├── Program.cs                    # 主程式
├── MetricsCollector.cs          # 指標收集器
├── TestDataSeeder.cs            # 測試數據生成器（可選）
├── Scenarios/                   # 測試場景
│   ├── AuctionListLoadTest.cs
│   ├── AuctionDetailLoadTest.cs
│   ├── CurrentBidLoadTest.cs
│   ├── FollowLoadTest.cs
│   ├── HotAuctionLoadTest.cs
│   └── UserAuctionsLoadTest.cs
└── reports/                     # 生成的報告（執行後）
```

## 延伸資訊

- [NBomber 官方文檔](https://nbomber.com)
- [NBomber GitHub](https://github.com/PragmaticFlow/NBomber)
