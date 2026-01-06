# API 指南

## 概述

BiddingService 提供 RESTful API 用於管理拍賣出價。所有端點都返回 JSON 響應並使用標準 HTTP 狀態碼。

## 基礎 URL
```
http://localhost:5000/api
```

## 身份驗證

API 使用 Bearer Token 身份驗證。Bidding Service 將 token 驗證委派給 **Member Service**。

1. 客戶端發送帶有 `Authorization: Bearer <token>` 的請求。
2. Bidding Service 將 token 轉發給 Member Service 進行驗證。
3. Member Service 返回結構化的驗證結果，包含：
   - `isValid`：布林值，表示 token 是否有效
   - `userId`：如果有效則為用戶 ID（無效則為 null）
   - `expiresAt`：token 到期時間
   - `errorMessage`：如果無效則為錯誤詳情
4. 如果有效，請求將使用已驗證的用戶 ID 繼續處理。
5. 如果無效，API 返回 `401 Unauthorized` 並包含錯誤詳情。

**必需的請求標頭**：
```
Authorization: Bearer <your-jwt-token>
```

## 通用響應格式

### 成功響應
```json
{
  "bidId": 1234567890123456789,
  "auctionId": 1,
  "bidderIdHash": "a1b2c3d4...",
  "amount": 150.00,
  "bidAt": "2024-01-15T10:30:00Z"
}
```

### 錯誤響應
```json
{
  "error": {
    "code": "AUCTION_NOT_FOUND",
    "message": "找不到 ID 為 999 的拍賣",
    "details": null
  }
}
```

## 端點

### 1. 提交出價

為拍賣提交新的出價。

**端點**: `POST /api/bids`

**請求主體**:
```json
{
  "auctionId": 1,
  "amount": 150.00
}
```

**響應**: `201 Created`
```json
{
  "bidId": 1234567890123456789,
  "auctionId": 1,
  "bidderIdHash": "a1b2c3d4...",
  "amount": 150.00,
  "bidAt": "2024-01-15T10:30:00Z"
}
```

**錯誤代碼**:
- `400 Bad Request`: 請求資料無效
- `404 Not Found`: 拍賣不存在
- `409 Conflict`: 拍賣未激活或出價金額太低

### 2. 根據 ID 獲取出價

根據出價 ID 檢索特定的出價。

**端點**: `GET /api/bids/{bidId}`

**響應**: `200 OK`
```json
{
  "bidId": 1234567890123456789,
  "auctionId": 1,
  "bidderIdHash": "a1b2c3d4...",
  "amount": 150.00,
  "bidAt": "2024-01-15T10:30:00Z"
}
```

**錯誤代碼**:
- `404 Not Found`: 出價不存在

### 3. 獲取出價歷史

檢索特定拍賣的出價歷史。

**端點**: `GET /api/bids/history/{auctionId}`

**查詢參數**:
- `page` (可選): 頁碼 (預設: 1)
- `pageSize` (可選): 每頁項目數 (預設: 50, 最大: 100)

**響應**: `200 OK`
```json
{
  "auctionId": 1,
  "bids": [
    {
      "bidId": 1234567890123456789,
      "auctionId": 1,
      "bidderIdHash": "a1b2c3d4...",
      "amount": 150.00,
      "bidAt": "2024-01-15T10:30:00Z"
    }
  ],
  "pagination": {
    "currentPage": 1,
    "pageSize": 50,
    "totalItems": 25,
    "totalPages": 1,
    "hasNext": false,
    "hasPrevious": false
  }
}
```

### 4. 獲取我的出價

檢索當前用戶提交的所有出價。

**端點**: `GET /api/bids/my-bids`

**查詢參數**:
- `page` (可選): 頁碼 (預設: 1)
- `pageSize` (可選): 每頁項目數 (預設: 50, 最大: 100)

