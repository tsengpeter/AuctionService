# MemberService

MemberService 是拍賣系統的核心身份驗證微服務，提供使用者註冊、登入、認證與個人資料管理功能。

## 架構

- **框架**: ASP.NET Core 10 Web API
- **資料庫**: PostgreSQL 16
- **ORM**: Entity Framework Core 10 (Code-First)
- **認證**: JWT (HS256 對稱演算法)
- **密碼雜湊**: BCrypt (work factor 12)
- **ID 產生**: Snowflake ID (64-bit 分散式唯一識別碼)
- **架構模式**: Clean Architecture (Domain/Application/Infrastructure/API)
- **測試**: xUnit + Moq + FluentAssertions + Testcontainers
- **日誌**: Serilog 結構化日誌
- **容器化**: Docker + docker-compose

## 功能特色

- ✅ 使用者註冊與登入
- ✅ JWT 認證 (15 分鐘 Access Token + 7 天 Refresh Token)
- ✅ 個人資訊查詢 (認證/公開)
- ✅ 個人資訊更新 (username/email)
- ✅ 密碼變更 (需驗證舊密碼)
- ✅ 健康檢查端點
- ✅ 完整的單元測試與整合測試 (>80% 覆蓋率)

## 系統需求

- .NET SDK 10.0 或更新版本
- Docker Desktop 4.0+
- PostgreSQL 16 (或使用 Docker 容器)
- Windows/Linux/macOS

## 快速開始

### 使用 Docker Compose (推薦)

1. 複製專案
   ```bash
   git clone <repository-url>
   cd AuctionService/MemberService
   ```

2. 啟動資料庫
   ```bash
   docker-compose up -d
   ```

3. 執行應用程式
   ```bash
   dotnet run --project src/MemberService.API
   ```

4. 驗證服務運行
   ```bash
   curl http://localhost:5000/api/health
   ```

### 本地開發設定

1. 安裝 PostgreSQL 16
2. 建立資料庫 `memberservice`
3. 設定環境變數 (見下方)
4. 執行資料庫遷移
   ```bash
   dotnet ef database update --project src/MemberService.API
   ```
5. 啟動應用程式
   ```bash
   dotnet run --project src/MemberService.API
   ```

## API 端點

### 認證端點
- `POST /api/auth/register` - 使用者註冊
- `POST /api/auth/login` - 使用者登入
- `POST /api/auth/refresh-token` - 重新整理 JWT 權杖
- `POST /api/auth/logout` - 登出

### 使用者端點
- `GET /api/users/me` - 取得當前使用者資訊 (需要認證)
- `GET /api/users/{id}` - 取得公開使用者資訊
- `PUT /api/users/me` - 更新個人資訊 (需要認證)
- `PUT /api/users/me/password` - 變更密碼 (需要認證)

### 健康檢查
- `GET /api/health` - 基本健康檢查
- `GET /api/health/detailed` - 詳細健康檢查

## 環境變數設定

建立 `.env` 檔案或設定系統環境變數：

```bash
# 資料庫設定
DB_CONNECTION_STRING=Host=localhost;Port=5432;Database=memberservice;Username=postgres;Password=yourpassword

# JWT 設定
JWT_SECRET_KEY=your-32-character-or-longer-secret-key-here
JWT_ACCESS_TOKEN_EXPIRATION_MINUTES=15
JWT_REFRESH_TOKEN_EXPIRATION_DAYS=7

# Snowflake ID 設定
SNOWFLAKE_WORKER_ID=1
SNOWFLAKE_DATACENTER_ID=1

# 應用程式設定
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://localhost:5000;https://localhost:5001

# 日誌設定
LOG_LEVEL=Information
LOG_FILE_PATH=logs/memberservice-.log
```

### 環境變數說明

