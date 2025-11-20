# Quick Start Guide: Auction Service

**Branch**: `002-auction-service`  
**Date**: 2025-11-20  
**Purpose**: Developer onboarding and local setup guide

---

## Overview

本指南協助開發者快速建立本地開發環境，並驗證 Auction Service 的基本功能。

---

## Prerequisites

### Required Software

- **.NET 9 SDK** - [下載連結](https://dotnet.microsoft.com/download/dotnet/9.0)
- **PostgreSQL 16** - 資料庫（本地安裝或 Docker）
- **Redis 7** - 分散式快取（本地安裝或 Docker）
- **Docker Desktop** (可選) - 容器化部署
- **Git** - 版本控制

### Optional Tools

- **Visual Studio 2022** 或 **VS Code** - IDE
- **Postman** 或 **curl** - API 測試
- **Azure CLI** 或 **AWS CLI** - 雲端儲存配置（若使用雲端物件儲存）
- **MinIO Client** - 本地物件儲存管理（若使用 MinIO）

---

## Environment Setup

### 1. Clone Repository

```bash
git clone https://github.com/tsengpeter/AuctionService.git
cd AuctionService
git checkout 002-auction-service
```

---

### 2. Database Setup

#### Option A: Docker PostgreSQL (推薦)

```bash
docker run -d --name auctionservice-db \
  -e POSTGRES_USER=auctionservice \
  -e POSTGRES_PASSWORD=Dev@Password123 \
  -e POSTGRES_DB=auctionservice_dev \
  -p 5432:5432 postgres:16-alpine
```

#### Option B: Local PostgreSQL Installation

1. 安裝 PostgreSQL 16
2. 建立資料庫：
   ```sql
   CREATE DATABASE auctionservice_dev;
   CREATE USER auctionservice WITH PASSWORD 'Dev@Password123';
   GRANT ALL PRIVILEGES ON DATABASE auctionservice_dev TO auctionservice;
   ```

#### Connection String

```bash
# appsettings.Development.json 或環境變數
DB_CONNECTION_STRING="Host=localhost;Port=5432;Database=auctionservice_dev;Username=auctionservice;Password=Dev@Password123"
```

---

### 3. Redis Setup

#### Option A: Docker Redis (推薦)

```bash
docker run -d --name auctionservice-redis \
  -p 6379:6379 redis:7-alpine
```

#### Option B: Local Redis Installation

1. 安裝 Redis 7
2. 啟動 Redis Server：
   ```bash
   redis-server
   ```

#### Connection String

```bash
# appsettings.Development.json 或環境變數
REDIS_CONNECTION_STRING="localhost:6379"
```

---

### 4. Object Storage Setup

#### Option A: MinIO (本地開發推薦)

```bash
# 啟動 MinIO
docker run -d --name auctionservice-minio \
  -p 9000:9000 -p 9001:9001 \
  -e MINIO_ROOT_USER=minioadmin \
  -e MINIO_ROOT_PASSWORD=minioadmin \
  quay.io/minio/minio server /data --console-address ":9001"

# 建立 Bucket
docker exec auctionservice-minio mkdir -p /data/auction-images
docker exec auctionservice-minio mc alias set local http://localhost:9000 minioadmin minioadmin
docker exec auctionservice-minio mc mb local/auction-images
docker exec auctionservice-minio mc policy set public local/auction-images
```

#### Option B: AWS S3

```bash
# 設定 AWS 憑證
aws configure

# appsettings.Development.json
{
  "AWS": {
    "Region": "ap-northeast-1",
    "S3": {
      "BucketName": "your-auction-images-bucket"
    }
  }
}
```

#### Option C: Azure Blob Storage

```bash
# appsettings.Development.json
{
  "Azure": {
    "Storage": {
      "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=youraccountname;AccountKey=youraccountkey;EndpointSuffix=core.windows.net",
      "ContainerName": "auction-images"
    }
  }
}
```

---

### 5. Code-First Database Migration

#### Step 1: Restore NuGet Packages

```bash
cd src/AuctionService
dotnet restore
```

#### Step 2: Install EF Core Tools

```bash
dotnet tool install --global dotnet-ef --version 9.0.0
```

#### Step 3: Create Initial Migration

```bash
# 在專案根目錄執行
dotnet ef migrations add InitialCreate \
  --project src/AuctionService/AuctionService.Infrastructure \
  --startup-project src/AuctionService/AuctionService.API \
  --output-dir Persistence/Migrations
```

#### Step 4: Apply Migration to Database

```bash
dotnet ef database update \
  --project src/AuctionService/AuctionService.Infrastructure \
  --startup-project src/AuctionService/AuctionService.API
```

#### Verification

```bash
# 連線至資料庫驗證 schema
docker exec -it auctionservice-db psql -U auctionservice -d auctionservice_dev -c "\dt"

# 預期輸出：
#              List of relations
#  Schema |      Name       | Type  |     Owner      
# --------+-----------------+-------+----------------
#  public | Auctions        | table | auctionservice
#  public | Categories      | table | auctionservice
#  public | Follows         | table | auctionservice
#  public | ResponseCodes   | table | auctionservice
```

---

### 6. Environment Variables Configuration

建立 `appsettings.Development.json`：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=auctionservice_dev;Username=auctionservice;Password=Dev@Password123"
  },
  "Redis": {
    "ConnectionString": "localhost:6379",
    "InstanceName": "AuctionService:"
  },
  "Snowflake": {
    "WorkerId": 2,
    "DatacenterId": 0
  },
  "BiddingService": {
    "BaseUrl": "http://localhost:5003",
    "Timeout": "00:00:03"
  },
  "ObjectStorage": {
    "Provider": "MinIO",
    "MinIO": {
      "Endpoint": "localhost:9000",
      "AccessKey": "minioadmin",
      "SecretKey": "minioadmin",
      "BucketName": "auction-images",
      "UseSSL": false
    }
  },
  "JWT": {
    "SecretKey": "your-super-secret-jwt-key-at-least-32-characters-long",
    "Issuer": "AuctionService",
    "Audience": "AuctionServiceUsers",
    "ExpiryMinutes": 15
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/auction-service-.log",
          "rollingInterval": "Day"
        }
      }
    ]
  }
}
```

---

## Build & Run

### Build Solution

```bash
cd src/AuctionService
dotnet build
```

### Run API

```bash
cd src/AuctionService/AuctionService.API
dotnet run
```

預期輸出：

```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5002
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

