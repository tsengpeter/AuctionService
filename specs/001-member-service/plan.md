# 實作計畫：MemberService

**分支**: `001-member-service` | **日期**: 2025-12-02 | **規格**: [spec.md](./spec.md)  
**輸入**: 功能規格文件 `/specs/001-member-service/spec.md`

**注意**: 本文件由 `/speckit.plan` 指令產生。執行工作流程請參閱 `.specify/templates/commands/plan.md`。

## 摘要

MemberService 提供使用者註冊、登入、身份驗證與個人資料管理功能，是拍賣系統的核心身份驗證服務。採用 ASP.NET Core 10 Web API + PostgreSQL 16 + Entity Framework Core 10 Code-First 架構，使用 Clean Architecture 分層設計（Domain/Application/Infrastructure/API）。

技術重點：
- **Snowflake ID**: 使用 IdGen 套件產生 64-bit 分散式唯一識別碼，取代 GUID（空間節省 50%，時間有序）
- **密碼安全**: bcrypt(password + snowflakeId) 組合，work factor 12，防禦深度策略
- **JWT 驗證**: HS256 對稱金鑰演算法，15 分鐘 Access Token + 7 天 Refresh Token
- **無 AutoMapper**: 使用 POCO 手動映射 DTO，提升效能與可讀性
- **Controller-based API**: 不使用 Minimal APIs，採用傳統控制器設計
- **TDD 驅動**: xUnit + Moq + FluentAssertions + Testcontainers，目標覆蓋率 >80%

## 技術背景

**語言/版本**: ASP.NET Core 10, C# 13 (.NET 10 LTS)  
**主要相依套件**:
  - IdGen 3.x (Snowflake ID 產生器)
  - BCrypt.Net-Next 4.0.3 (密碼雜湊)
  - System.IdentityModel.Tokens.Jwt 8.0.0 (JWT 驗證)
  - Npgsql.EntityFrameworkCore.PostgreSQL 10.0 (PostgreSQL 驅動)
  - FluentValidation.AspNetCore 11.3.0 (輸入驗證)
  - Serilog.AspNetCore 8.0 (結構化日誌)

**資料儲存**: 
  - **本地開發**: PostgreSQL 16 本地安裝或 Docker 容器
  - **正式環境**: Azure Database for PostgreSQL / AWS RDS PostgreSQL（雲端託管）
  - **資料庫建置**: EF Core Code-First Migrations（資料庫結構由程式碼自動產生）
  
**測試工具**: xUnit 2.6+、Moq 4.20+、FluentAssertions 6.12+、Testcontainers.PostgreSql 3.7+ (整合測試)  
**目標平台**: Linux 容器 (Docker)，透過 .NET 10 跨平台部署  
**專案類型**: 單一微服務（所有專案檔案、解決方案、Docker、README 等建置文檔集中在單一資料夾 `MemberService/` 中）  
**專案架構**: Clean Architecture 四層架構（API、Application、Domain、Infrastructure）  
**效能目標**:
  - JWT 驗證延遲 <50ms p95
  - API 端點回應時間 <200ms p95
  - bcrypt 密碼雜湊 ~250-350ms（work factor 12）
  - Snowflake ID 產生 <1ms

**限制條件**:
  - 必須採用 TDD 開發流程（測試覆蓋率 >80%）
  - 不使用 AutoMapper（手動 POCO 映射）
  - 不使用 Minimal APIs（Controller-based）
  - API Gateway 使用 YARP（非 Ocelot）
  - 錯誤訊息與文件必須使用繁體中文
  - JWT 存取權杖有效期限：15 分鐘
  - 更新權杖有效期限：7 天
  - 密碼：至少 8 字元，需包含大小寫字母、數字與特殊符號
  - 使用者名稱：3-50 個字元，僅允許字母與空格
  - bcrypt 工作因子：12

**規模範圍**:
  - 預估初期使用者數：10,000 人
  - 同時線上使用者：500-1,000 人
  - API 端點數量：8 個（4 個公開 + 4 個私有）
  - 資料庫資料表：2 個（Users + RefreshTokens）
  - 預估程式碼規模：~3,000-5,000 LOC（不含測試）
  - 測試覆蓋率：業務邏輯 > 80%

