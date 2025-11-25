# Implementation Plan: Member Service

**Branch**: `001-member-service` | **Date**: 2025-11-18 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-member-service/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Member Service 提供使用者註冊、登入、身份驗證與個人資料管理功能，是拍賣系統的核心身份驗證服務。採用 ASP.NET Core 9 Web API + PostgreSQL 16 + Entity Framework Core 9 Code-First 架構，使用 Clean Architecture 分層設計（Domain/Application/Infrastructure/API）。

技術重點：
- **Snowflake ID**: 使用 IdGen 套件產生 64-bit 分散式唯一識別碼，取代 GUID（空間節省 50%，時間有序）
- **密碼安全**: bcrypt(password + snowflakeId) 組合，work factor 12，防禦深度策略
- **JWT 驗證**: HS256 對稱金鑰演算法，15 分鐘 Access Token + 7 天 Refresh Token
- **無 AutoMapper**: 使用 POCO 手動映射 DTO，提升效能與可讀性
- **Controller-based API**: 不使用 Minimal APIs，採用傳統控制器設計
- **TDD 驅動**: xUnit + Moq + FluentAssertions + Testcontainers，目標覆蓋率 >80%

## Technical Context

**Language/Version**: ASP.NET Core 9, C# 13 (.NET 9 LTS)  
**Primary Dependencies**:
- IdGen 3.x (Snowflake ID 產生器)
- BCrypt.Net-Next 4.0.3 (密碼雜湊)
- System.IdentityModel.Tokens.Jwt 7.0.3 (JWT 驗證)
- Npgsql.EntityFrameworkCore.PostgreSQL 9.0 (PostgreSQL 驅動)
- FluentValidation.AspNetCore 11.3.0 (輸入驗證)
- Serilog.AspNetCore 8.0 (結構化日誌)

**Storage**: 
- **本地開發**: PostgreSQL 16 本地安裝或 Docker 容器
- **正式環境**: Azure Database for PostgreSQL / AWS RDS PostgreSQL（雲端託管）
- **資料庫建置**: EF Core Code-First Migrations（資料庫結構由程式碼自動產生）

**Testing**: xUnit 2.6, Moq 4.20, FluentAssertions 6.12, Testcontainers.PostgreSql 3.6  
**Target Platform**: Linux/Windows Server, Docker 容器部署  
**Project Type**: Web API (Clean Architecture - 4 層專案結構)  
**Performance Goals**:
- JWT 驗證延遲 <50ms p95
- API 端點回應時間 <200ms p95
- bcrypt 密碼雜湊 ~250-350ms（work factor 12）
- Snowflake ID 產生 <1ms

**Constraints**:
- 必須採用 TDD 開發流程（測試覆蓋率 >80%）
- 不使用 AutoMapper（手動 POCO 映射）
- 不使用 Minimal APIs（Controller-based）
- API Gateway 使用 YARP（非 Ocelot）
- 錯誤訊息與文件必須使用繁體中文

**Scale/Scope**:
- 預估初期使用者數：10,000 人
- 同時線上使用者：500-1,000 人
- API 端點數量：8 個（4 個公開 + 4 個私有）
- 資料庫資料表：2 個（Users + RefreshTokens）
- 預估程式碼規模：~3,000-5,000 LOC（不含測試）

## Database Strategy

### 開發環境 (Local Development)

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

**資料庫初始化流程** (EF Core Code-First):
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

### 正式環境 (Production)

**雲端資料庫服務** (選擇其一):

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

**連線字串配置** (透過環境變數注入):
```bash
# Azure 範例
DB_CONNECTION_STRING="Host=memberservice-prod.postgres.database.azure.com;Port=5432;Database=memberservice_prod;Username=adminuser;Password=${PROD_DB_PASSWORD};SslMode=Require"

# AWS RDS 範例
DB_CONNECTION_STRING="Host=memberservice-prod.abc123.us-east-1.rds.amazonaws.com;Port=5432;Database=memberservice_prod;Username=adminuser;Password=${PROD_DB_PASSWORD};SslMode=Require"
```

