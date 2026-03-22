# AuctionService

基於 ASP.NET Core 10 的模組化單體後端，專為拍賣平台設計，包含五個領域模組（Member、Auction、Bidding、Ordering、Notification）。採用 PostgreSQL Schema 隔離、JWT 身份驗證，以及 MediatR 進程內事件機制。

ASP.NET Core 10 Modular Monolith backend for an auction platform, featuring five domain modules (Member, Auction, Bidding, Ordering, Notification) with PostgreSQL schema isolation, JWT authentication, and MediatR-based in-process events.

## 環境需求 / Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) — 開發資料庫 + 應用程式 Image 打包 / Dev database + application image build

## 快速開始 / Quick Start

```powershell
# 1. 複製專案 / Clone and enter project
git clone <repo-url>
cd AuctionService

# 2. 設定環境變數 / Copy and configure environment
cp .env.example .env
# 編輯 .env：設定 POSTGRES_PASSWORD、JWT_SECRET（最少 32 字元）
# Edit .env: set POSTGRES_PASSWORD, JWT_SECRET (min 32 chars)

# 3. 啟動資料庫 / Start database
docker compose up -d

# 4. 設定連線字串環境變數 / Set connection string env var (PowerShell)
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=auctionservice;Username=auctionuser;Password=<your-password>"
$env:JWT_SECRET = "<your-32-char-secret>"

# 5. 套用資料庫遷移 / Apply migrations
foreach ($m in @("Member","Auction","Bidding","Ordering","Notification")) {
    dotnet ef database update --project src/Modules/$m/$m.csproj `
        --startup-project src/AuctionService.Api/AuctionService.Api.csproj `
        --context "${m}DbContext"
}

# 6. 啟動 API / Run the API
dotnet run --project src/AuctionService.Api/AuctionService.Api.csproj

# 7. 開啟 Swagger UI / Open Swagger UI
# http://localhost:5000/swagger
```

## 專案結構 / Project Structure

```
src/
├── AuctionService.Api/          # API 主機（Program.cs、Middleware）/ API host (Program.cs, middleware)
├── AuctionService.Shared/       # 共用層：ApiResponse<T>、BaseEntity、IDomainEvent、ValidationBehavior
│                                # Shared: ApiResponse<T>, BaseEntity, IDomainEvent, ValidationBehavior
└── Modules/
    ├── Member/                  # schema: member
    ├── Auction/                 # schema: auction
    ├── Bidding/                 # schema: bidding
    ├── Ordering/                # schema: ordering
    └── Notification/            # schema: notification

tests/
├── AuctionService.UnitTests/         # 54 個單元測試（無外部依賴）/ 54 unit tests (no external dependencies)
└── AuctionService.IntegrationTests/  # 13 個整合測試（Testcontainers PostgreSQL）/ 13 integration tests
```

## 主要端點 / Key Endpoints

| 方法 / Method | 路徑 / Path | 說明 / Description |
|--------|------|-------------|
| GET | `/health` | 健康檢查 / Health check (Healthy/Degraded) |
| GET | `/swagger` | API 文件（僅開發環境）/ Swagger UI (Development only) |

## 執行測試 / Running Tests

```bash
dotnet test
```

## Docker Image 打包 / Docker Image Build

```powershell
# 建置 Image / Build image
docker build -t auctionservice:latest .

# 執行容器（需提供環境變數）/ Run container (provide env vars)
docker run -d \
  -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="Host=host.docker.internal;Port=5432;Database=auctionservice;Username=auctionuser;Password=<password>" \
  -e JWT_SECRET="<your-32-char-secret>" \
  -e JWT_ISSUER="AuctionService" \
  -e JWT_AUDIENCE="AuctionService" \
  --name auctionservice \
  auctionservice:latest
```

詳細說明請參閱 [DEVELOPMENT.md](DEVELOPMENT.md)。See [DEVELOPMENT.md](DEVELOPMENT.md) for full details.

## 相關文件 / Documentation

- [DEVELOPMENT.md](DEVELOPMENT.md) — 開發者設定、常用指令、疑難排解 / Developer setup, commands, troubleshooting
- [ARCHITECTURE.md](ARCHITECTURE.md) — 架構概覽、模組邊界、事件流程 / Architecture overview, module boundaries, event flow
- [specs/001-backend-scaffold/spec.md](specs/001-backend-scaffold/spec.md) — 功能規格 / Feature specification
