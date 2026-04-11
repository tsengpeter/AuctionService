# 快速開始：Member 模組開發

**Branch**: `002-member-module`  
**Date**: 2026-04-08

---

## 前置條件

```bash
# 啟動 PostgreSQL（Docker Compose）
docker compose up -d

# 確認 .env 或 appsettings.Development.json 有以下環境變數
# JWT_SECRET=<至少 32 字元的密鑰>
# ConnectionStrings__MemberDb=Host=localhost;Port=5432;...
```

---

## 安裝 NuGet 套件

```bash
# BCrypt.Net-Next（密碼雜湊）
dotnet add src/Modules/Member/Member.csproj package BCrypt.Net-Next

# 其餘套件（MediatR、FluentValidation、EF Core）已在 Shared/Api 專案安裝
```

---

## 執行 Migration

```bash
# 建立 Migration（需在 repo root 執行）
dotnet ef migrations add AddMemberModuleSchema \
  --project src/Modules/Member/Member.csproj \
  --startup-project src/AuctionService.Api/AuctionService.Api.csproj \
  --output-dir Infrastructure/Persistence/Migrations

# 套用 Migration
dotnet ef database update \
  --project src/Modules/Member/Member.csproj \
  --startup-project src/AuctionService.Api/AuctionService.Api.csproj
```

---

## 目錄結構（預計產出）

```text
src/Modules/Member/
├── Member.csproj
├── Domain/
│   ├── MemberUser.cs
│   ├── RefreshToken.cs
│   └── MemberRole.cs
├── Application/
│   ├── Abstractions/
│   │   ├── IPasswordHasher.cs
│   │   └── IJwtTokenService.cs
│   ├── Commands/
│   │   ├── Register/
│   │   │   ├── RegisterCommand.cs
│   │   │   ├── RegisterCommandHandler.cs
│   │   │   └── RegisterCommandValidator.cs
│   │   ├── Login/
│   │   │   ├── LoginCommand.cs
│   │   │   ├── LoginCommandHandler.cs
│   │   │   └── LoginCommandValidator.cs
│   │   ├── RefreshToken/
│   │   │   ├── RefreshTokenCommand.cs
│   │   │   └── RefreshTokenCommandHandler.cs
│   │   ├── Logout/
│   │   │   ├── LogoutCommand.cs
│   │   │   └── LogoutCommandHandler.cs
│   │   ├── UpdateProfile/
│   │   │   ├── UpdateProfileCommand.cs
│   │   │   ├── UpdateProfileCommandHandler.cs
│   │   │   └── UpdateProfileCommandValidator.cs
│   │   └── ChangePassword/
│   │       ├── ChangePasswordCommand.cs
│   │       ├── ChangePasswordCommandHandler.cs
│   │       └── ChangePasswordCommandValidator.cs
│   └── Queries/
│       └── GetMe/
│           ├── GetMeQuery.cs
│           ├── GetMeQueryHandler.cs
│           └── UserDto.cs
├── Infrastructure/
│   ├── Persistence/
│   │   ├── MemberDbContext.cs
│   │   ├── Configurations/
│   │   │   ├── MemberUserConfiguration.cs
│   │   │   └── RefreshTokenConfiguration.cs
│   │   └── Migrations/
│   ├── Services/
│   │   ├── BcryptPasswordHasher.cs
│   │   └── JwtTokenService.cs
│   ├── BackgroundServices/
│   │   └── RefreshTokenCleanupService.cs
│   └── DependencyInjection.cs

src/AuctionService.Api/Controllers/
├── AuthController.cs      # POST /api/auth/register, login, refresh, logout
└── UsersController.cs     # GET/PUT /api/users/me, PUT /api/users/me/password

tests/AuctionService.UnitTests/Member/
├── Domain/
│   ├── MemberUserTests.cs
│   └── RefreshTokenTests.cs
├── Application/
│   ├── RegisterCommandHandlerTests.cs
│   ├── LoginCommandHandlerTests.cs
│   ├── RefreshTokenCommandHandlerTests.cs
│   ├── LogoutCommandHandlerTests.cs
│   ├── UpdateProfileCommandHandlerTests.cs
│   ├── ChangePasswordCommandHandlerTests.cs
│   └── GetMeQueryHandlerTests.cs

tests/AuctionService.IntegrationTests/Member/
├── AuthControllerTests.cs
└── UsersControllerTests.cs
```

---

## API 快速測試（使用 .http 檔）

```http
### 1. 註冊
POST http://localhost:5000/api/auth/register
Content-Type: application/json

{
  "email": "alice@example.com",
  "username": "alice",
  "password": "Secret1!",
  "displayName": "Alice Chen"
}

### 2. 登入
POST http://localhost:5000/api/auth/login
Content-Type: application/json

{
  "email": "alice@example.com",
  "password": "Secret1!"
}

### 3. 查詢個人資料（替換 {{token}}）
GET http://localhost:5000/api/users/me
Authorization: Bearer {{token}}

### 4. 更新個人資料
PUT http://localhost:5000/api/users/me
Authorization: Bearer {{token}}
Content-Type: application/json

{
  "username": "alice2",
  "displayName": "Alice Updated",
  "address": {
    "country": "台灣",
    "city": "台北市",
    "postalCode": "100",
    "addressLine": "中正區重慶南路一段122號"
  }
}

### 5. Refresh Token（替換 {{refreshToken}}）
POST http://localhost:5000/api/auth/refresh
Content-Type: application/json

{
  "refreshToken": "{{refreshToken}}"
}

### 6. 變更密碼
PUT http://localhost:5000/api/users/me/password
Authorization: Bearer {{token}}
Content-Type: application/json

{
  "currentPassword": "Secret1!",
  "newPassword": "NewSecret2!"
}

### 7. 登出
POST http://localhost:5000/api/auth/logout
Content-Type: application/json

{
  "refreshToken": "{{refreshToken}}"
}
```

---

## 執行測試

```bash
# 全部測試
dotnet test

# 僅 Member 模組單元測試
dotnet test tests/AuctionService.UnitTests --filter "FullyQualifiedName~Member"

# 僅 Member 模組整合測試
dotnet test tests/AuctionService.IntegrationTests --filter "FullyQualifiedName~Member"
```
