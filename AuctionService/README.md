# AuctionService

A microservice for managing auctions in the auction application.

## Overview

The AuctionService is built using ASP.NET Core 10 Web API with Clean Architecture principles. It provides auction management functionality including creating, browsing, and bidding on auctions.

## Architecture

- **API Layer**: ASP.NET Core Web API controllers and middleware
- **Core Layer**: Business logic and domain entities
- **Infrastructure Layer**: Data access and external service integrations
- **Shared Layer**: Common utilities and extensions

## Technology Stack

- **Framework**: ASP.NET Core 10 Web API
- **Language**: C# 13
- **Database**: PostgreSQL 16+
- **ORM**: Entity Framework Core 10
- **Testing**: xUnit, FluentAssertions, Moq, Testcontainers
- **Logging**: Serilog
- **Validation**: FluentValidation
- **Documentation**: Swagger/OpenAPI

## Prerequisites

- .NET 10 SDK
- Docker (for local PostgreSQL development)
- Docker Compose (for multi-container setup)

## Development Setup

1. Clone the repository
2. Navigate to the AuctionService directory
3. Restore packages: `dotnet restore`
4. Start PostgreSQL: `docker-compose up -d postgres`
5. Run migrations: `dotnet ef database update`
6. Run the application: `dotnet run --project src/AuctionService.Api`

## Testing

- Unit tests: `dotnet test tests/AuctionService.UnitTests`
- Integration tests: `dotnet test tests/AuctionService.IntegrationTests`
- Contract tests: `dotnet test tests/AuctionService.ContractTests`

## Quick Start

### 快速啟動（推薦）
```powershell
.\scripts\start-dev.ps1
```
此腳本會自動：
- ✅ 檢查 Docker 狀態
- ✅ 停止舊容器
- ✅ 啟動新容器
- ✅ 等待服務就緒
- ✅ 自動打開 API 文檔

### 手動啟動
```bash
# 啟動所有服務
docker-compose up -d

# 查看日誌
docker-compose logs -f auction-service

# 停止服務
docker-compose down
```

## API 文檔

服務啟動後可訪問以下端點：

| 服務 | URL | 說明 |
|------|-----|------|
| 🌐 Swagger UI | http://localhost:5000/swagger | 傳統 OpenAPI 文檔介面 |
| 🎨 Scalar UI | http://localhost:5000/scalar/v1 | 現代化 API 文檔（推薦） |
| 📄 OpenAPI JSON | http://localhost:5000/openapi/v1.json | JSON 格式 API 規格 |
| 📄 OpenAPI YAML | http://localhost:5000/openapi/v1/openapi.yaml | YAML 格式 API 規格 |

### PostgreSQL 連接資訊
- **Host**: localhost
- **Port**: 5432
- **Database**: auctiondb
- **Username**: auctionuser
- **Password**: auctionpass

## 效能優化

Docker 啟動效能提升：

| 項目 | 優化前 | 優化後 | 改善 |
|------|--------|--------|------|
| PostgreSQL 就緒時間 | ~30s | ~5-8s | ⬇️ 75% |
| API 首次連接 | 多次重試 | 一次成功 | ✅ |
| 總啟動時間 | ~45s | ~10-15s | ⬇️ 67% |

**優化項目**：
- PostgreSQL healthcheck 機制（5s 間隔，5 次重試）
- `service_healthy` 條件確保資料庫就緒後再啟動 API
- 簡化 Docker 配置，移除 HTTPS，統一使用 HTTP 8080 端口

## 常見問題

### Q: 首次啟動為什麼較慢？
A: 需要下載 Docker 映像檔（~300MB），之後會使用快取。

### Q: 容器無法啟動？
A: 檢查日誌並確認：
```bash
docker-compose logs auction-service
```
- PostgreSQL 已啟動
- 端口 5000 和 5432 未被占用
- 連接字符串正確

### Q: 開發時如何即時重新載入？
A: 使用 dotnet watch 在本地開發：
```bash
cd src/AuctionService.Api
dotnet watch run
```
瀏覽器訪問：http://localhost:5106/swagger

### Q: 如何清理 Docker 資源？
```bash
# 停止並刪除容器
docker-compose down

# 完全清理（包含資料卷）
docker-compose down -v

# 刪除映像
docker rmi auctionservice-auction-service
```

## Contributing

1. Follow the established coding standards
2. Write tests for new functionality
3. Ensure all tests pass before submitting PR
4. Update documentation as needed