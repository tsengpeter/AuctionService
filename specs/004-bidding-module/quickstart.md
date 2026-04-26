# Quickstart: Bidding 模組

**Branch**: `004-bidding-module` | **Date**: 2026-04-27

---

## 環境需求

- Docker Desktop（執行 PostgreSQL + Redis）
- .NET 10 SDK
- EF Core CLI：`dotnet tool install --global dotnet-ef`

---

## 步驟 1：新增 Redis 至 docker-compose.yml

編輯 `docker-compose.yml`，在 `services:` 區塊加入：

```yaml
redis:
  image: redis:7-alpine
  container_name: auction_redis
  ports:
    - "6379:6379"
  restart: unless-stopped
```

啟動所有依賴：

```bash
docker compose up -d
```

驗證 Redis 已啟動：

```bash
docker compose ps
# auction_redis 應顯示 running
```

---

## 步驟 2：設定 Redis 連線字串

編輯 `src/AuctionService.Api/appsettings.Development.json`，加入：

```json
{
  "Redis": "localhost:6379"
}
```

---

## 步驟 3：新增 NuGet 套件

在 `src/Modules/Bidding/` 目錄執行：

```bash
cd src/Modules/Bidding
dotnet add package StackExchange.Redis
dotnet add package Microsoft.Extensions.Caching.StackExchangeRedis
dotnet add package Npgsql
dotnet add package FluentValidation.DependencyInjectionExtensions
```

---

## 步驟 4：執行 EF Migration

新增 `AddBidStatusAndBidder` migration：

```bash
dotnet ef migrations add AddBidStatusAndBidder \
  --project src/Modules/Bidding/Bidding.csproj \
  --startup-project src/AuctionService.Api/AuctionService.Api.csproj \
  --output-dir Infrastructure/Persistence/Migrations
```

套用 migration：

```bash
dotnet ef database update \
  --project src/Modules/Bidding/Bidding.csproj \
  --startup-project src/AuctionService.Api/AuctionService.Api.csproj
```

---

## 步驟 5：建置並啟動 API

```bash
dotnet build
dotnet run --project src/AuctionService.Api/AuctionService.Api.csproj
```

API 預設執行在 `https://localhost:7190`（依 launchSettings.json 而定）。

---

## 步驟 6：API 使用範例

### 6.1 取得 JWT Token（先登入）

```bash
curl -X POST https://localhost:7190/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username": "testuser", "password": "Test@1234"}'
```

將回應中的 `token` 儲存為環境變數：

```bash
TOKEN="<your_jwt_token>"
AUCTION_ID="<target_auction_id>"
```

### 6.2 出價

```bash
curl -X POST https://localhost:7190/api/auctions/$AUCTION_ID/bids \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"amount": 150}'
```

**預期回應（201）**：
```json
{
  "success": true,
  "statusCode": 201,
  "data": {
    "bidId": "3fa85f64-...",
    "auctionId": "...",
    "amount": 150,
    "placedAt": "2026-04-27T10:00:00Z",
    "status": "leading"
  }
}
```

### 6.3 查詢商品出價歷史

```bash
curl "https://localhost:7190/api/auctions/$AUCTION_ID/bids?page=1&pageSize=20"
```

### 6.4 查詢我的出價清單

```bash
curl "https://localhost:7190/api/bids/my?page=1&pageSize=20" \
  -H "Authorization: Bearer $TOKEN"
```

---

## 執行測試

```bash
# 全部測試
dotnet test

# 只執行 Bidding 相關測試
dotnet test --filter "FullyQualifiedName~Bidding"

# 只執行整合測試（需 Docker 執行中）
dotnet test tests/AuctionService.IntegrationTests/
```

---

## 常見問題

### Redis 連線失敗

確認 Redis container 已啟動：
```bash
docker compose ps
docker compose logs redis
```

確認連線字串設定正確（`appsettings.Development.json` 中的 `Redis` 欄位）。

### Migration 失敗：`Amount` 類型轉換錯誤

若 `bids` 表已有資料，`decimal → bigint` 轉型可能失敗（若有小數點值）。  
開發環境可先清空表再執行 migration：
```sql
TRUNCATE bidding.bids;
```

### 整合測試 Redis Testcontainer 失敗

確認 Docker Desktop 已執行，且 Testcontainers 版本支援 Redis：
```bash
dotnet list package | grep Testcontainers
# 需要 Testcontainers.Redis 4.x
```