## 資料庫策略

### 開發環境（本地開發）

**資料庫部署方式**:
- **選項 A（推薦）**: 使用 Docker 容器執行 PostgreSQL 16
  ```bash
  docker run -d --name memberservice-db \
    -e POSTGRES_USER=memberservice \
    -e POSTGRES_PASSWORD=Dev@Password123 \
    -e POSTGRES_DB=memberservice_dev \
    -p 5432:5432 postgres:16-alpine
  ```
- **選項 B**: 本機安裝 PostgreSQL 16（Windows/macOS/Linux）

**連線字串**:
```bash
DB_CONNECTION_STRING="Host=localhost;Port=5432;Database=memberservice_dev;Username=memberservice;Password=Dev@Password123"
BCRYPT_WORK_FACTOR="10"  # 開發環境建議降低成本因子以加速單元測試（正式環境使用 12）
```

**資料庫初始化流程**（EF Core Code-First）:
```bash
# 1. 建立遷移檔案（開發者在新增/修改實體後執行）
cd MemberService.Infrastructure
dotnet ef migrations add InitialCreate --startup-project ../MemberService.API

# 2. 執行遷移，自動建立/更新資料庫結構
dotnet ef database update --startup-project ../MemberService.API
```

**優點**:
- ✅ 完全本地控制，無需網路連線
- ✅ 快速啟動與測試（Docker 容器秒級啟動）
- ✅ 開發者可自由建立/刪除資料庫進行測試
- ✅ 支援離線開發
- ✅ 無雲端資源成本

### 正式環境（Production）

**雲端資料庫服務**（選擇其一）:

#### 選項 1: Azure Database for PostgreSQL - Flexible Server
- **規格建議**: 
  - Compute: General Purpose, 2 vCores, 8GB RAM（初期）
  - Storage: 128GB, Auto-growth enabled
  - Backup: 7 天自動備份，異地備援
- **連線方式**: Private Endpoint（透過 VNet 連線，不對外公開）
- **高可用性**: Zone-redundant HA（可選，建議正式環境啟用）

#### 選項 2: AWS RDS for PostgreSQL
- **規格建議**: 
  - Instance: db.t4g.medium (2 vCPU, 4GB RAM)（初期）
  - Storage: 100GB gp3, Auto-scaling enabled
  - Backup: 7 天自動備份，Multi-AZ 部署（HA）
- **連線方式**: 置於 Private Subnet，透過 Security Group 限制存取

**連線字串配置**（透過環境變數注入）:
```bash
# Azure 範例
DB_CONNECTION_STRING="Host=memberservice-prod.postgres.database.azure.com;Port=5432;Database=memberservice_prod;Username=adminuser;Password=${PROD_DB_PASSWORD};SslMode=Require"

# AWS RDS 範例
DB_CONNECTION_STRING="Host=memberservice-prod.abc123.us-east-1.rds.amazonaws.com;Port=5432;Database=memberservice_prod;Username=adminuser;Password=${PROD_DB_PASSWORD};SslMode=Require"
```

**安全設定**:
- ✅ **強制 SSL/TLS 連線**（`SslMode=Require`）
- ✅ **密碼透過 Azure Key Vault / AWS Secrets Manager 管理**（絕不硬編碼）
- ✅ **IP 白名單 / Private Endpoint**（僅允許 API Server 存取）
- ✅ **定期自動備份**（7-30 天保留期）
- ✅ **啟用查詢效能監控**（Azure Query Performance Insight / AWS Performance Insights）

### 部署與資料庫遷移流程

**Code-First 遷移策略**:

1. **開發階段**:
   ```bash
   # 開發者在本地執行
   dotnet ef migrations add AddUserProfilePicture
   dotnet ef database update  # 更新本地資料庫
   git add Migrations/
   git commit -m "feat: add user profile picture field"
   ```

