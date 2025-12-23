# 快速開始指南 (Quickstart Guide)

**功能**: 競標服務 (Bidding Service)  
**日期**: 2025-12-03  
**狀態**: Phase 1 Design

---

## 概述 (Overview)

本指南協助開發者在本地環境快速啟動 Bidding Service 進行開發與測試。

**目標**:
- ✅ 5 分鐘內完成環境設定
- ✅ 執行單元測試與整合測試
- ✅ 本地執行服務並測試 API

---

## 前置需求 (Prerequisites)

### 必要工具

| 工具 | 版本 | 用途 | 安裝方式 |
|------|------|------|------|
| **.NET SDK** | 10.0+ | 執行 ASP.NET Core | [下載](https://dotnet.microsoft.com/download) |
| **Docker Desktop** | 4.25+ | 執行 PostgreSQL + Redis | [下載](https://www.docker.com/products/docker-desktop) |
| **Git** | 2.40+ | 版本控制 | [下載](https://git-scm.com/downloads) |
| **VS Code** (選用) | 1.85+ | 程式編輯器 | [下載](https://code.visualstudio.com/) |

### 選用工具

| 工具 | 用途 | 安裝方式 |
|------|------|---------|
| **Azure Data Studio** | PostgreSQL 管理 | [下載](https://docs.microsoft.com/sql/azure-data-studio/download) |
| **RedisInsight** | Redis 管理 | [下載](https://redis.com/redis-enterprise/redis-insight/) |
| **Postman** | API 測試 | [下載](https://www.postman.com/downloads/) |

### 驗證安裝

```powershell
# 驗證 .NET SDK
dotnet --version
# 預期輸出: 10.0.x

# 驗證 Docker
docker --version
# 預期輸出: Docker version 24.x.x

# 驗證 Git
git --version
# 預期輸出: git version 2.x.x
```

---

## 快速啟動 (Quick Start)

### 步驟 1: Clone 專案

```powershell
# Clone 專案
git clone https://github.com/tsengpeter/AuctionService.git
cd BiddingService
```

### 步驟 2: 啟動基礎設施 (PostgreSQL + Redis)

```powershell
# 啟動 Docker Compose (在專案根目錄)
docker-compose up -d

# 驗證容器狀態
docker-compose ps
# 預期輸出:
# NAME                IMAGE               STATUS
# postgres_bidding    postgres:16-alpine  Up
# redis_bidding       redis:7-alpine      Up
```

**docker-compose.yml 範例**:
```yaml
version: '3.8'

services:
  postgres:
    image: postgres:16-alpine
    container_name: postgres_bidding
    environment:
      POSTGRES_DB: bidding_dev
      POSTGRES_USER: bidding_user
      POSTGRES_PASSWORD: bidding_pass
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U bidding_user"]
      interval: 10s
      timeout: 5s
      retries: 5

  redis:
    image: redis:7-alpine
    container_name: redis_bidding
    ports:
      - "6379:6379"
    command: redis-server --appendonly yes --appendfsync everysec
    volumes:
      - redis_data:/data
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      timeout: 3s
      retries: 5

volumes:
  postgres_data:
  redis_data:
```

### 步驟 3: 設定環境變數

```powershell
# 複製環境變數範本
cd src/BiddingService.Api
cp appsettings.Development.json.example appsettings.Development.json

# 編輯 appsettings.Development.json (選用，預設值已可使用)
```

**appsettings.Development.json 範例**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=bidding_dev;Username=bidding_user;Password=bidding_pass",
    "Redis": "localhost:6379,abortConnect=false"
  },
  "IdGenerator": {
    "WorkerId": 1,
    "DatacenterId": 1,
    "Epoch": "2024-01-01T00:00:00Z"
  },
  "Encryption": {
    "KeyVaultUrl": "https://your-keyvault.vault.azure.net/",
    "KeyName": "bidding-encryption-key",
    "UseLocalKey": true,
    "LocalKey": "DEVELOPMENT_KEY_32_BYTES_BASE64=="
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "BiddingService": "Debug"
    }
  },
  "AuctionService": {
    "BaseUrl": "http://localhost:5000",
    "Timeout": 5000
  }
}
```

### 步驟 4: 執行資料庫遷移

```powershell
# 安裝 EF Core CLI (首次執行)
dotnet tool install --global dotnet-ef

# 新增初始 Migration
dotnet ef migrations add InitialCreate --project src/BiddingService.Infrastructure

# 套用 Migration
dotnet ef database update --project src/BiddingService.Infrastructure

# 驗證資料庫
docker exec -it postgres_bidding psql -U bidding_user -d bidding_dev -c "\dt"
# 預期輸出: Bids 資料表
```

### 步驟 5: 執行測試

```powershell
# 單元測試
cd tests/BiddingService.UnitTests
dotnet test

# 整合測試 (需要 Docker 執行 Testcontainers)
cd tests/BiddingService.IntegrationTests
dotnet test

# 負載測試 (選用)
cd loadtests/BiddingService.LoadTests
dotnet run
```

**預期輸出**:
```
Passed!  - Failed:     0, Passed:    42, Skipped:     0, Total:    42, Duration: 2.5s
```

### 步驟 6: 啟動服務

```powershell
# 啟動 API
cd src/BiddingService.Api
dotnet run

# 預期輸出:
# info: Microsoft.Hosting.Lifetime[0]
#       Now listening on: http://localhost:5001
# info: Microsoft.Hosting.Lifetime[0]
#       Application started. Press Ctrl+C to shut down.
```

### 步驟 7: 測試 API

**使用 cURL**:
```powershell
# 健康檢查
curl http://localhost:5001/health

# 新增出價 (需要 JWT Token)
curl -X POST http://localhost:5001/api/bids `
  -H "Content-Type: application/json" `
  -H "Authorization: Bearer YOUR_JWT_TOKEN" `
  -d '{\"auctionId\": 123456789, \"amount\": 1500.00}'

# 查詢商品出價歷史
curl http://localhost:5001/api/auctions/123456789/bids

# 查詢最高出價
curl http://localhost:5001/api/auctions/123456789/highest-bid
```

**使用 Postman**:
1. 匯入 `contracts/openapi.yaml`
2. 設定環境變數 `baseUrl = http://localhost:5001`
3. 執行 Collection Runner

---

## 專案結構 (Project Structure)

```
BiddingService/                              # 專案根目錄
├── BiddingService.sln                       # Visual Studio 解決方案
├── README.md
├── docker-compose.yml
├── Dockerfile
├── .gitignore
├── .editorconfig
│
├── src/
│   ├── BiddingService.Api/                  # ASP.NET Core Web API
│   │   ├── Controllers/
│   │   ├── Middlewares/
│   │   ├── Program.cs
│   │   └── appsettings.json
│   ├── BiddingService.Core/                 # 核心業務邏輯
│   │   ├── Entities/
│   │   ├── DTOs/
│   │   ├── Services/
│   │   └── Interfaces/
│   ├── BiddingService.Infrastructure/       # 基礎設施
│   │   ├── Data/
│   │   ├── Repositories/
│   │   ├── Redis/
│   │   ├── BackgroundServices/
│   │   └── Encryption/
│   └── BiddingService.Shared/               # 共用元件
│       ├── Constants/
│       └── Extensions/
│
├── tests/
│   ├── BiddingService.UnitTests/
│   └── BiddingService.IntegrationTests/
│
├── loadtests/
│   └── BiddingService.LoadTests/
│
├── scripts/
│   ├── build.sh
│   └── run-tests.sh
│
├── docs/
│   ├── architecture.md
│   └── api-guide.md
│
└── .github/
    └── workflows/
```

---

## 開發工作流程 (Development Workflow)

### 1. 新增功能

```powershell
# 1. 建立功能分支
git checkout -b feature/add-bid-notification

# 2. 實作功能 (TDD)
# - 撰寫測試 (tests/BiddingService.UnitTests/)
# - 實作程式碼 (src/BiddingService.Api/ 或 src/BiddingService.Core/)
# - 執行測試確保通過

dotnet test

# 3. 提交變更
git add .
git commit -m "feat: 新增出價通知功能"

# 4. 推送分支
git push origin feature/add-bid-notification

# 5. 建立 Pull Request
```

### 2. 執行整合測試

```powershell
# 確保 Docker 容器執行中
docker-compose ps

# 執行整合測試
cd tests/BiddingService.IntegrationTests
dotnet test --logger "console;verbosity=detailed"
```

### 3. 偵錯 (Debugging)

**VS Code**:
1. 按 `F5` 啟動偵錯
2. 在程式碼設定中斷點
3. 使用 Postman 發送請求觸發中斷點

**命令列**:
```powershell
# 啟用詳細日誌
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run --project src/BiddingService.Api
```

### 4. 查看日誌

```powershell
# PostgreSQL 日誌
docker logs postgres_bidding

# Redis 日誌
docker logs redis_bidding

# 應用程式日誌 (Serilog 輸出到 Console)
# 查看 Terminal 輸出
```

---

## 常見問題 (Troubleshooting)

### 問題 1: 無法連線到 PostgreSQL

**症狀**:
```
Npgsql.NpgsqlException: Connection refused
```

**解決方案**:
```powershell
# 1. 檢查容器狀態
docker-compose ps

# 2. 重啟容器
docker-compose restart postgres

# 3. 檢查連線字串
# 確認 appsettings.Development.json 中的 Host 為 localhost (非 127.0.0.1)
```

### 問題 2: Redis 連線逾時

**症狀**:
```
StackExchange.Redis.RedisTimeoutException: Timeout performing GET
```

**解決方案**:
```powershell
# 1. 測試 Redis 連線
docker exec -it redis_bidding redis-cli ping
# 預期輸出: PONG

# 2. 檢查連線字串
# 確認 ConnectionStrings:Redis 包含 abortConnect=false

# 3. 增加逾時設定
# Redis: "localhost:6379,abortConnect=false,connectTimeout=10000"
```

### 問題 3: EF Core Migration 失敗

**症狀**:
```
Build FAILED.
```

**解決方案**:
```powershell
# 1. 清理專案
dotnet clean

# 2. 重新建置
dotnet build

# 3. 移除失敗的 Migration
dotnet ef migrations remove --project src/BiddingService.Infrastructure

# 4. 重新新增 Migration
dotnet ef migrations add InitialCreate --project src/BiddingService.Infrastructure
```

### 問題 4: 測試容器無法啟動 (Testcontainers)

**症狀**:
```
Docker.DotNet.DockerApiException: Docker API responded with status code=InternalServerError
```

**解決方案**:
```powershell
# 1. 確認 Docker Desktop 執行中
# 檢查系統狀態列

# 2. 重啟 Docker Desktop

# 3. 確認 Docker Daemon 可存取
docker ps

# 4. 清理未使用的容器
docker system prune -f
```

### 問題 5: Snowflake ID 重複

**症狀**:
```
Npgsql.PostgresException: duplicate key value violates unique constraint "PK_Bids"
```

**解決方案**:
```csharp
// 確保每個實例使用不同的 WorkerId
// appsettings.Development.json
{
  "IdGenerator": {
    "WorkerId": 1,  // 每個實例需不同
    "DatacenterId": 1
  }
}
```

---

## 效能測試 (Performance Testing)

### 使用 LoadTests 專案

```powershell
cd loadtests/BiddingService.LoadTests
dotnet run

# 預期輸出:
# Requests: 10000
# Success: 9998 (99.98%)
# Failed: 2 (0.02%)
# P50: 8ms
# P95: 12ms
# P99: 25ms
```

### 使用 Apache Bench

```powershell
# 安裝 Apache Bench (透過 Chocolatey)
choco install apache-httpd

# 執行負載測試 (1000 請求，10 並發)
ab -n 1000 -c 10 -H "Authorization: Bearer YOUR_JWT_TOKEN" `
   -p bid-payload.json -T application/json `
   http://localhost:5001/api/bids
```

**bid-payload.json**:
```json
{
  "auctionId": 123456789,
  "amount": 1500.00
}
```

---

## 部署準備 (Deployment Preparation)

### 環境變數檢查清單

- [ ] `ConnectionStrings:DefaultConnection` (PostgreSQL 連線字串)
- [ ] `ConnectionStrings:Redis` (Redis 連線字串)
- [ ] `Encryption:KeyVaultUrl` (Azure Key Vault URL)
- [ ] `Encryption:KeyName` (加密金鑰名稱)
- [ ] `IdGenerator:WorkerId` (每個實例唯一)
- [ ] `AuctionService:BaseUrl` (Auction Service URL)

### Docker 映像檔建置

```powershell
# 建置映像檔 (在專案根目錄)
docker build -t bidding-service:1.0.0 .

# 執行容器
docker run -d -p 5001:8080 `
  -e ConnectionStrings__DefaultConnection="Host=postgres;..." `
  -e ConnectionStrings__Redis="redis:6379" `
  --name bidding-service `
  bidding-service:1.0.0

# 測試
curl http://localhost:5001/health
```

**Dockerfile 範例**:
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/BiddingService.Api/BiddingService.Api.csproj", "src/BiddingService.Api/"]
COPY ["src/BiddingService.Core/BiddingService.Core.csproj", "src/BiddingService.Core/"]
COPY ["src/BiddingService.Infrastructure/BiddingService.Infrastructure.csproj", "src/BiddingService.Infrastructure/"]
COPY ["src/BiddingService.Shared/BiddingService.Shared.csproj", "src/BiddingService.Shared/"]
RUN dotnet restore "src/BiddingService.Api/BiddingService.Api.csproj"
COPY . .
WORKDIR "/src/src/BiddingService.Api"
RUN dotnet build "BiddingService.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BiddingService.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BiddingService.Api.dll"]
```

---

## 資源連結 (Resources)

### 官方文件
- [ASP.NET Core 文件](https://docs.microsoft.com/aspnet/core/)
- [Entity Framework Core 文件](https://docs.microsoft.com/ef/core/)
- [StackExchange.Redis 文件](https://stackexchange.github.io/StackExchange.Redis/)
- [xUnit 文件](https://xunit.net/)
- [Testcontainers 文件](https://dotnet.testcontainers.org/)

### 相關專案
- [IdGen GitHub](https://github.com/RobThree/IdGen)
- [Serilog GitHub](https://github.com/serilog/serilog)
- [Polly GitHub](https://github.com/App-vNext/Polly)

### 工具
- [Swagger UI](http://localhost:5001/swagger) (本地開發)
- [RedisInsight](https://redis.com/redis-enterprise/redis-insight/)
- [Azure Data Studio](https://docs.microsoft.com/sql/azure-data-studio/)

---

## 下一步 (Next Steps)

1. **閱讀規格文件**: 查看 `specs/003-bidding-service/spec.md`
2. **了解技術決策**: 查看 `specs/003-bidding-service/research.md`
3. **查看資料模型**: 查看 `specs/003-bidding-service/data-model.md`
4. **測試 API**: 匯入專案根目錄的 `contracts/openapi.yaml` 到 Postman
5. **執行整合測試**: `dotnet test tests/BiddingService.IntegrationTests`

---

**版本**: 1.0  
**狀態**: Phase 1 Design Complete  
**作者**: AI Assistant  
**審核**: Pending
