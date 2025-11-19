# Member Service 快速開始指南

本文件提供 Member Service 本機開發環境的快速設置指南。

## 目錄

1. [前置需求](#前置需求)
2. [環境準備](#環境準備)
3. [專案啟動](#專案啟動)
4. [驗證安裝](#驗證安裝)
5. [常見問題](#常見問題)

---

## 前置需求

在開始之前，請確保您的開發環境已安裝以下工具：

### 必要工具

| 工具 | 版本要求 | 下載連結 |
|-----|---------|---------|
| .NET SDK | 9.0 或更高 | https://dotnet.microsoft.com/download |
| Docker Desktop | 最新穩定版 | https://www.docker.com/products/docker-desktop |
| PostgreSQL | 16 或更高 | https://www.postgresql.org/download/ |
| Git | 2.x 或更高 | https://git-scm.com/downloads |

### 推薦工具

- **IDE**: Visual Studio 2022、Rider 或 VS Code（含 C# Dev Kit）
- **API 測試**: Postman 或 Insomnia
- **資料庫管理**: DBeaver、pgAdmin 或 DataGrip

### 驗證安裝

```powershell
# 檢查 .NET SDK 版本
dotnet --version
# 預期輸出: 9.0.xxx

# 檢查 Docker 版本
docker --version
# 預期輸出: Docker version 24.x.x

# 檢查 PostgreSQL 版本
psql --version
# 預期輸出: psql (PostgreSQL) 16.x
```

---

## 環境準備

### 1. 複製專案

```powershell
# 複製倉庫
git clone https://github.com/tsengpeter/AuctionService.git

# 切換到 Member Service 專案目錄
cd AuctionService\src\MemberService
```

### 2. 啟動 PostgreSQL 資料庫

> **重要**：本指南使用**本地資料庫**進行開發。正式環境部署時會使用**雲端資料庫**（Azure Database for PostgreSQL / AWS RDS PostgreSQL），詳見 [plan.md - Database Strategy](plan.md#database-strategy)。

#### 方法 A：使用 Docker（推薦）

```powershell
# 啟動 PostgreSQL 容器
docker run -d `
  --name memberservice-db `
  -e POSTGRES_USER=memberservice `
  -e POSTGRES_PASSWORD=Dev@Password123 `
  -e POSTGRES_DB=memberservice_dev `
  -p 5432:5432 `
  postgres:16-alpine

# 驗證容器狀態
docker ps | Select-String memberservice-db
```

**優點**：
- ✅ 快速啟動（秒級）
- ✅ 完全隔離（不影響系統其他 PostgreSQL 安裝）
- ✅ 可隨時刪除重建（`docker rm -f memberservice-db`）
- ✅ 跨平台一致性（Windows/Linux/macOS）

#### 方法 B：本機安裝的 PostgreSQL

```powershell
# 建立資料庫
psql -U postgres -c "CREATE DATABASE memberservice_dev;"

# 建立專用使用者
psql -U postgres -c "CREATE USER memberservice WITH PASSWORD 'Dev@Password123';"
psql -U postgres -c "GRANT ALL PRIVILEGES ON DATABASE memberservice_dev TO memberservice;"
```

#### 正式環境（參考用，不在本指南執行）

正式環境使用雲端託管資料庫，由 DevOps/維運團隊配置：

**Azure Database for PostgreSQL**:
```bash
# 連線字串範例（透過 Azure Key Vault 注入）
DB_CONNECTION_STRING="Host=memberservice-prod.postgres.database.azure.com;Port=5432;Database=memberservice_prod;Username=adminuser;Password=${AZURE_DB_PASSWORD};SslMode=Require"
```

**AWS RDS for PostgreSQL**:
```bash
# 連線字串範例（透過 AWS Secrets Manager 注入）
DB_CONNECTION_STRING="Host=memberservice-prod.abc123.us-east-1.rds.amazonaws.com;Port=5432;Database=memberservice_prod;Username=adminuser;Password=${AWS_DB_PASSWORD};SslMode=Require"
```

**關鍵差異**：
- ✅ 強制 SSL/TLS 連線 (`SslMode=Require`)
- ✅ 密碼透過密鑰管理服務注入（絕不硬編碼）
- ✅ Private Endpoint / IP 白名單限制存取
- ✅ 自動備份與異地備援
- ✅ 高可用性配置（Multi-AZ / Zone-redundant HA）

### 3. 設定環境變數

#### Windows PowerShell

```powershell
# 設定 JWT 密鑰（至少 32 個字元）
$env:JWT_SECRET_KEY="your-super-secret-jwt-key-min-32-chars-long-for-hs256-algorithm"

# 設定資料庫連線字串
$env:DB_CONNECTION_STRING="Host=localhost;Port=5432;Database=memberservice_dev;Username=memberservice;Password=Dev@Password123"

# 設定 Snowflake ID 參數
$env:SNOWFLAKE_WORKER_ID="1"
$env:SNOWFLAKE_DATACENTER_ID="1"

# 驗證環境變數
Write-Host "JWT_SECRET_KEY: $env:JWT_SECRET_KEY"
Write-Host "DB_CONNECTION_STRING: $env:DB_CONNECTION_STRING"
```

#### Linux/macOS

```bash
# 設定環境變數
export JWT_SECRET_KEY="your-super-secret-jwt-key-min-32-chars-long-for-hs256-algorithm"
export DB_CONNECTION_STRING="Host=localhost;Port=5432;Database=memberservice_dev;Username=memberservice;Password=Dev@Password123"
export SNOWFLAKE_WORKER_ID="1"
export SNOWFLAKE_DATACENTER_ID="1"

# 驗證環境變數
echo $JWT_SECRET_KEY
echo $DB_CONNECTION_STRING
```

#### appsettings.Development.json（不推薦，僅限開發環境）

**⚠️ 注意：絕對不要將此檔案提交到版本控制系統！**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=memberservice_dev;Username=memberservice;Password=Dev@Password123"
  },
  "JwtSettings": {
    "SecretKey": "your-super-secret-jwt-key-min-32-chars-long-for-hs256-algorithm",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  },
  "SnowflakeSettings": {
    "WorkerId": 1,
    "DatacenterId": 1
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

### 4. 還原 NuGet 套件

```powershell
# 在專案根目錄執行
dotnet restore
```

---

## 專案啟動

### 1. 執行資料庫遷移（Code-First 自動建立資料表）

> **重要概念**：Member Service 採用 **EF Core Code-First** 設計，資料庫結構完全由程式碼驅動。開發者無需手動執行 SQL 建表語句，只需執行 Migration 命令即可自動建立/更新資料庫。

```powershell
# 切換到 Infrastructure 專案目錄
cd MemberService.Infrastructure

# 檢查現有的 Migration 清單
dotnet ef migrations list --startup-project ../MemberService.API
# 預期輸出: 20251118000000_InitialCreate (Pending)

# 執行 Migration，自動建立資料表（Users + RefreshTokens）
dotnet ef database update --startup-project ../MemberService.API

# 預期輸出:
# Applying migration '20251118000000_InitialCreate'.
# Done.
```

**Migration 做了什麼？**
1. ✅ 建立 `Users` 資料表（7 個欄位：Id, Email, PasswordHash, Username, CreatedAt, UpdatedAt）
2. ✅ 建立 `RefreshTokens` 資料表（6 個欄位：Id, Token, UserId, ExpiresAt, IsRevoked, CreatedAt）
3. ✅ 建立索引：Email (UNIQUE), Token (UNIQUE), (UserId, ExpiresAt) Composite
4. ✅ 設定外鍵：RefreshTokens.UserId → Users.Id (CASCADE DELETE)
5. ✅ 記錄 Migration 版本到 `__EFMigrationsHistory` 資料表

**驗證資料庫結構**：
```powershell
# 使用 psql 檢查建立的資料表
docker exec -it memberservice-db psql -U memberservice -d memberservice_dev -c "\dt"

# 預期輸出:
#                    List of relations
#  Schema |          Name           | Type  |    Owner     
# --------+-------------------------+-------+--------------
#  public | RefreshTokens          | table | memberservice
#  public | Users                  | table | memberservice
#  public | __EFMigrationsHistory  | table | memberservice
```

**Code-First 工作流程**（未來新增功能時）：
```powershell
# 1. 開發者修改實體類別（例如：在 User 類別新增 ProfilePicture 屬性）

# 2. 建立新的 Migration
dotnet ef migrations add AddUserProfilePicture --startup-project ../MemberService.API

# 3. 執行 Migration 更新資料庫
dotnet ef database update --startup-project ../MemberService.API

# 4. Migration 檔案自動提交到 Git，團隊成員拉取後執行相同命令即可同步資料庫結構
```

### 2. 啟動 API 服務

```powershell
# 切換到 API 專案目錄
cd ../MemberService.API

# 啟動開發伺服器（含熱重載）
dotnet watch run

# 預期輸出:
# info: Microsoft.Hosting.Lifetime[14]
#       Now listening on: http://localhost:5001
# info: Microsoft.Hosting.Lifetime[0]
#       Application started. Press Ctrl+C to shut down.
```

### 3. 存取 Swagger UI

在瀏覽器開啟：
```
http://localhost:5001/swagger
```

---

## 驗證安裝

### 使用 Swagger UI 測試

1. 開啟 http://localhost:5001/swagger
2. 展開 `POST /api/auth/register` 端點
3. 點擊 "Try it out"
4. 輸入測試資料：

```json
{
  "email": "test@example.com",
  "password": "TestPassword123",
  "username": "測試使用者"
}
```

5. 點擊 "Execute"
6. 預期回應：HTTP 201 Created

```json
{
  "success": true,
  "data": {
    "userId": 1234567890123456,
    "email": "test@example.com",
    "username": "測試使用者",
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "c4e5a8b9d2f3e1a7..."
  }
}
```

### 使用 curl 測試

#### 1. 註冊使用者

```powershell
curl -X POST http://localhost:5001/api/auth/register `
  -H "Content-Type: application/json" `
  -d '{
    "email": "user@example.com",
    "password": "SecurePassword123",
    "username": "張三"
  }'
```

#### 2. 登入

```powershell
curl -X POST http://localhost:5001/api/auth/login `
  -H "Content-Type: application/json" `
  -d '{
    "email": "user@example.com",
    "password": "SecurePassword123"
  }'
```

#### 3. 查詢個人資料

```powershell
# 將上一步取得的 accessToken 存入變數
$token="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."

curl -X GET http://localhost:5001/api/users/me `
  -H "Authorization: Bearer $token"
```

### 驗證資料庫

```powershell
# 使用 Docker 容器的 psql
docker exec -it memberservice-db psql -U memberservice -d memberservice_dev

# SQL 查詢
SELECT * FROM "Users";
SELECT * FROM "RefreshTokens";

# 退出 psql
\q
```

---

## 常見問題

### Q1: 遷移執行失敗，提示 "relation already exists"

**原因**：資料庫中已存在相同名稱的資料表。

**解決方案**：

```powershell
# 方法 A：刪除資料庫並重新建立
docker exec -it memberservice-db psql -U postgres -c "DROP DATABASE memberservice_dev;"
docker exec -it memberservice-db psql -U postgres -c "CREATE DATABASE memberservice_dev;"

# 方法 B：重置 EF Core 遷移
cd MemberService.Infrastructure
dotnet ef database drop --startup-project ../MemberService.API --force
dotnet ef database update --startup-project ../MemberService.API
```

### Q2: API 啟動時提示 "JWT Secret Key is too short"

**原因**：JWT_SECRET_KEY 環境變數未設定或長度不足。

**解決方案**：

```powershell
# 設定至少 32 個字元的密鑰
$env:JWT_SECRET_KEY="your-super-secret-jwt-key-min-32-chars-long-for-hs256-algorithm"

# 重新啟動 API
dotnet run
```

### Q3: 無法連線到 PostgreSQL，提示 "Connection refused"

**原因**：PostgreSQL 容器未啟動或連線參數錯誤。

**解決方案**：

```powershell
# 檢查 Docker 容器狀態
docker ps -a | Select-String memberservice-db

# 如果容器未執行，啟動它
docker start memberservice-db

# 檢查連線字串是否正確
$env:DB_CONNECTION_STRING
```

### Q4: Snowflake ID 產生失敗，提示 "Worker ID out of range"

**原因**：SNOWFLAKE_WORKER_ID 或 SNOWFLAKE_DATACENTER_ID 超出有效範圍（0-31）。

**解決方案**：

```powershell
# 設定有效的 Worker ID（0-31）
$env:SNOWFLAKE_WORKER_ID="1"
$env:SNOWFLAKE_DATACENTER_ID="1"
```

### Q5: 執行測試時失敗，提示 "Testcontainers requires Docker"

**原因**：Docker Desktop 未啟動或無法偵測到 Docker Daemon。

**解決方案**：

```powershell
# 啟動 Docker Desktop
Start-Process "C:\Program Files\Docker\Docker\Docker Desktop.exe"

# 等待 Docker 啟動完成
docker ps

# 重新執行測試
dotnet test
```

### Q6: 密碼變更後前端仍能使用舊的 Access Token

**行為**：這是預期的設計。Access Token 在過期前（15 分鐘）仍然有效。

**說明**：
- 密碼變更會**立即撤銷所有 Refresh Token**
- 但已簽發的 Access Token 在過期前仍可使用（無狀態 JWT 設計）
- 15 分鐘後，當 Access Token 過期時，客戶端嘗試使用 Refresh Token 更新時會失敗（因為 Refresh Token 已被撤銷）
- 此時使用者必須重新登入

**如需更嚴格的安全控制**，可考慮：
- 縮短 Access Token 有效期限（例如 5 分鐘）
- 實作 Token 黑名單機制（但會犧牲 JWT 的無狀態優勢）

---

## 執行測試

### 單元測試

```powershell
# 執行所有測試
cd ..  # 回到方案根目錄
dotnet test

# 僅執行單元測試
dotnet test --filter "FullyQualifiedName~MemberService.Domain.Tests"

# 執行特定測試類別
dotnet test --filter "FullyQualifiedName~UserTests"
```

### 整合測試（需要 Docker）

```powershell
# 確保 Docker Desktop 正在運行
docker ps

# 執行整合測試（Testcontainers 會自動啟動 PostgreSQL 容器）
dotnet test --filter "FullyQualifiedName~Integration"
```

### 產生測試覆蓋率報告

```powershell
# 安裝 coverlet 工具
dotnet tool install -g coverlet.console

# 執行測試並產生覆蓋率
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# 產生 HTML 報告（需要安裝 ReportGenerator）
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:**/coverage.opencover.xml -targetdir:coverage-report -reporttypes:Html

# 開啟報告
Start-Process coverage-report/index.html
```

---

## 開發工作流程

### 建立新的遷移

```powershell
cd MemberService.Infrastructure

# 建立遷移
dotnet ef migrations add YourMigrationName --startup-project ../MemberService.API

# 預覽 SQL（不執行）
dotnet ef migrations script --startup-project ../MemberService.API

# 執行遷移
dotnet ef database update --startup-project ../MemberService.API
```

### 回滾遷移

```powershell
# 查看遷移歷史
dotnet ef migrations list --startup-project ../MemberService.API

# 回滾到指定遷移
dotnet ef database update PreviousMigrationName --startup-project ../MemberService.API

# 刪除最後一個遷移（尚未套用到資料庫）
dotnet ef migrations remove --startup-project ../MemberService.API
```

### 查看日誌

```powershell
# 即時查看日誌（在另一個終端視窗）
dotnet run | Select-String -Pattern "ERROR|WARN"

# 查看 Serilog 輸出的 JSON 日誌檔案（如有設定）
Get-Content logs/member-service-$(Get-Date -Format yyyy-MM-dd).json -Tail 50 -Wait
```

---

## 下一步

- 📖 閱讀 [API 文件](contracts/openapi.yaml)
- 🧪 執行測試套件：`dotnet test`
- 🏗️ 查看專案結構：`specs/001-member-service/plan.md`
- 🔍 研究技術決策：`specs/001-member-service/research.md`
- 📊 理解資料模型：`specs/001-member-service/data-model.md`

如有問題，請參閱專案 Wiki 或聯繫開發團隊。