2. **CI/CD Pipeline**（Azure DevOps / GitHub Actions）:
   ```yaml
   # 自動化部署流程
   - name: Build Docker Image
     run: docker build -t memberservice:${{ github.sha }} .
   
   - name: Run Database Migrations
     run: |
       docker run --rm \
         -e DB_CONNECTION_STRING="${{ secrets.PROD_DB_CONNECTION }}" \
         memberservice:${{ github.sha }} \
         dotnet ef database update --project MemberService.Infrastructure \
                                    --startup-project MemberService.API
   
   - name: Deploy to Production
     run: kubectl apply -f k8s/deployment.yaml
   ```

3. **正式環境資料庫更新**:
   - ✅ 遷移檔案隨程式碼一起版控（Git）
   - ✅ 部署前自動執行 `dotnet ef database update`
   - ✅ 支援 Blue-Green Deployment（舊版 schema 相容新版程式碼）
   - ✅ 遷移失敗時自動回滾（交易式 migration）

**Zero-Downtime Migration 策略**:
- 新增欄位時設定 `nullable: true`，避免部署中斷
- 刪除欄位前先部署不使用該欄位的程式碼版本
- 使用 `[Obsolete]` 標記待移除的實體屬性

### 環境變數配置對照表

| 環境 | 資料庫來源 | SSL | 備份策略 | 連線池大小 | 效能監控 |
|------|----------|-----|---------|-----------|----------|
| **Local** | Docker/本機 | 不需要 | 手動（開發者自行備份） | 預設 | Console Logs |
| **Staging** | Azure/AWS（小規格） | **必須** | 7 天自動備份 | 20-50 | 啟用（測試） |
| **Production** | Azure/AWS（HA 部署） | **必須** | 30 天自動備份 + 異地備援 | 100+ | **全面監控** |

### 資料庫遷移注意事項

⚠️ **破壞性變更檢查清單**:
- [ ] 刪除資料表/欄位前確認無程式碼引用
- [ ] 修改欄位型別前評估資料轉換影響（例如 `string` → `int`）
- [ ] 新增 `NOT NULL` 欄位時必須提供預設值或遷移腳本填充資料
- [ ] 修改主鍵/外鍵前確認無級聯影響

✅ **安全遷移實踐**:
- 在 Staging 環境先執行遷移測試
- 保留遷移回滾腳本（`dotnet ef migrations remove`）
- 大規模資料變更使用批次處理（避免 Lock Table）
- 重要遷移前手動備份資料庫快照

## 憲法檢查

*檢查閘門：必須在 Phase 0 研究前通過，並在 Phase 1 設計後重新檢查。*

### 原則 I：程式碼品質與可維護性 ✅ 通過
- 採用 Clean Architecture（Domain → Application → Infrastructure → API），依賴倒置原則確保核心業務邏輯不依賴外部框架
- Value Objects（Email, Password, Username）封裝驗證邏輯，提升可維護性
- 不使用 AutoMapper，手動映射 DTO 避免隱式複雜度
- SOLID 原則透過依賴注入與介面導向設計應用

### 原則 II：測試驅動開發 (TDD) ✅ 通過
- 強制 TDD 流程，測試覆蓋率目標 >80%
- 測試策略涵蓋單元測試（xUnit + Moq）、整合測試（Testcontainers + 真實 PostgreSQL）、API 契約測試（OpenAPI 驗證）
- 所有 Value Objects 與業務規則必須先寫測試
- 遵循紅燈-綠燈-重構循環，確保程式碼品質

### 原則 III：使用者體驗一致性 ✅ 通過
- RESTful API 設計遵循一致的回應格式（`{success, data/error}`）
- 錯誤代碼標準化（EMAIL_ALREADY_EXISTS, INVALID_CREDENTIALS 等）
- 所有錯誤訊息使用繁體中文，timestamp + path 便於問題追蹤
- JWT 過期處理明確（15 分鐘 Access Token + 7 天 Refresh Token）
- 正確的 HTTP 狀態碼（200、201、400、401、404、500）