**安全設定**:
- ✅ **強制 SSL/TLS 連線** (`SslMode=Require`)
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

2. **CI/CD Pipeline** (Azure DevOps / GitHub Actions):
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

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### 原則 1: Code Quality & Maintainability
✅ **符合**: 採用 Clean Architecture（Domain → Application → Infrastructure → API），依賴倒置原則確保核心業務邏輯不依賴外部框架。Value Objects（Email, Password, Username）封裝驗證邏輯，提升可維護性。不使用 AutoMapper，手動映射 DTO 避免隱式複雜度。

### 原則 2: Test-Driven Development (TDD)
✅ **符合**: 強制 TDD 流程，測試覆蓋率目標 >80%。測試策略涵蓋單元測試（xUnit + Moq）、整合測試（Testcontainers + 真實 PostgreSQL）、API 契約測試（OpenAPI 驗證）。所有 Value Objects 與業務規則必須先寫測試。

### 原則 3: User Experience Consistency
✅ **符合**: RESTful API 設計遵循一致的回應格式（`{success, data/error}`），錯誤代碼標準化（EMAIL_ALREADY_EXISTS, INVALID_CREDENTIALS 等）。所有錯誤訊息使用繁體中文，timestamp + path 便於問題追蹤。JWT 過期處理明確（15 分鐘 Access Token + 7 天 Refresh Token）。

### 原則 4: Performance & Scalability
✅ **符合**: Snowflake ID 提供時間有序 + 分散式友善特性，支援水平擴展。資料庫索引策略（Email unique, Token unique, (UserId, ExpiresAt) composite）確保查詢效能。bcrypt work factor 12 在安全性與效能間取得平衡（~300ms）。JWT 無狀態設計避免伺服器端 session 儲存，p95 目標 <200ms。

### 原則 5: Observability & Debugging
✅ **符合**: Serilog 結構化日誌（JSON 格式），記錄 UserId、RequestId、執行時間、錯誤堆疊。每個 API 回應包含 timestamp + path。JWT Claims 包含 UserId + Email 便於追蹤。整合測試使用 Testcontainers，確保與正式環境行為一致。