**響應**: `200 OK`
```json
{
  "bids": [
    {
      "bidId": 1234567890123456789,
      "auctionId": 1,
      "bidderIdHash": "a1b2c3d4...",
      "amount": 150.00,
      "bidAt": "2024-01-15T10:30:00Z"
    }
  ],
  "pagination": {
    "currentPage": 1,
    "pageSize": 50,
    "totalItems": 5,
    "totalPages": 1,
    "hasNext": false,
    "hasPrevious": false
  }
}
```

### 5. 獲取最高出價

檢索拍賣的當前最高出價。

**端點**: `GET /api/bids/highest/{auctionId}`

**響應**: `200 OK`
```json
{
  "auctionId": 1,
  "highestBid": {
    "bidId": 1234567890123456789,
    "auctionId": 1,
    "bidderIdHash": "a1b2c3d4...",
    "amount": 200.00,
    "bidAt": "2024-01-15T10:45:00Z"
  }
}
```

**注意**: 如果沒有出價，`highestBid` 返回 `null`。

### 6. 獲取拍賣統計

檢索拍賣的綜合統計資訊。

**端點**: `GET /api/bids/auctions/{auctionId}/stats`

**響應**: `200 OK`
```json
{
  "auctionId": 1,
  "totalBids": 25,
  "uniqueBidders": 8,
  "startingPrice": 100.00,
  "currentHighestBid": 250.00,
  "averageBidAmount": 175.50,
  "priceGrowthRate": 150.00,
  "firstBidAt": "2024-01-15T09:00:00Z",
  "lastBidAt": "2024-01-15T11:30:00Z",
  "bidsInLastHour": 5,
  "bidsInLast24Hours": 25
}
```

## 速率限制

**注意**: 速率限制尚未實作。考慮根據以下條件實作：
- IP 地址
- 用戶 ID (當添加身份驗證時)
- 拍賣 ID

## 分頁

返回列表的端點支援分頁：

- 預設頁面大小: 50 項目
- 最大頁面大小: 100 項目
- 使用 `page` 和 `pageSize` 查詢參數

## 錯誤代碼

| 代碼 | HTTP 狀態 | 描述 |
|------|-------------|-------------|
| `UNAUTHORIZED` | 401 | 缺少或無效的 JWT token |
| `AUCTION_NOT_FOUND` | 404 | 拍賣不存在 |
| `AUCTION_NOT_ACTIVE` | 409 | 拍賣目前未激活 |
| `BID_AMOUNT_TOO_LOW` | 409 | 出價金額低於最低要求 |
| `DUPLICATE_BID` | 409 | 用戶已對此拍賣出價 |
| `BID_NOT_FOUND` | 404 | 特定出價不存在 |
| `VALIDATION_ERROR` | 400 | 請求資料驗證失敗 |

## 範例

### 提交出價
```bash
curl -X POST http://localhost:5000/api/bids \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "auctionId": 1,
    "amount": 150.00
  }'
```

### 獲取拍賣統計
```bash
curl http://localhost:5000/api/bids/auctions/1/stats
```

### 獲取出價歷史並分頁
```bash
curl "http://localhost:5000/api/bids/history/1?page=1&pageSize=20"
```

## SDK 和函式庫

**注意**: 尚未提供官方 SDK。請使用標準 HTTP 客戶端。

### JavaScript (fetch)
```javascript
const response = await fetch('/api/bids', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
  },
  body: JSON.stringify({
    auctionId: 1,
    amount: 150.00
  })
});

const result = await response.json();
```

### C# (HttpClient)
```csharp
using var client = new HttpClient();
client.BaseAddress = new Uri("http://localhost:5000");

var bidRequest = new { auctionId = 1, amount = 150.00m };
var response = await client.PostAsJsonAsync("/api/bids", bidRequest);
var bid = await response.Content.ReadFromJsonAsync<BidResponse>();
```

## Webhooks

**注意**: Webhooks 尚未實作。未來版本可能包含：
- 出價通知
- 拍賣即將結束提醒
- 被超過出價通知

## 版本控制

API 使用 URL 版本控制。目前版本為 v1 (基礎路徑中隱含)。

未來版本將在以下位置提供：
- `/api/v2/bids`
- `/api/v3/bids`