### 原則 IV：效能與可擴展性 ✅ 通過
- Snowflake ID 提供時間有序 + 分散式友善特性，支援水平擴展
- 資料庫索引策略（Email unique, Token unique, (UserId, ExpiresAt) composite）確保查詢效能
- bcrypt work factor 12 在安全性與效能間取得平衡（~300ms）
- JWT 無狀態設計避免伺服器端 session 儲存，p95 目標 <200ms
- I/O 密集任務採用非同步操作（資料庫、密碼雜湊）

### 原則 V：可觀測性與除錯 ✅ 通過
- Serilog 結構化日誌（JSON 格式），記錄 UserId、RequestId、執行時間、錯誤堆疊
- 每個 API 回應包含 timestamp + path，便於問題追蹤
- JWT Claims 包含 UserId + Email 便於追蹤
- 整合測試使用 Testcontainers，確保與正式環境行為一致
- 健康檢查端點反映服務就緒與存活狀態

### 文件語言要求 ✅ 通過
- 所有規格文件（spec.md, research.md, data-model.md, plan.md, quickstart.md）、API 文件（OpenAPI）、錯誤訊息、註解均使用**繁體中文**
- 程式碼識別符號使用英文（符合 C# 慣例）
- 每種文件類型保持一致的語言使用，避免混用

**檢查結果**：✅ 所有原則均已滿足 - 進入 Phase 0

## 專案結構

### 文件（此功能）

```text
specs/001-member-service/
├── plan.md              # 本檔案（/speckit.plan 指令輸出）
├── research.md          # Phase 0 輸出（/speckit.plan 指令）
├── data-model.md        # Phase 1 輸出（/speckit.plan 指令）
├── quickstart.md        # Phase 1 輸出（快速開始指南）
├── contracts/           # Phase 1 輸出（/speckit.plan 指令）
│   └── openapi.yaml     # RESTful API 合約
└── tasks.md             # Phase 2 輸出（/speckit.tasks 指令 - 非 /speckit.plan 建立）
```

### 文件內容指引

為確保新增的文件資料夾（docs/, scripts/, .github/）內容一致且完整，以下提供各文件的建議內容大綱：

**docs/architecture.md** - 架構說明文件
- Clean Architecture 四層架構說明（Domain/Application/Infrastructure/API）
- 層級之間的依賴關係圖（使用 Mermaid 或 PlantUML）
- 主要設計決策記錄：
  - Snowflake ID 選擇理由（vs GUID）
  - bcrypt + snowflakeId 組合雜湊策略
  - JWT HS256 vs RS256 選擇原因
  - 不使用 AutoMapper 的考量
- 資料流程圖（API → Application → Domain → Infrastructure）
- 關鍵設計模式應用（Repository, Value Object, Domain Exception）

**docs/api-guide.md** - API 使用指南
- 環境設定（Base URL, 環境變數）
- 驗證機制說明（JWT Bearer Token 使用方式）
- API 端點完整範例（包含 Request/Response）：
  - POST /api/auth/register（註冊範例）
  - POST /api/auth/login（登入範例）
  - POST /api/auth/refresh（更新 Token 範例）
  - GET /api/users/{id}（查詢使用者範例）
  - PUT /api/users/{id}（更新資料範例）
  - PUT /api/users/{id}/password（變更密碼範例）
- 錯誤處理說明（錯誤代碼對照表）
- 常見使用情境（註冊後自動登入、Token 過期處理）
- Postman Collection / curl 範例

**docs/deployment.md** - 部署指南
- 環境需求（.NET 10, PostgreSQL 16）
- 環境變數配置清單：
  - DB_CONNECTION_STRING（資料庫連線字串）
  - JWT_SECRET_KEY（JWT 密鑰）
  - JWT_ISSUER / JWT_AUDIENCE
  - BCRYPT_WORK_FACTOR
  - SNOWFLAKE_WORKER_ID / SNOWFLAKE_DATACENTER_ID
- Docker 部署步驟（docker-compose.yml 使用說明）
- Kubernetes 部署範例（Deployment + Service + ConfigMap + Secret）
- 資料庫遷移執行步驟（dotnet ef database update）
- 健康檢查端點設定（/health/ready, /health/live）
- 監控與日誌配置（Serilog 輸出設定）
- Blue-Green / Rolling Update 策略說明

