# AuctionService API 使用指南

## 概述

AuctionService 提供完整的商品拍賣 REST API，支援商品的瀏覽、搜尋、建立、管理以及使用者追蹤功能。本指南包含 API 使用範例、認證說明以及常見使用情境。

## API 基礎資訊

### 基礎 URL
- **開發環境**: `http://localhost:5000`
- **生產環境**: `https://api.auctionservice.com`

### 認證
AuctionService 使用 JWT (JSON Web Token) Bearer 認證。

#### 取得認證 Token
```bash
# 向認證服務請求 token (範例)
curl -X POST https://auth.auctionservice.com/token \
  -H "Content-Type: application/json" \
  -d '{
    "username": "user@example.com",
    "password": "password123"
  }'
```

#### 使用 Token
```bash
curl -H "Authorization: Bearer YOUR_JWT_TOKEN" \
     https://api.auctionservice.com/api/auctions
```

### 回應格式
所有 API 回應都遵循統一格式：

```json
{
  "success": true,
  "data": { ... },
  "metadata": {
    "statusCode": "SUCCESS",
    "statusName": "成功",
    "message": "操作成功",
    "timestamp": "2025-12-10T10:00:00Z",
    "correlationId": "550e8400-e29b-41d4-a716-446655440000"
  }
}
```

### 錯誤處理
```json
{
  "success": false,
  "data": null,
  "metadata": {
    "statusCode": "AUCTION_NOT_FOUND",
    "statusName": "商品不存在",
    "message": "找不到指定的商品",
    "timestamp": "2025-12-10T10:00:00Z",
    "correlationId": "550e8400-e29b-41d4-a716-446655440000"
  }
}
```

### 分頁
支援分頁的 API 會在回應中包含分頁資訊：

```json
{
  "success": true,
  "data": {
    "items": [ ... ],
    "pageNumber": 1,
    "pageSize": 20,
    "totalCount": 150,
    "totalPages": 8,
    "hasNextPage": true,
    "hasPreviousPage": false
  },
  "metadata": { ... }
}
```

## API 端點

### 商品管理

#### 1. 查詢商品清單
**GET** `/api/auctions`

查詢所有進行中的拍賣商品，支援分頁、篩選、搜尋和排序。

**查詢參數**:
- `page` (int): 頁碼 (預設: 1)
- `pageSize` (int): 每頁筆數 (預設: 20, 最大: 100)
- `status` (string): 商品狀態 (Active/Pending/Ended)
- `categoryId` (int): 分類 ID
- `search` (string): 關鍵字搜尋 (商品名稱或描述)
- `sortBy` (string): 排序欄位 (EndTime/StartingPrice/CreatedAt)
- `sortOrder` (string): 排序方向 (asc/desc)

**範例請求**:
```bash
# 查詢第一頁，每頁 10 筆，狀態為進行中
curl "http://localhost:5000/api/auctions?page=1&pageSize=10&status=Active"

# 搜尋包含 "iPhone" 的商品
curl "http://localhost:5000/api/auctions?search=iPhone"

# 查詢電子產品分類，按結束時間排序
curl "http://localhost:5000/api/auctions?categoryId=1&sortBy=EndTime&sortOrder=asc"
```