---

## Testing

### 1. Health Check

```bash
curl http://localhost:5002/health
```

預期回應：

```json
{
  "status": "Healthy",
  "timestamp": "2025-11-20T10:30:00Z"
}
```

### 2. Readiness Check

```bash
curl http://localhost:5002/ready
```

預期回應：

```json
{
  "status": "Ready",
  "database": "Connected",
  "redis": "Connected",
  "timestamp": "2025-11-20T10:30:00Z"
}
```

### 3. Get Categories

```bash
curl http://localhost:5002/api/categories
```

預期回應：

```json
{
  "data": [
    { "id": 1, "name": "電子產品", "displayOrder": 1, "isEnabled": true },
    { "id": 2, "name": "家具", "displayOrder": 2, "isEnabled": true },
    { "id": 3, "name": "收藏品", "displayOrder": 3, "isEnabled": true },
    { "id": 4, "name": "服飾", "displayOrder": 4, "isEnabled": true },
    { "id": 5, "name": "書籍", "displayOrder": 5, "isEnabled": true },
    { "id": 6, "name": "其他", "displayOrder": 99, "isEnabled": true }
  ],
  "metadata": {
    "statusCode": "SUCCESS",
    "statusName": "Success",
    "message": "操作成功"
  }
}
```

### 4. Create Auction (需要 JWT Token)

首先從 Member Service 取得 JWT Token，然後：

```bash
curl -X POST http://localhost:5002/api/auctions \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "iPhone 15 Pro Max",
    "description": "全新未拆封，256GB 鈦金屬色",
    "startingPrice": 30000,
    "categoryId": 1,
    "endTime": "2025-11-21T18:00:00Z"
  }'
```

### 5. Get Auctions List

```bash
curl "http://localhost:5002/api/auctions?pageIndex=0&pageSize=20&categoryId=1"
```

### 6. Search Auctions

```bash
curl "http://localhost:5002/api/auctions?keyword=iPhone"
```

---

## Run Tests

### Unit Tests

```bash
cd tests/AuctionService.Domain.Tests
dotnet test

cd ../AuctionService.Application.Tests
dotnet test

cd ../AuctionService.Infrastructure.Tests
dotnet test
```

### Integration Tests

```bash
cd tests/AuctionService.IntegrationTests
dotnet test
```

### Test Coverage Report

```bash
cd tests
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:CoverletOutput=../coverage/
```

---

## Database Management

### View Current Schema

