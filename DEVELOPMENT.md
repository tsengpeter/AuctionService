# Development Guide / 開發指南

## 環境設定 / Environment Setup

### 必要工具 / Required Tools

| 工具 / Tool | 版本 / Version | 安裝 / Install |
|------|---------|--------|
| .NET SDK | 10.0.x | https://dotnet.microsoft.com/download/dotnet/10.0 |
| Docker Desktop | Latest | https://www.docker.com/products/docker-desktop |
| dotnet-ef | 10.x | `dotnet tool install --global dotnet-ef` |

### 環境變數 / Environment Variables

將 `.env.example` 複製為 `.env` 並填入設定值：

Copy `.env.example` to `.env` and fill in values:

```env
ASPNETCORE_ENVIRONMENT=Development
POSTGRES_USER=auctionuser
POSTGRES_PASSWORD=<your-password>
POSTGRES_DB=auctionservice
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=auctionservice;Username=auctionuser;Password=<your-password>
JWT_SECRET=<minimum-32-character-secret>
JWT_ISSUER=AuctionService
JWT_AUDIENCE=AuctionService
JWT_EXPIRY_MINUTES=60
```

> **安全性 / Security**：請勿將 `.env` 提交至版本控制。該檔案已列於 `.gitignore`。  
> Never commit `.env` to source control. The file is in `.gitignore`.

## 常用指令 / Common Commands

### 建置 / Build

```bash
dotnet build
```

### 啟動 API / Run API

```powershell
# 先設定必要環境變數 / Set required env vars first
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=auctionservice;Username=auctionuser;Password=<password>"
$env:JWT_SECRET = "<secret>"
$env:JWT_ISSUER = "AuctionService"
$env:JWT_AUDIENCE = "AuctionService"

dotnet run --project src/AuctionService.Api/AuctionService.Api.csproj
```

### 測試 / Test

```bash
# 執行所有測試 / All tests
dotnet test

# 僅單元測試 / Unit tests only
dotnet test tests/AuctionService.UnitTests/AuctionService.UnitTests.csproj

# 僅整合測試（需要 Docker）/ Integration tests only (requires Docker)
dotnet test tests/AuctionService.IntegrationTests/AuctionService.IntegrationTests.csproj
```

### 資料庫 / Database

```bash
# 啟動服務 / Start services
docker compose up -d

# 停止服務 / Stop services
docker compose down

# 停止並移除 Volumes / Stop and remove volumes
docker compose down -v
```

### 資料庫遷移 / Migrations

```powershell
# 新增遷移（替換 <Module> 和 <MigrationName>）/ Add migration
dotnet ef migrations add <MigrationName> `
    --project src/Modules/<Module>/<Module>.csproj `
    --startup-project src/AuctionService.Api/AuctionService.Api.csproj `
    --output-dir Infrastructure/Persistence/Migrations `
    --context <Module>DbContext

# 套用所有模組遷移 / Apply migrations for all modules
foreach ($m in @("Member","Auction","Bidding","Ordering","Notification")) {
    dotnet ef database update `
        --project "src/Modules/$m/$m.csproj" `
        --startup-project src/AuctionService.Api/AuctionService.Api.csproj `
        --context "${m}DbContext"
}
```

## Docker Image 打包與執行 / Docker Image Build & Run

### 建置 Image / Build Image

```powershell
# 在專案根目錄執行 / Run from project root
docker build -t auctionservice:latest .

# 指定版本標籤 / Tag with version
docker build -t auctionservice:1.0.0 -t auctionservice:latest .
```

### 執行容器 / Run Container

```powershell
docker run -d \
  -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="Host=host.docker.internal;Port=5432;Database=auctionservice;Username=auctionuser;Password=<password>" \
  -e JWT_SECRET="<your-32-char-secret>" \
  -e JWT_ISSUER="AuctionService" \
  -e JWT_AUDIENCE="AuctionService" \
  -e ASPNETCORE_ENVIRONMENT="Production" \
  --name auctionservice \
  auctionservice:latest
```

> 容器對外監聽 port **8080**。連線 PostgreSQL 時，`Host=host.docker.internal` 可從容器內連回本機的 Docker Compose 資料庫。  
> Container listens on port **8080**. Use `Host=host.docker.internal` to reach the local Docker Compose database from inside the container.

### 驗證容器是否正常運作 / Verify Container

```powershell
# 健康檢查 / Health check
Invoke-RestMethod http://localhost:8080/health

# 查看容器 log / View logs
docker logs auctionservice

# 停止並移除 / Stop and remove
docker stop auctionservice
docker rm auctionservice
```

### 注意事項 / Notes

- Image 採用**多階段建置（multi-stage build）**，最終 image 僅含執行環境（`aspnet:10.0`），不含 SDK。  
  The image uses **multi-stage build**; the final image contains only the runtime (`aspnet:10.0`), not the SDK.
- 容器以**非 root 使用者**（`appuser`）執行，符合最小權限原則。  
  The container runs as a **non-root user** (`appuser`) following the principle of least privilege.
- `ASPNETCORE_ENVIRONMENT=Production` 時 Swagger UI 不會對外開放。  
  Swagger UI is disabled when `ASPNETCORE_ENVIRONMENT=Production`.
- 資料庫遷移**不會**在容器啟動時自動執行，請在部署前於外部執行。  
  Database migrations are **not** run automatically on container start; run them externally before deployment.

## 架構概覽 / Architecture Overview

請參閱 [ARCHITECTURE.md](ARCHITECTURE.md) 了解完整系統設計。

See [ARCHITECTURE.md](ARCHITECTURE.md) for full system design.

## 疑難排解 / Troubleshooting

### API 無法啟動：「Connection string not configured」

請確認在執行 `dotnet run` 前已設定 `ConnectionStrings__DefaultConnection` 環境變數。

Ensure environment variable `ConnectionStrings__DefaultConnection` is set before running `dotnet run`.

### 整合測試失敗：無法連線資料庫 / Integration tests fail: cannot connect to database

請確認 Docker Desktop 正在執行。Testcontainers 會自動啟動隔離的 PostgreSQL 容器，測試結束後自動清除。

Ensure Docker Desktop is running. Testcontainers will start its own ephemeral PostgreSQL container automatically.

### dotnet-ef：找不到指令 / dotnet-ef: command not found

```powershell
dotnet tool install --global dotnet-ef
# 加入 PATH / Add to PATH
$env:PATH += ";$env:USERPROFILE\.dotnet\tools"
```

### 「More than one DbContext was found」

執行 `dotnet ef` 指令時請務必加上 `--context <Module>DbContext`，因為啟動專案可見所有 5 個模組的 DbContext。

Always specify `--context <Module>DbContext` when running `dotnet ef` commands for this solution since all 5 module DbContexts are visible from the startup project.

### JWT 驗證使用有效 Token 仍回傳 401 / JWT authentication returns 401 even with valid token

請確認 `JWT_SECRET`、`JWT_ISSUER`、`JWT_AUDIENCE` 環境變數與簽發 Token 時使用的值完全一致。

Verify `JWT_SECRET`, `JWT_ISSUER`, and `JWT_AUDIENCE` environment variables match the values used to issue the token.
