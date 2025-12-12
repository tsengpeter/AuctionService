# 快速開始指南：商品拍賣服務

**功能分支**: `002-auction-service`  
**建立日期**: 2025-12-02  
**版本**: 1.0.0

## 概述

本指南提供商品拍賣服務的快速安裝、設定與執行步驟，協助開發者在本地環境快速啟動專案。

---

## 前置需求

### 必要工具

| 工具 | 版本 | 說明 |
|------|------|------|
| .NET SDK | 10.0+ | [下載連結](https://dotnet.microsoft.com/download/dotnet/10.0) |
| PostgreSQL | 16.0+ | [下載連結](https://www.postgresql.org/download/) |
| Docker | 20.10+ | [下載連結](https://www.docker.com/products/docker-desktop) (可選) |
| Git | 2.30+ | [下載連結](https://git-scm.com/downloads) |

### 選用工具

| 工具 | 說明 |
|------|------|
| Visual Studio 2022 | 整合式開發環境 |
| JetBrains Rider | 跨平台 .NET IDE |
| VS Code | 輕量級編輯器 (需安裝 C# 擴充) |
| Postman / Insomnia | API 測試工具 |
| pgAdmin | PostgreSQL 管理工具 |

---

## 安裝步驟

### 方案 1: 使用 Docker Compose (推薦)

#### 1. Clone 專案

```bash
git clone https://github.com/your-org/AuctionService.git
cd AuctionService
git checkout 002-auction-service
```

#### 2. 啟動服務

```bash
# 啟動 PostgreSQL 與 API 服務
docker-compose up -d

# 查看服務狀態
docker-compose ps

# 查看日誌
docker-compose logs -f auction-api
```

#### 3. 驗證服務

```bash
# 健康檢查
curl http://localhost:5000/health

# 查詢商品清單
curl http://localhost:5000/api/auctions
```

#### 4. 停止服務

```bash
# 停止服務
docker-compose down

# 停止並刪除資料
docker-compose down -v
```

---

### 方案 2: 本地開發環境

#### 1. Clone 專案

```bash
git clone https://github.com/your-org/AuctionService.git
cd AuctionService
git checkout 002-auction-service
```

#### 2. 安裝 PostgreSQL

**Windows (使用 Chocolatey)**:
```powershell
choco install postgresql
```

**macOS (使用 Homebrew)**:
```bash
brew install postgresql@16
brew services start postgresql@16
```

**Linux (Ubuntu/Debian)**:
```bash
sudo apt update
sudo apt install postgresql-16
sudo systemctl start postgresql
```

#### 3. 建立資料庫

```bash
# 登入 PostgreSQL
psql -U postgres

# 建立資料庫與使用者
CREATE DATABASE auction_dev;
CREATE USER auction_user WITH PASSWORD 'auction_pass';
GRANT ALL PRIVILEGES ON DATABASE auction_dev TO auction_user;
\q
```

#### 4. 設定連線字串

編輯 `src/AuctionService.Api/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=auction_dev;Username=auction_user;Password=auction_pass"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

#### 5. 還原套件

```bash
cd src/AuctionService.Api
dotnet restore
```

#### 6. 執行 EF Core Migrations

```bash
# 建立初始 Migration
dotnet ef migrations add InitialCreate --project ../AuctionService.Infrastructure --startup-project .

# 套用 Migration 到資料庫
dotnet ef database update --project ../AuctionService.Infrastructure --startup-project .
```

#### 7. 執行專案

```bash
# 開發模式（從專案根目錄）
cd src/AuctionService.Api
dotnet run

# 或使用 watch 模式 (自動重新載入)
dotnet watch run
```

#### 8. 驗證服務

開啟瀏覽器訪問:
- **Swagger UI**: http://localhost:5000/swagger
- **健康檢查**: http://localhost:5000/health
- **API 端點**: http://localhost:5000/api/auctions

---

## 專案結構

```
AuctionService/                          # 單一專案根目錄
├── AuctionService.sln                   # Visual Studio 解決方案
├── README.md                            # 專案說明文件
├── .gitignore                           # Git 忽略設定
├── .editorconfig                        # 編輯器設定
├── docker-compose.yml                   # 本地開發環境
├── Dockerfile                           # API 容器映像
│
├── src/                                 # 原始碼資料夾
│   ├── AuctionService.Api/              # REST API Controllers (非 API Gateway)
│   │   ├── Controllers/                 # API 控制器
│   │   │   ├── AuctionsController.cs
│   │   │   ├── FollowsController.cs
│   │   │   └── CategoriesController.cs
│   │   ├── Middlewares/                 # 中介軟體
│   │   │   ├── ExceptionHandlingMiddleware.cs
│   │   │   ├── CorrelationIdMiddleware.cs
│   │   │   └── RequestLoggingMiddleware.cs
│   │   ├── Filters/                     # 過濾器
│   │   │   └── ValidationFilter.cs
│   │   ├── appsettings.json
│   │   ├── appsettings.Development.json
│   │   └── Program.cs
│   │
│   ├── AuctionService.Core/             # 核心業務邏輯
│   │   ├── Entities/                    # EF Core 實體
│   │   │   ├── Auction.cs
│   │   │   ├── Category.cs
│   │   │   ├── Follow.cs
│   │   │   └── ResponseCode.cs
│   │   ├── DTOs/                        # POCO DTOs
│   │   │   ├── Requests/
│   │   │   │   ├── CreateAuctionRequest.cs
│   │   │   │   ├── UpdateAuctionRequest.cs
│   │   │   │   └── FollowAuctionRequest.cs
│   │   │   └── Responses/
│   │   │       ├── AuctionListItemDto.cs
│   │   │       ├── AuctionDetailDto.cs
│   │   │       ├── CurrentBidDto.cs
│   │   │       ├── FollowDto.cs
│   │   │       └── CategoryDto.cs
│   │   ├── Interfaces/                  # 服務與儲存庫介面
│   │   │   ├── IRepository.cs
│   │   │   ├── IAuctionRepository.cs
│   │   │   ├── IFollowRepository.cs
│   │   │   ├── IAuctionService.cs
│   │   │   ├── IFollowService.cs
│   │   │   └── IBiddingServiceClient.cs
│   │   ├── Services/                    # 業務邏輯服務
│   │   │   ├── AuctionService.cs
│   │   │   ├── FollowService.cs
│   │   │   └── ResponseCodeService.cs
│   │   ├── Validators/                  # 驗證邏輯
│   │   │   └── AuctionValidator.cs
│   │   └── Exceptions/                  # 自訂例外
│   │       ├── AuctionNotFoundException.cs
│   │       ├── UnauthorizedException.cs
│   │       └── ValidationException.cs
│   │
│   ├── AuctionService.Infrastructure/   # 基礎設施層
│   │   ├── Data/                        # EF Core DbContext
│   │   │   ├── AuctionDbContext.cs
│   │   │   └── Configurations/          # Entity Configurations
│   │   │       ├── AuctionConfiguration.cs
│   │   │       ├── CategoryConfiguration.cs
│   │   │       ├── FollowConfiguration.cs
│   │   │       └── ResponseCodeConfiguration.cs
│   │   ├── Repositories/                # Repository 實作
│   │   │   ├── GenericRepository.cs
│   │   │   ├── AuctionRepository.cs
│   │   │   └── FollowRepository.cs
│   │   ├── HttpClients/                 # 外部服務客戶端
│   │   │   └── BiddingServiceClient.cs
│   │   ├── Migrations/                  # EF Core Migrations
│   │   └── Seed/                        # 初始資料種子
│   │       ├── CategorySeeder.cs
│   │       └── ResponseCodeSeeder.cs
│   │
│   └── AuctionService.Shared/           # 共用工具
│       ├── Constants/
│       │   └── ResponseCodes.cs
│       ├── Extensions/
│       │   ├── ServiceCollectionExtensions.cs
│       │   └── AuctionExtensions.cs
│       └── Helpers/
│           └── StatusCalculator.cs
│
├── tests/                               # 測試專案資料夾
│   ├── AuctionService.UnitTests/        # 單元測試
│   │   ├── Services/
│   │   ├── Controllers/
│   │   ├── Validators/
│   │   └── AuctionService.UnitTests.csproj
│   │
│   ├── AuctionService.IntegrationTests/ # 整合測試
│   │   ├── Controllers/
│   │   ├── Repositories/
│   │   ├── Infrastructure/
│   │   │   └── PostgreSqlTestContainer.cs
│   │   └── AuctionService.IntegrationTests.csproj
│   │
│   └── AuctionService.ContractTests/    # 契約測試
│       ├── BiddingServiceContractTests.cs
│       └── AuctionService.ContractTests.csproj
│
├── docs/                                # 文件資料夾
│   ├── architecture.md                  # 架構說明
│   ├── api-guide.md                     # API 使用指南
│   └── deployment.md                    # 部署指南
│
├── scripts/                             # 建置與部署腳本
│   ├── build.sh                         # Linux/macOS 建置腳本
│   ├── build.ps1                        # Windows 建置腳本
│   ├── init-db.sql                      # PostgreSQL 初始化腳本
│   └── run-tests.sh                     # 測試執行腳本
│
└── .github/                             # GitHub 相關設定
    ├── workflows/                       # CI/CD 工作流程
    │   ├── build.yml
    │   └── test.yml
    └── prompts/                         # AI 提示詞
        └── speckit.plan.prompt.md
```

**結構說明**:
- **單一根目錄**: 所有專案檔案（解決方案、Docker、README、建置文檔）都在 `AuctionService/` 根目錄中
- **src/**: 原始碼專案（4 個專案：Api, Core, Infrastructure, Shared）
- **tests/**: 測試專案（3 個測試專案）
- **docs/**: 技術文件集中管理
- **scripts/**: 建置與初始化腳本
- **.github/**: CI/CD 與 GitHub 設定

---
        ├── plan.md
        ├── research.md
        ├── data-model.md
        ├── quickstart.md (本檔案)
        └── contracts/
            └── openapi.yaml
```

---

## 設定檔說明

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=auction_dev;Username=auction_user;Password=auction_pass;Pooling=true;MinPoolSize=5;MaxPoolSize=100"
  },
  "BiddingService": {
    "BaseUrl": "http://localhost:5002"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  },
  "AllowedHosts": "*",
  "ReverseProxy": {
    "Routes": {
      "auction-route": {
        "ClusterId": "auction-cluster",
        "Match": {
          "Path": "/api/auctions/{**catch-all}"
        }
      }
    },
    "Clusters": {
      "auction-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://localhost:5001/"
          }
        }
      }
    }
  }
}
```

---

## 常用指令

### .NET CLI

```bash
# 還原套件
dotnet restore