```bash
# 連線至資料庫
docker exec -it auctionservice-db psql -U auctionservice -d auctionservice_dev

# 列出所有資料表
\dt

# 查看特定資料表結構
\d "Auctions"

# 查詢商品資料
SELECT "Id", "Title", "Status" FROM "Auctions";

# 離開
\q
```

### Create New Migration

```bash
# 修改 Entity 後，建立新的 Migration
dotnet ef migrations add AddNewFeature \
  --project src/AuctionService/AuctionService.Infrastructure \
  --startup-project src/AuctionService/AuctionService.API

# 套用 Migration
dotnet ef database update \
  --project src/AuctionService/AuctionService.Infrastructure \
  --startup-project src/AuctionService/AuctionService.API
```

### Rollback Migration

```bash
# 回滾至特定 Migration
dotnet ef database update PreviousMigrationName \
  --project src/AuctionService/AuctionService.Infrastructure \
  --startup-project src/AuctionService/AuctionService.API

# 移除最後一個 Migration（尚未套用至資料庫）
dotnet ef migrations remove \
  --project src/AuctionService/AuctionService.Infrastructure \
  --startup-project src/AuctionService/AuctionService.API
```

### Reset Database

```bash
# 刪除資料庫
dotnet ef database drop --force \
  --project src/AuctionService/AuctionService.Infrastructure \
  --startup-project src/AuctionService/AuctionService.API

# 重新建立
dotnet ef database update \
  --project src/AuctionService/AuctionService.Infrastructure \
  --startup-project src/AuctionService/AuctionService.API
```

---

## Redis Management

### Connect to Redis CLI

```bash
docker exec -it auctionservice-redis redis-cli
```

### View Cached Bids

```bash
# 列出所有 keys
KEYS AuctionService:bid:*

# 查看特定商品的快取出價
GET AuctionService:bid:123456789

# 刪除快取
DEL AuctionService:bid:123456789

# 離開
exit
```

---

## MinIO Management

### Access MinIO Console

開啟瀏覽器訪問：http://localhost:9001

- **Username**: minioadmin
- **Password**: minioadmin

### Upload Test Image

```bash
# 使用 MinIO Client
mc alias set local http://localhost:9000 minioadmin minioadmin
mc cp test-image.jpg local/auction-images/auctions/123/test-image.jpg
```

---

## Troubleshooting

### 1. Database Connection Failed

**問題**：`Npgsql.NpgsqlException: Connection refused`

**解決方案**：
```bash
# 檢查 PostgreSQL 是否運行
docker ps | grep auctionservice-db

# 檢查連線字串是否正確
# 確認 Host、Port、Username、Password
```

---

### 2. Redis Connection Failed

**問題**：`StackExchange.Redis.RedisConnectionException: No connection`

**解決方案**：
```bash
# 檢查 Redis 是否運行
docker ps | grep auctionservice-redis

# 測試連線
redis-cli -h localhost -p 6379 ping
# 預期輸出：PONG
```

---

### 3. Migration Failed

**問題**：`PostgreSQL error: relation "Auctions" already exists`

**解決方案**：
```bash
# 選項 1: 刪除並重新建立資料庫
dotnet ef database drop --force
dotnet ef database update

# 選項 2: 手動刪除資料表
docker exec -it auctionservice-db psql -U auctionservice -d auctionservice_dev
DROP TABLE "Auctions", "Categories", "Follows", "ResponseCodes" CASCADE;
\q
dotnet ef database update
```

---

### 4. Bidding Service Unavailable

**問題**：API 回應 `BIDDING_SERVICE_UNAVAILABLE`，`dataSource: "cache"`

**說明**：這是正常的降級行為，Bidding Service 不可用時從快取讀取最後已知出價。

**解決方案**：
```bash
# 確認 Bidding Service 是否運行
curl http://localhost:5003/health

# 若 Bidding Service 正常但仍無法連線，檢查 appsettings.json
# "BiddingService": { "BaseUrl": "http://localhost:5003" }
```

---

### 5. Image Upload Failed

**問題**：`Unable to upload image to object storage`

**解決方案**：
```bash
# 檢查 MinIO 是否運行
docker ps | grep auctionservice-minio

# 檢查 Bucket 是否存在
docker exec auctionservice-minio mc ls local/

# 檢查 Bucket 權限
docker exec auctionservice-minio mc policy get local/auction-images
# 預期輸出：public
```

---

### 6. Full-Text Search Not Working

**問題**：搜尋關鍵字無法找到商品