**回應範例**:
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "550e8400-e29b-41d4-a716-446655440000",
        "name": "iPhone 15 Pro",
        "startingPrice": 35000,
        "categoryId": 1,
        "categoryName": "電子產品",
        "status": "Active",
        "endTime": "2025-12-15T10:00:00Z",
        "createdAt": "2025-12-10T09:00:00Z"
      }
    ],
    "pageNumber": 1,
    "pageSize": 10,
    "totalCount": 25,
    "totalPages": 3,
    "hasNextPage": true,
    "hasPreviousPage": false
  },
  "metadata": {
    "statusCode": "SUCCESS",
    "statusName": "成功",
    "message": "操作成功"
  }
}
```

#### 2. 取得商品詳細資訊
**GET** `/api/auctions/{id}`

取得指定商品的詳細資訊。

**路徑參數**:
- `id` (string): 商品 GUID

**範例請求**:
```bash
curl http://localhost:5000/api/auctions/550e8400-e29b-41d4-a716-446655440000
```

#### 3. 取得商品目前出價
**GET** `/api/auctions/{id}/current-bid`

取得商品的目前出價資訊。

**範例請求**:
```bash
curl http://localhost:5000/api/auctions/550e8400-e29b-41d4-a716-446655440000/current-bid
```

**回應範例**:
```json
{
  "success": true,
  "data": {
    "auctionId": "550e8400-e29b-41d4-a716-446655440000",
    "currentBid": 38000,
    "bidCount": 15,
    "highestBidderUserId": "user-123"
  },
  "metadata": { ... }
}
```

#### 4. 建立新商品
**POST** `/api/auctions`

建立新的拍賣商品（需要賣家權限）。

**請求標頭**:
- `Authorization: Bearer {token}`
- `Content-Type: application/json`

**請求本文**:
```json
{
  "name": "MacBook Pro M3",
  "description": "全新的 MacBook Pro 14 吋，M3 晶片",
  "startingPrice": 45000,
  "categoryId": 1,
  "startTime": "2025-12-10T10:00:00Z",
  "endTime": "2025-12-17T10:00:00Z"
}
```

**範例請求**:
```bash
curl -X POST http://localhost:5000/api/auctions \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "MacBook Pro M3",
    "description": "全新的 MacBook Pro 14 吋，M3 晶片",
    "startingPrice": 45000,
    "categoryId": 1,
    "startTime": "2025-12-10T10:00:00Z",
    "endTime": "2025-12-17T10:00:00Z"
  }'
```

#### 5. 更新商品資訊
**PUT** `/api/auctions/{id}`

更新現有商品的資訊（需要商品擁有者權限，且商品未結束）。

**範例請求**:
```bash
curl -X PUT http://localhost:5000/api/auctions/550e8400-e29b-41d4-a716-446655440000 \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "MacBook Pro M3 (更新)",
    "description": "全新的 MacBook Pro 14 吋，M3 晶片 - 含原廠保固",
    "startingPrice": 46000,
    "endTime": "2025-12-18T10:00:00Z"
  }'
```

#### 6. 刪除商品
**DELETE** `/api/auctions/{id}`

刪除指定的商品（需要商品擁有者權限，且商品未結束且無出價記錄）。

**範例請求**:
```bash
curl -X DELETE http://localhost:5000/api/auctions/550e8400-e29b-41d4-a716-446655440000 \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

#### 7. 查詢使用者商品
**GET** `/api/auctions/user/{userId}`

查詢指定使用者建立的所有商品。

**範例請求**:
```bash
curl http://localhost:5000/api/auctions/user/user-123
```

### 分類管理

#### 取得所有分類
**GET** `/api/categories`

取得所有啟用的商品分類。

**範例請求**:
```bash
curl http://localhost:5000/api/categories
```

**回應範例**:
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "name": "電子產品",
      "displayOrder": 1
    },
    {
      "id": 2,
      "name": "家具",
      "displayOrder": 2
    }
  ],
  "metadata": { ... }
}
```

### 追蹤管理

#### 1. 追蹤商品
**POST** `/api/follows`

開始追蹤指定的商品。

**請求本文**:
```json
{
  "auctionId": "550e8400-e29b-41d4-a716-446655440000"
}
```

#### 2. 取消追蹤
**DELETE** `/api/follows/{auctionId}`

取消追蹤指定的商品。

#### 3. 查詢追蹤清單
**GET** `/api/follows`

取得目前使用者追蹤的所有商品。

### 健康檢查

#### 系統健康狀態
**GET** `/api/health`

檢查系統整體健康狀態，包括資料庫連線和外部服務可用性。

**範例請求**:
```bash
curl http://localhost:5000/api/health
```

**回應範例**:
```json
{
  "status": "healthy",
  "timestamp": "2025-12-10T10:00:00Z",
  "service": "AuctionService",
  "checks": [
    {
      "name": "database",
      "status": "healthy",
      "timestamp": "2025-12-10T10:00:00Z",
      "details": "Database connection successful"
    },
    {
      "name": "bidding-service",
      "status": "healthy",
      "timestamp": "2025-12-10T10:00:00Z",
      "details": "BiddingService is available"
    }
  ]
}
```

## 使用情境範例

### 情境 1: 買家瀏覽商品

```bash
# 1. 取得分類清單
curl http://localhost:5000/api/categories

# 2. 瀏覽電子產品分類的商品
curl "http://localhost:5000/api/auctions?categoryId=1&status=Active&pageSize=20"

# 3. 搜尋特定商品
curl "http://localhost:5000/api/auctions?search=iPhone"