**scripts/build.sh** - Linux/macOS 建置腳本
```bash
#!/bin/bash
# 清理舊建置
dotnet clean
# 還原套件
dotnet restore
# 建置專案（Release 模式）
dotnet build --configuration Release --no-restore
# 執行單元測試
dotnet test --no-build --configuration Release --logger "console;verbosity=detailed"
# 建置 Docker 映像（可選）
# docker build -t memberservice:latest .
```

**scripts/build.ps1** - Windows 建置腳本
```powershell
# 清理舊建置
dotnet clean
# 還原套件
dotnet restore
# 建置專案（Release 模式）
dotnet build --configuration Release --no-restore
# 執行單元測試
dotnet test --no-build --configuration Release --logger "console;verbosity=detailed"
# 建置 Docker 映像（可選）
# docker build -t memberservice:latest .
```

**scripts/init-db.sql** - PostgreSQL 初始化腳本
```sql
-- 建立資料庫（如果不存在）
CREATE DATABASE memberservice_dev;

-- 建立使用者與授權（本地開發用）
CREATE USER memberservice WITH PASSWORD 'Dev@Password123';
GRANT ALL PRIVILEGES ON DATABASE memberservice_dev TO memberservice;

-- 啟用 UUID 擴充功能（如未來需要）
-- CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- 建議：正式環境不使用此腳本，應透過 EF Core Migrations 建立結構
```

**scripts/run-tests.sh** - 測試執行腳本
```bash
#!/bin/bash
# 執行所有測試（包含整合測試）
dotnet test --configuration Debug --logger "console;verbosity=normal"

# 產生測試覆蓋率報告（需安裝 coverlet）
# dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:CoverletOutput=./coverage/

# 執行特定測試類別
# dotnet test --filter "FullyQualifiedName~MemberService.Domain.Tests"
```

**.github/workflows/build.yml** - CI 建置工作流程
```yaml
name: Build

on:
  push:
    branches: [ main, develop, '**-member-service' ]
  pull_request:
    branches: [ main, develop ]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - name: Restore dependencies
        run: dotnet restore
      - name: Build
        run: dotnet build --configuration Release --no-restore
      - name: Test
        run: dotnet test --no-build --configuration Release
```

**.github/workflows/test.yml** - CI 測試工作流程
```yaml
name: Test

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  test:
    runs-on: ubuntu-latest
    services:
      postgres:
        image: postgres:16-alpine
        env:
          POSTGRES_USER: memberservice
          POSTGRES_PASSWORD: Test@Password123
          POSTGRES_DB: memberservice_test
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5
        ports:
          - 5432:5432
    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - name: Run tests
        run: dotnet test --configuration Debug --logger "trx;LogFileName=test-results.trx"
        env:
          DB_CONNECTION_STRING: "Host=localhost;Port=5432;Database=memberservice_test;Username=memberservice;Password=Test@Password123"
      - name: Upload test results
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: test-results
          path: '**/TestResults/*.trx'
```

這些內容指引確保實作者在建立文件時有明確的方向，避免內容不一致或遺漏關鍵資訊。

### 原始碼（儲存庫根目錄）

**專案組織結構**：所有 MemberService 相關檔案集中在單一根目錄 `MemberService/` 中，包含：
- 解決方案檔案（.sln）
- 所有專案（API、Application、Domain、Infrastructure、測試專案）
- Docker 相關檔案（Dockerfile、docker-compose.yml）
- 建置與部署文檔（README.md、建置腳本）
- 設定檔與環境變數範本