# 建置專案
dotnet build

# 執行專案
dotnet run --project src/AuctionService.Api

# 執行測試
dotnet test

# 執行測試並顯示覆蓋率
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# 清理建置產物
dotnet clean
```

### Entity Framework Core

```bash
# 建立 Migration
dotnet ef migrations add <MigrationName> --project src/AuctionService.Infrastructure --startup-project src/AuctionService.Api

# 套用 Migration
dotnet ef database update --project src/AuctionService.Infrastructure --startup-project src/AuctionService.Api

# 回復 Migration
dotnet ef database update <PreviousMigrationName> --project src/AuctionService.Infrastructure --startup-project src/AuctionService.Api

# 移除最後一個 Migration (未套用)
dotnet ef migrations remove --project src/AuctionService.Infrastructure --startup-project src/AuctionService.Api

# 產生 SQL 腳本
dotnet ef migrations script --project src/AuctionService.Infrastructure --startup-project src/AuctionService.Api --output migration.sql
```

### Docker

```bash
# 從專案根目錄啟動服務
docker-compose up -d

# 停止服務
docker-compose down

# 查看日誌
docker-compose logs -f

# 重新建置映像
docker-compose build

# 進入容器
docker exec -it auction-api bash
docker exec -it auction-postgres psql -U auction_user -d auction_dev
```

---

## API 測試範例

### 使用 cURL

#### 1. 查詢商品清單

```bash
curl -X GET "http://localhost:5000/api/auctions?pageNumber=1&pageSize=20" \
  -H "Accept: application/json"