- **DB_CONNECTION_STRING**: PostgreSQL 連線字串
- **JWT_SECRET_KEY**: JWT 簽署金鑰 (至少 32 個字元)
- **JWT_ACCESS_TOKEN_EXPIRATION_MINUTES**: Access Token 過期時間 (預設 15 分鐘)
- **JWT_REFRESH_TOKEN_EXPIRATION_DAYS**: Refresh Token 過期時間 (預設 7 天)
- **SNOWFLAKE_WORKER_ID**: Snowflake 工作者 ID (0-31)
- **SNOWFLAKE_DATACENTER_ID**: Snowflake 資料中心 ID (0-31)
- **LOG_LEVEL**: 日誌等級 (Trace, Debug, Information, Warning, Error, Critical)
- **LOG_FILE_PATH**: 日誌檔案路徑模式

## Docker 部署

### 使用 Docker Compose

```bash
# 建置並啟動所有服務
docker-compose up --build

# 背景執行
docker-compose up -d --build

# 停止服務
docker-compose down

# 檢視日誌
docker-compose logs -f memberservice
```

### 手動 Docker 建置

```bash
# 建置映像
docker build -t memberservice .

# 執行容器
docker run -p 5000:80 \
  -e DB_CONNECTION_STRING="..." \
  -e JWT_SECRET_KEY="..." \
  memberservice
```

### Docker Compose 服務

- **memberservice**: ASP.NET Core API (連接埠 5000)
- **postgres**: PostgreSQL 資料庫 (連接埠 5432)
- **pgadmin**: PostgreSQL 管理介面 (連接埠 5050, 預設帳號: admin@admin.com / admin)

## 開發指南

### 專案結構

```
MemberService/
├── src/
│   ├── MemberService.API/           # Web API 層
│   ├── MemberService.Application/   # 應用程式層 (DTOs, Services, Validators)
│   ├── MemberService.Domain/        # 領域層 (Entities, Value Objects, Exceptions)
│   └── MemberService.Infrastructure/ # 基礎設施層 (EF Core, Security, Persistence)
├── tests/
│   ├── MemberService.Domain.Tests/      # 領域層測試
│   ├── MemberService.Application.Tests/ # 應用程式層測試
│   └── MemberService.IntegrationTests/  # 整合測試
├── docker-compose.yml
├── Dockerfile
└── README.md
```

### 執行測試

```bash
# 執行所有測試
dotnet test

# 執行特定專案測試
dotnet test tests/MemberService.Domain.Tests/

# 產生覆蓋率報告 (需要安裝 coverlet)
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=lcov
```

### 資料庫遷移

```bash
# 建立新遷移
dotnet ef migrations add MigrationName --project src/MemberService.API

# 套用遷移
dotnet ef database update --project src/MemberService.API

# 復原遷移
dotnet ef database update PreviousMigration --project src/MemberService.API
```

### 程式碼品質

- 遵循 Clean Architecture 原則
- 使用 TDD (Test-Driven Development) 開發流程
- 測試覆蓋率目標 >80%
- 使用 FluentValidation 進行輸入驗證
- 使用 Serilog 進行結構化日誌記錄

## 效能基準

- JWT 驗證延遲: <50ms (p95)
- API 端點回應時間: <200ms (p95)
- 密碼雜湊時間: ~250-350ms (BCrypt work factor 12)
- Snowflake ID 產生: <1ms

## 安全性考量

- 密碼使用 BCrypt 雜湊 (work factor 12)
- JWT 使用 HS256 對稱演算法
- Refresh Token 輪換機制
- SQL 注入防護 (EF Core 參數化查詢)
- CORS 設定 (視部署需求調整)

## 疑難排解

### 常見問題

1. **資料庫連線失敗**
   - 確認 PostgreSQL 服務正在運行
   - 檢查連線字串設定
   - 驗證資料庫存在且可存取

2. **JWT 認證失敗**
   - 確認 JWT_SECRET_KEY 至少 32 字元
   - 檢查權杖是否過期
   - 驗證權杖格式正確性

3. **測試執行失敗**
   - 確保 Docker Desktop 正在運行 (Testcontainers 需要)
   - 檢查連接埠 5432 未被其他服務佔用
   - 確認所有 NuGet 套件已還原

### 日誌位置

- 主控台日誌: 應用程式執行時輸出
- 檔案日誌: `logs/memberservice-YYYYMMDD.log`
- 結構化 JSON 格式，包含請求 ID、使用者 ID、執行時間等資訊

## 授權

本專案採用 MIT 授權條款。