```text
MemberService/                              # 服務根目錄（所有內容集中於此）
├── MemberService.sln                       # Visual Studio 解決方案檔案
├── README.md                               # 專案說明文件
├── .gitignore                              # Git 忽略規則
├── .editorconfig                           # 編輯器設定
├── docker-compose.yml                      # Docker Compose 設定
├── docker-compose.override.yml             # 本地開發覆寫設定
├── Dockerfile                              # 多階段建置 Dockerfile
├── .dockerignore                           # Docker 忽略規則
├── global.json                             # .NET SDK 版本鎖定
│
├── docs/                                   # 文件資料夾
│   ├── architecture.md                     # 架構說明
│   ├── api-guide.md                        # API 使用指南
│   └── deployment.md                       # 部署指南
│
├── scripts/                                # 建置與部署腳本
│   ├── build.sh                            # Linux/macOS 建置腳本
│   ├── build.ps1                           # Windows 建置腳本
│   ├── init-db.sql                         # PostgreSQL 初始化腳本
│   └── run-tests.sh                        # 測試執行腳本
│
├── .github/                                # GitHub 相關設定
│   └── workflows/                          # CI/CD 工作流程
│       ├── build.yml                       # 建置工作流程
│       └── test.yml                        # 測試工作流程
│
├── src/                                    # 原始碼目錄
│   ├── MemberService.API/                  # API 層（HTTP 端點）
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs           # 驗證端點（註冊/登入/登出）
│   │   │   └── UsersController.cs          # 使用者端點（查詢/更新資料）
│   │   ├── Middlewares/
│   │   │   ├── ExceptionHandlingMiddleware.cs   # 全域例外處理
│   │   │   └── RequestLoggingMiddleware.cs      # 請求日誌
│   │   ├── Program.cs                      # 應用程式進入點
│   │   ├── appsettings.json                # 設定檔
│   │   ├── appsettings.Development.json    # 開發環境設定
│   │   └── MemberService.API.csproj        # 專案檔案（目標框架：net10.0）
│   │
│   ├── MemberService.Application/          # 應用層（Use Cases）
│   │   ├── DTOs/
│   │   │   ├── Auth/
│   │   │   │   ├── RegisterRequest.cs           # 註冊請求 DTO
│   │   │   │   ├── LoginRequest.cs              # 登入請求 DTO
│   │   │   │   ├── RefreshTokenRequest.cs       # 更新 Token 請求 DTO
│   │   │   │   ├── AuthResponse.cs              # 驗證回應 DTO
│   │   │   │   └── TokenValidationResponse.cs   # Token 驗證回應 DTO
│   │   │   └── Users/
│   │   │       ├── UserProfileResponse.cs       # 使用者資料回應 DTO
│   │   │       ├── UpdateProfileRequest.cs      # 更新資料請求 DTO
│   │   │       └── ChangePasswordRequest.cs     # 變更密碼請求 DTO
│   │   ├── Services/
│   │   │   ├── IAuthService.cs                  # 驗證服務介面
│   │   │   ├── AuthService.cs                   # 驗證服務實作
│   │   │   ├── IUserService.cs                  # 使用者服務介面
│   │   │   └── UserService.cs                   # 使用者服務實作
│   │   ├── Validators/
│   │   │   ├── RegisterRequestValidator.cs      # 註冊請求驗證器
│   │   │   ├── LoginRequestValidator.cs         # 登入請求驗證器
│   │   │   └── UpdateProfileRequestValidator.cs # 更新資料驗證器
│   │   └── MemberService.Application.csproj
│   │
│   ├── MemberService.Domain/               # 領域層（核心業務邏輯）
│   │   ├── Entities/
│   │   │   ├── User.cs                          # 使用者實體
│   │   │   └── RefreshToken.cs                  # Refresh Token 實體
│   │   ├── ValueObjects/
│   │   │   ├── Email.cs                         # Email Value Object
│   │   │   ├── Password.cs                      # Password Value Object
│   │   │   └── Username.cs                      # Username Value Object
│   │   ├── Interfaces/
│   │   │   ├── IUserRepository.cs               # 使用者儲存庫介面
│   │   │   ├── IRefreshTokenRepository.cs       # Refresh Token 儲存庫介面
│   │   │   └── IPasswordHasher.cs               # 密碼雜湊介面
│   │   ├── Exceptions/
│   │   │   ├── DomainException.cs               # 領域例外基底類別
│   │   │   ├── EmailAlreadyExistsException.cs   # Email 重複例外
│   │   │   └── InvalidCredentialsException.cs   # 登入失敗例外
│   │   └── MemberService.Domain.csproj
│   │
│   └── MemberService.Infrastructure/       # 基礎設施層（外部依賴）
│       ├── Persistence/
│       │   ├── MemberDbContext.cs               # EF Core DbContext
│       │   ├── Configurations/
│       │   │   ├── UserConfiguration.cs         # User 實體設定
│       │   │   └── RefreshTokenConfiguration.cs # RefreshToken 實體設定
│       │   ├── Repositories/
│       │   │   ├── UserRepository.cs            # 使用者儲存庫實作
│       │   │   └── RefreshTokenRepository.cs    # Refresh Token 儲存庫實作
│       │   └── Migrations/
│       │       └── 20251118000000_InitialCreate.cs # 初始資料庫遷移
│       ├── Security/
│       │   ├── BCryptPasswordHasher.cs          # bcrypt 密碼雜湊實作
│       │   ├── JwtTokenGenerator.cs             # JWT 產生器
│       │   └── RefreshTokenGenerator.cs         # Refresh Token 產生器
│       ├── IdGeneration/
│       │   └── SnowflakeIdGenerator.cs          # Snowflake ID 產生器
│       └── MemberService.Infrastructure.csproj
│
└── tests/                                  # 測試專案目錄
├── MemberService.Domain.Tests/                  # 領域層單元測試
│   ├── Entities/
│   │   ├── UserTests.cs
│   │   └── RefreshTokenTests.cs
│   ├── ValueObjects/
│   │   ├── EmailTests.cs
│   │   ├── PasswordTests.cs
│   │   └── UsernameTests.cs
│   └── MemberService.Domain.Tests.csproj
│
├── MemberService.Application.Tests/             # 應用層單元測試
│   ├── Services/
│   │   ├── AuthServiceTests.cs
│   │   └── UserServiceTests.cs
│   ├── Validators/
│   │   ├── RegisterRequestValidatorTests.cs
│   │   └── UpdateProfileRequestValidatorTests.cs
│   └── MemberService.Application.Tests.csproj
│
├── MemberService.Infrastructure.Tests/          # 基礎設施層單元測試
│   ├── Security/
│   │   ├── BCryptPasswordHasherTests.cs
│   │   └── JwtTokenGeneratorTests.cs
│   ├── IdGeneration/
│   │   └── SnowflakeIdGeneratorTests.cs
│   └── MemberService.Infrastructure.Tests.csproj
│
└── MemberService.IntegrationTests/              # 整合測試（Testcontainers）
    ├── API/
    │   ├── AuthControllerTests.cs
    │   └── UsersControllerTests.cs
    ├── Persistence/
    │   ├── UserRepositoryTests.cs
    │   └── RefreshTokenRepositoryTests.cs
    └── TestFixtures/
        └── PostgreSqlContainerFixture.cs        # Testcontainers 設定
```