```

#### 2. 查詢商品詳細資訊

```bash
curl -X GET "http://localhost:5000/api/auctions/123e4567-e89b-12d3-a456-426614174000" \
  -H "Accept: application/json"
```

#### 3. 建立商品 (需要 JWT Token)

```bash
curl -X POST "http://localhost:5000/api/auctions" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "name": "iPhone 15 Pro Max",
    "description": "全新未拆封 iPhone 15 Pro Max 256GB 鈦藍色",
    "startingPrice": 15000.00,
    "categoryId": 1,
    "endTime": "2025-12-05T10:00:00Z"
  }'
```

#### 4. 追蹤商品 (需要 JWT Token)

```bash
curl -X POST "http://localhost:5000/api/follows" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "auctionId": "123e4567-e89b-12d3-a456-426614174000"
  }'
```

#### 5. 查詢分類清單

```bash
curl -X GET "http://localhost:5000/api/categories" \
  -H "Accept: application/json"
```

### 使用 PowerShell

```powershell
# 查詢商品清單
Invoke-RestMethod -Uri "http://localhost:5000/api/auctions" -Method Get

# 建立商品
$headers = @{
    "Content-Type" = "application/json"
    "Authorization" = "Bearer YOUR_JWT_TOKEN"
}

$body = @{
    name = "iPhone 15 Pro Max"
    description = "全新未拆封 iPhone 15 Pro Max 256GB 鈦藍色"
    startingPrice = 15000.00
    categoryId = 1
    endTime = "2025-12-05T10:00:00Z"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5000/api/auctions" -Method Post -Headers $headers -Body $body
```

---

## 常見問題排解

### 1. 無法連線到 PostgreSQL

**症狀**: `Npgsql.NpgsqlException: Connection refused`

**解決方法**:
- 確認 PostgreSQL 服務已啟動
- 檢查連線字串 Host、Port、Username、Password 是否正確
- 檢查防火牆設定

```bash
# Windows
sc query postgresql-x64-16

# Linux
sudo systemctl status postgresql

# 測試連線
psql -h localhost -U auction_user -d auction_dev
```

### 2. Migration 失敗

**症狀**: `A connection was successfully established...`

**解決方法**:
```bash
# 刪除 Migrations 資料夾中的所有檔案
# 重新建立 Migration
dotnet ef migrations add InitialCreate --project src/AuctionService.Infrastructure --startup-project src/AuctionService.Api

# 如果資料庫已存在，先刪除
dotnet ef database drop --project src/AuctionService.Infrastructure --startup-project src/AuctionService.Api
dotnet ef database update --project src/AuctionService.Infrastructure --startup-project src/AuctionService.Api
```

### 3. 套件還原失敗

**症狀**: `Unable to find package...`

**解決方法**:
```bash
# 清理 NuGet 快取
dotnet nuget locals all --clear

# 重新還原
dotnet restore
```

### 4. Port 被佔用

**症狀**: `Failed to bind to address...`

**解決方法**:
```bash
# Windows: 查詢 Port 使用情況
netstat -ano | findstr :5000

# 終止佔用的程序 (PID)
taskkill /F /PID <PID>

# 或修改 appsettings.json 中的 Port 設定
```

### 5. Docker Compose 啟動失敗

**症狀**: `Error response from daemon...`

**解決方法**:
```bash
# 查看詳細日誌
docker-compose logs

# 重新建置映像
docker-compose build --no-cache

# 清理未使用的資源
docker system prune -a
```

---

## 開發流程

### 1. 建立新分支

```bash
git checkout -b feature/new-feature
```

### 2. 開發與測試

```bash
# 執行專案
dotnet watch run --project src/AuctionService.Api

# 執行測試 (監看模式)
dotnet watch test
```

### 3. 提交變更

```bash
git add .
git commit -m "feat: add new feature"
git push origin feature/new-feature
```

### 4. 建立 Pull Request

前往 GitHub 建立 Pull Request，等待 Code Review。

---

## 效能監控

### 查詢執行時間

```bash
# 啟用 EF Core 查詢日誌
dotnet run --project src/AuctionService.Api --Logging:LogLevel:Microsoft.EntityFrameworkCore=Debug
```

### 資料庫查詢分析

```sql
-- 查看慢查詢
SELECT * FROM pg_stat_statements 
WHERE mean_exec_time > 100 
ORDER BY mean_exec_time DESC;

-- 查看索引使用情況
SELECT * FROM pg_stat_user_indexes WHERE idx_scan = 0;
```

---

## 下一步

1. 閱讀 [API 契約文件](./contracts/openapi.yaml)
2. 閱讀 [資料模型設計](./data-model.md)
3. 執行整合測試: `dotnet test tests/AuctionService.IntegrationTests`
4. 參考 [實作計畫](./plan.md) 了解完整架構

---

## 支援與聯絡

- **問題回報**: [GitHub Issues](https://github.com/your-org/AuctionService/issues)
- **Email**: support@auctionservice.com
- **文件**: [Wiki](https://github.com/your-org/AuctionService/wiki)

---

**最後更新**: 2025-12-02  
**維護者**: AuctionService Team