**解決方案**：
```bash
# 檢查 SearchVector 是否正確建立
docker exec -it auctionservice-db psql -U auctionservice -d auctionservice_dev
SELECT "Id", "Title", "SearchVector" FROM "Auctions" LIMIT 5;

# 若 SearchVector 為空，手動觸發更新
UPDATE "Auctions" SET "Title" = "Title";

# 檢查 Trigger 是否存在
\df update_auction_search_vector
```

---

### 7. JWT Authentication Failed

**問題**：`401 Unauthorized` - JWT Token 驗證失敗

**解決方案**：
```bash
# 確認 JWT SecretKey 與 Member Service 一致
# 兩個服務必須使用相同的 JWT 設定

# 檢查 Token 是否過期（Access Token 有效期 15 分鐘）
# 使用 https://jwt.io/ 解碼 Token 檢查 exp (過期時間)

# 確認 Authorization Header 格式正確
# 正確格式：Authorization: Bearer <token>
```

---

## Production Deployment

### Environment Configuration

正式環境需調整以下設定：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=your-db-server.postgres.database.azure.com;Port=5432;Database=auctionservice_prod;Username=adminuser;Password=${PROD_DB_PASSWORD};SslMode=Require"
  },
  "Redis": {
    "ConnectionString": "your-redis-server.redis.cache.windows.net:6380,password=${PROD_REDIS_PASSWORD},ssl=True,abortConnect=False"
  },
  "Snowflake": {
    "WorkerId": 2,
    "DatacenterId": 1
  },
  "BiddingService": {
    "BaseUrl": "https://api.production.com/bidding-service",
    "Timeout": "00:00:03"
  },
  "ObjectStorage": {
    "Provider": "AWS",
    "AWS": {
      "Region": "ap-northeast-1",
      "S3": {
        "BucketName": "production-auction-images"
      }
    }
  }
}
```

### Database Migration in Production

```bash
# 使用 CI/CD Pipeline 自動執行 Migration
# 確保在部署前備份資料庫

# Azure DevOps / GitHub Actions 範例
- name: Run EF Core Migrations
  run: |
    dotnet ef database update --project src/AuctionService/AuctionService.Infrastructure --startup-project src/AuctionService/AuctionService.API --connection "${{ secrets.PROD_DB_CONNECTION }}"
```

---

## Common Tasks

### Add New Category

```sql
-- 連線至資料庫
docker exec -it auctionservice-db psql -U auctionservice -d auctionservice_dev

-- 新增分類（使用雪花ID）
INSERT INTO "Categories" ("Id", "Name", "DisplayOrder", "IsEnabled") 
VALUES (7, '運動用品', 7, TRUE);
```

### View Auction Status Distribution

```sql
-- 查詢各狀態的商品數量
SELECT 
  CASE 
    WHEN NOW() < "StartTime" THEN 'Pending'
    WHEN NOW() >= "StartTime" AND NOW() < "EndTime" THEN 'Active'
    ELSE 'Ended'
  END AS Status,
  COUNT(*) AS Count
FROM "Auctions"
GROUP BY Status;
```

### Test Full-Text Search

```sql
-- 測試搜尋功能
SELECT "Id", "Title", 
       ts_rank("SearchVector", plainto_tsquery('simple', 'iPhone')) AS rank
FROM "Auctions"
WHERE "SearchVector" @@ plainto_tsquery('simple', 'iPhone')
ORDER BY rank DESC
LIMIT 10;
```

---

## Development Workflow

1. **切換至 feature branch**
   ```bash
   git checkout -b feature/add-new-feature
   ```

2. **修改 Entity 或 Value Object**

3. **建立 Migration**
   ```bash
   dotnet ef migrations add AddNewFeature
   ```

4. **套用 Migration**
   ```bash
   dotnet ef database update
   ```

5. **執行測試**
   ```bash
   dotnet test
   ```

6. **提交變更**
   ```bash
   git add .
   git commit -m "feat: add new feature"
   git push origin feature/add-new-feature
   ```

---

## Next Steps

1. **閱讀 API 文件**: 參考 `contracts/openapi.yaml` 了解完整 API 規格
2. **執行整合測試**: `dotnet test tests/AuctionService.IntegrationTests`
3. **配置 YARP Gateway**: 設定 API Gateway 路由至 Auction Service
4. **整合 Member Service**: 設定 JWT 驗證與使用者識別
5. **整合 Bidding Service**: 設定 HTTP Client 呼叫出價 API

---

**Last Updated**: 2025-11-20  
**Author**: GitHub Copilot (Claude Sonnet 4.5)