**結構決策**：MemberService 採用 Clean Architecture 分層結構（4 個專案），符合依賴倒置原則：
- **Domain**: 核心業務邏輯，無外部依賴（純 C# 類別）
- **Application**: 應用用例，依賴 Domain 介面（不依賴具體實作）
- **Infrastructure**: 外部依賴實作（EF Core, BCrypt, JWT, Snowflake ID）
- **API**: HTTP 端點與中介軟體，依賴 Application 服務

測試結構鏡像原始碼結構，整合測試使用 Testcontainers 確保與正式環境一致性。

## 複雜度追蹤

> **僅在憲法檢查發現違規需要說明時填寫**

| 違規項目 | 需要原因 | 拒絕簡化替代方案的理由 |
|---------|---------|---------------------|
| 無 | 無 | 無 |

**無違規項目** - 所有設計決策符合 Constitution 的 5 項核心原則與繁體中文文件要求。

**架構說明**：四層 Clean Architecture（API、Application、Domain、Infrastructure）是此規模微服務的標準做法。分層架構提供：
- 明確的依賴方向（Infrastructure → Application → Domain）
- 高度可測試性（Domain 與 Application 可在無基礎設施的情況下測試）
- 未來變更的彈性（更換 PostgreSQL 為其他資料庫而不影響業務邏輯）
- 團隊協作（不同層級可平行開發）

此架構並非過度複雜，而是可維護微服務的業界最佳實踐。