### Documentation Language Requirement
✅ **符合**: 所有規格文件（spec.md, research.md, data-model.md, plan.md, quickstart.md）、API 文件（OpenAPI）、錯誤訊息、註解均使用**繁體中文**。程式碼識別符號使用英文（符合 C# 慣例）。

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/
├── MemberService/
│   ├── MemberService.Domain/                    # 領域層（核心業務邏輯）
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
│   │   └── Exceptions/
│   │       ├── DomainException.cs               # 領域例外基底類別
│   │       ├── EmailAlreadyExistsException.cs   # Email 重複例外
│   │       └── InvalidCredentialsException.cs   # 登入失敗例外
│   │
│   ├── MemberService.Application/               # 應用層（Use Cases）
│   │   ├── DTOs/
│   │   │   ├── Auth/
│   │   │   │   ├── RegisterRequest.cs           # 註冊請求 DTO
│   │   │   │   ├── LoginRequest.cs              # 登入請求 DTO
│   │   │   │   ├── RefreshTokenRequest.cs       # 更新 Token 請求 DTO
│   │   │   │   └── AuthResponse.cs              # 驗證回應 DTO
│   │   │   └── Users/
│   │   │       ├── UserProfileResponse.cs       # 使用者資料回應 DTO
│   │   │       ├── UpdateProfileRequest.cs      # 更新資料請求 DTO
│   │   │       └── ChangePasswordRequest.cs     # 變更密碼請求 DTO
│   │   ├── Services/
│   │   │   ├── IAuthService.cs                  # 驗證服務介面
│   │   │   ├── AuthService.cs                   # 驗證服務實作
│   │   │   ├── IUserService.cs                  # 使用者服務介面
│   │   │   └── UserService.cs                   # 使用者服務實作
│   │   └── Validators/
│   │       ├── RegisterRequestValidator.cs      # 註冊請求驗證器
│   │       ├── LoginRequestValidator.cs         # 登入請求驗證器
│   │       └── UpdateProfileRequestValidator.cs # 更新資料驗證器
│   │
│   ├── MemberService.Infrastructure/            # 基礎設施層（外部依賴）
│   │   ├── Persistence/
│   │   │   ├── MemberDbContext.cs               # EF Core DbContext
│   │   │   ├── Configurations/
│   │   │   │   ├── UserConfiguration.cs         # User 實體設定
│   │   │   │   └── RefreshTokenConfiguration.cs # RefreshToken 實體設定
│   │   │   ├── Repositories/
│   │   │   │   ├── UserRepository.cs            # 使用者儲存庫實作
│   │   │   │   └── RefreshTokenRepository.cs    # Refresh Token 儲存庫實作
│   │   │   └── Migrations/
│   │   │       └── 20251118000000_InitialCreate.cs # 初始資料庫遷移
│   │   ├── Security/
│   │   │   ├── BCryptPasswordHasher.cs          # bcrypt 密碼雜湊實作
│   │   │   ├── JwtTokenGenerator.cs             # JWT 產生器
│   │   │   └── RefreshTokenGenerator.cs         # Refresh Token 產生器
│   │   └── IdGeneration/
│   │       └── SnowflakeIdGenerator.cs          # Snowflake ID 產生器
│   │
│   └── MemberService.API/                       # API 層（HTTP 端點）
│       ├── Controllers/
│       │   ├── AuthController.cs                # 驗證端點（註冊/登入/登出）
│       │   └── UsersController.cs               # 使用者端點（查詢/更新資料）
│       ├── Middlewares/
│       │   ├── ExceptionHandlingMiddleware.cs   # 全域例外處理
│       │   └── RequestLoggingMiddleware.cs      # 請求日誌
│       ├── Program.cs                           # 應用程式進入點
│       ├── appsettings.json                     # 設定檔
│       ├── appsettings.Development.json         # 開發環境設定
│       └── Dockerfile                           # Docker 映像定義

tests/
├── MemberService.Domain.Tests/                  # 領域層單元測試
│   ├── Entities/
│   │   ├── UserTests.cs
│   │   └── RefreshTokenTests.cs
│   └── ValueObjects/
│       ├── EmailTests.cs
│       ├── PasswordTests.cs
│       └── UsernameTests.cs
│
├── MemberService.Application.Tests/             # 應用層單元測試
│   ├── Services/
│   │   ├── AuthServiceTests.cs
│   │   └── UserServiceTests.cs
│   └── Validators/
│       ├── RegisterRequestValidatorTests.cs
│       └── UpdateProfileRequestValidatorTests.cs
│
├── MemberService.Infrastructure.Tests/          # 基礎設施層單元測試
│   ├── Security/
│   │   ├── BCryptPasswordHasherTests.cs
│   │   └── JwtTokenGeneratorTests.cs
│   └── IdGeneration/
│       └── SnowflakeIdGeneratorTests.cs
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

**Structure Decision**: 採用 Clean Architecture 分層結構（4 個專案），符合依賴倒置原則：
- **Domain**: 核心業務邏輯，無外部依賴（純 C# 類別）
- **Application**: 應用用例，依賴 Domain 介面（不依賴具體實作）
- **Infrastructure**: 外部依賴實作（EF Core, BCrypt, JWT, Snowflake ID）
- **API**: HTTP 端點與中介軟體，依賴 Application 服務

測試結構鏡像原始碼結構，整合測試使用 Testcontainers 確保與正式環境一致性。

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

**無違規項目** - 所有設計決策符合 Constitution 的 5 項核心原則與繁體中文文件要求。