# 4. 查看商品詳細資訊和目前出價
curl http://localhost:5000/api/auctions/550e8400-e29b-41d4-a716-446655440000
curl http://localhost:5000/api/auctions/550e8400-e29b-41d4-a716-446655440000/current-bid

# 5. 追蹤感興趣的商品
curl -X POST http://localhost:5000/api/follows \
  -H "Authorization: Bearer BUYER_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"auctionId": "550e8400-e29b-41d4-a716-446655440000"}'
```

### 情境 2: 賣家管理商品

```bash
# 1. 建立新商品
curl -X POST http://localhost:5000/api/auctions \
  -H "Authorization: Bearer SELLER_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "iPad Air 5",
    "description": "全新的 iPad Air 5，64GB WiFi",
    "startingPrice": 15000,
    "categoryId": 1,
    "startTime": "2025-12-10T10:00:00Z",
    "endTime": "2025-12-20T10:00:00Z"
  }'

# 2. 查看自己的商品
curl -H "Authorization: Bearer SELLER_TOKEN" \
     http://localhost:5000/api/auctions/user/seller-123

# 3. 更新商品資訊（在商品結束前）
curl -X PUT http://localhost:5000/api/auctions/550e8400-e29b-41d4-a716-446655440000 \
  -H "Authorization: Bearer SELLER_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "iPad Air 5 (特價)",
    "description": "全新的 iPad Air 5，64GB WiFi - 限時特價",
    "startingPrice": 14000,
    "endTime": "2025-12-18T10:00:00Z"
  }'

# 4. 刪除不需要的商品（無出價記錄）
curl -X DELETE http://localhost:5000/api/auctions/550e8400-e29b-41d4-a716-446655440000 \
  -H "Authorization: Bearer SELLER_TOKEN"
```

## 錯誤處理

### 常見錯誤代碼

| 狀態代碼 | 說明 | 處理方式 |
|---------|------|---------|
| `AUCTION_NOT_FOUND` | 商品不存在 | 檢查商品 ID 是否正確 |
| `UNAUTHORIZED` | 未授權訪問 | 確認 JWT token 是否有效 |
| `VALIDATION_ERROR` | 請求資料驗證失敗 | 檢查請求參數格式 |
| `AUCTION_ENDED` | 商品已結束 | 無法修改已結束的商品 |
| `INSUFFICIENT_PERMISSIONS` | 權限不足 | 確認使用者身份和權限 |

### 錯誤回應範例

```json
{
  "success": false,
  "data": null,
  "metadata": {
    "statusCode": "AUCTION_NOT_FOUND",
    "statusName": "商品不存在",
    "message": "找不到 ID 為 550e8400-e29b-41d4-a716-446655440000 的商品",
    "timestamp": "2025-12-10T10:00:00Z",
    "correlationId": "550e8400-e29b-41d4-a716-446655440000"
  }
}
```

## 效能考量

### 分頁最佳實務
- 使用適當的 `pageSize` (建議 20-50)
- 實作無限滾動時注意記憶體使用
- 監控資料庫查詢效能

### 快取策略
- 分類資料通常不常變更，可考慮前端快取
- 商品清單可根據查詢條件實作短期快取
- 出價資訊為動態資料，不建議快取

### 並發處理
- 商品建立和更新操作需要注意並發控制
- 多個使用者同時出價時的競態條件處理
- 資料庫鎖定和交易隔離等級設定

## SDK 和工具

### OpenAPI 規範
完整的 API 規範可透過以下端點取得：
- **Swagger UI**: `http://localhost:5000/swagger`
- **Scalar UI**: `http://localhost:5000/scalar/v1`
- **OpenAPI JSON**: `http://localhost:5000/openapi/v1.json`

### 測試工具
```bash
# 使用 curl 進行 API 測試
# 使用 Postman 或 Insomnia 進行圖形化測試
# 使用 Newman 進行自動化測試
```

### 程式碼產生
```bash
# 使用 OpenAPI Generator 產生客戶端 SDK
npx openapi-generator-cli generate \
  -i http://localhost:5000/openapi/v1.json \
  -g csharp \
  -o ./generated-client
```

## 支援與聯絡

如有 API 使用問題或功能請求，請聯絡：
- **技術支援**: support@auctionservice.com
- **開發團隊**: dev@auctionservice.com
- **文件更新**: docs@auctionservice.com