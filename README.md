# Auction Service - Member Service API

Member Service 是一個完整的身份驗證和用戶管理微服務，提供用戶註冊、登錄、令牌刷新和個人資料管理功能。

## 快速開始

### 先決條件

- **.NET 9** SDK (LTS - 長期支持)
- **PostgreSQL 15+** （本地或 Docker）
- **Docker**（可選，用於本地 PostgreSQL）

### 設置步驟

#### 1. 配置數據庫連接

編輯 `src/MemberService/MemberService.API/appsettings.Development.json`：

```json
{
  "Database": {
    "ConnectionString": "Host=localhost;Port=5432;Database=auction_member_dev;Username=postgres;Password=your_password"
  }
}
```

#### 2. 啟動 PostgreSQL（使用 Docker）

```bash
docker run --name postgres-auction \
  -e POSTGRES_PASSWORD=your_password \
  -e POSTGRES_DB=auction_member_dev \
  -p 5432:5432 \
  -d postgres:15
```

#### 3. 安裝依賴項

```bash
cd src/MemberService/MemberService.API
dotnet restore
```

#### 4. 運行數據庫遷移

```bash
dotnet ef database update --startup-project src/MemberService/MemberService.API
```

#### 5. 啟動服務

```bash
cd src/MemberService/MemberService.API
dotnet run
```

服務將在 `http://localhost:5000` 啟動。

### API 文檔

- **Swagger UI**: `http://localhost:5000/swagger`
- **Health Check**: `http://localhost:5000/api/health`
- **Readiness Check**: `http://localhost:5000/api/ready`

## API 端點

### 身份驗證

#### 用戶註冊
```http
POST /api/auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePassword123!",
  "username": "John Doe"
}
```

**成功響應**（201 Created）:
```json
{
  "userId": 123456789,
  "email": "user@example.com",
  "username": "John Doe",
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "base64_encoded_random_token",
  "expiresIn": 900
}
```

#### 用戶登錄
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}
```

**成功響應**（200 OK）:
```json
{
  "userId": 123456789,
  "email": "user@example.com",
  "username": "John Doe",
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "base64_encoded_random_token",
  "expiresIn": 900
}
```

#### 刷新令牌
```http
POST /api/auth/refresh
Content-Type: application/json

{
  "refreshToken": "base64_encoded_random_token"
}
```

**成功響應**（200 OK）:
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "base64_encoded_new_random_token",
  "expiresIn": 900
}
```

#### 用戶登出
```http
POST /api/auth/logout
Authorization: Bearer {accessToken}
```

**成功響應**（204 No Content）

### 用戶管理

#### 獲取自己的用戶檔案
```http
GET /api/users/me
Authorization: Bearer {accessToken}
```

**成功響應**（200 OK）:
```json
{
  "userId": 123456789,
  "email": "user@example.com",
  "username": "John Doe",
  "createdAt": "2024-01-15T10:30:00Z",
  "updatedAt": "2024-01-15T10:30:00Z"
}
```

#### 獲取其他用戶的公開檔案
```http
GET /api/users/{userId}
Authorization: Bearer {accessToken}
```

**成功響應**（200 OK）:
```json
{
  "userId": 987654321,
  "username": "Jane Doe",
  "createdAt": "2024-01-10T15:20:00Z"
}
```

**注意**: 此端點的響應被緩存 5 分鐘。

#### 更新用戶檔案
```http
PUT /api/users/{userId}
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "username": "Jane Smith",
  "email": "newemail@example.com"
}
```

**成功響應**（200 OK）:
```json
{
  "userId": 123456789,
  "email": "newemail@example.com",
  "username": "Jane Smith",
  "createdAt": "2024-01-15T10:30:00Z",
  "updatedAt": "2024-01-15T11:45:00Z"
}
```

#### 更改密碼
```http
PUT /api/users/{userId}/password
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "currentPassword": "SecurePassword123!",
  "newPassword": "NewSecurePassword456!"
}
```

**成功響應**（204 No Content）

### 健康檢查

#### 活動性探針
```http
GET /api/health
```

**成功響應**（200 OK）:
```json
{
  "status": "healthy",
  "service": "MemberService",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

#### 就緒探針
```http
GET /api/ready
```

**成功響應**（200 OK）:
```json
{
  "status": "ready",
  "database": "connected"
}
```

**服務未就緒**（503 Service Unavailable）:
```json
{
  "status": "not ready",
  "database": "disconnected"
}
```

## 錯誤響應

### 驗證錯誤（400 Bad Request）
```json
{
  "errors": {
    "Email": ["Invalid email format"],
    "Password": ["Password must be at least 8 characters long"]
  }
}
```

### 未授權（401 Unauthorized）
```json
{
  "message": "Invalid credentials"
}
```

### 禁止（403 Forbidden）
```json
{
  "message": "Access denied"
}
```

### 未找到（404 Not Found）
```json
{
  "message": "User not found"
}
```

### 衝突（409 Conflict）
```json
{
  "message": "Email already in use"
}
```

### 內部服務器錯誤（500 Internal Server Error）
```json
{
  "message": "An unexpected error occurred"
}
```

## 身份驗證

該服務使用 **JWT（JSON Web Token）** 進行身份驗證。

1. 調用 `/api/auth/login` 獲取訪問令牌
2. 在受保護端點的 `Authorization` 標頭中使用該令牌：
   ```
   Authorization: Bearer {accessToken}
   ```

## 安全特性

- ✅ **密碼哈希**: 使用 bcrypt 進行安全密碼存儲
- ✅ **JWT 令牌**: 有 15 分鐘有效期的 JWT 訪問令牌
- ✅ **刷新令牌**: 長期刷新令牌用於令牌輪換
- ✅ **CORS 支持**: 配置用於跨域請求
- ✅ **結構化日誌**: 使用 Serilog 記錄所有身份驗證事件
- ✅ **響應緩存**: 公開端點的緩存以改進性能
- ✅ **連接池**: 優化的數據庫連接管理

## 架構

Member Service 采用分層架構：

```
MemberService.API/
├── Controllers/          # HTTP 端點
├── Middlewares/          # 請求/響應中間件
└── Properties/           # 項目設置

MemberService.Application/
├── DTOs/                 # 數據傳輸對象
├── Services/             # 業務邏輯（AuthService、UserService）
└── Validators/           # FluentValidation 驗證器

MemberService.Domain/
├── Entities/             # User、RefreshToken 域實體
├── Exceptions/           # 自定義異常
├── Interfaces/           # 存儲庫和服務接口
└── ValueObjects/         # Email 值對象

MemberService.Infrastructure/
├── IdGeneration/         # ID 生成器（Snowflake）
├── Persistence/          # DbContext 和 EF 配置
└── Security/             # JWT 令牌提供程序
```

## 測試

運行所有測試：
```bash
dotnet test
```

生成覆蓋率報告：
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## 性能指標

- **健康檢查**: < 100ms
- **登錄**: < 500ms
- **個人資料獲取**: < 100ms（無緩存）/ < 10ms（有緩存）
- **數據庫連接**: 連接池 5-20
- **存取令牌有效期**: 15 分鐘
- **刷新令牌有效期**: 7 天

## 已知限制

- **電子郵件驗證**: 不包括電子郵件確認機制（超出範圍 - 參見規範）
- **社交登錄**: 不支持 OAuth/SSO 集成
- **MFA**: 不支持多因素身份驗證

## 故障排除

### 數據庫連接失敗

檢查 PostgreSQL 是否正在運行：
```bash
# Docker 容器
docker ps | grep postgres

# 或檢查連接字符串
echo "Host=localhost;Port=5432;..." | dotnet user-secrets set "Database:ConnectionString"
```

### JWT 令牌過期

使用刷新令牌獲取新的訪問令牌：
```bash
POST /api/auth/refresh
{
  "refreshToken": "your_refresh_token"
}
```

### Swagger UI 不可訪問

確保在 Program.cs 中啟用了中間件：
```csharp
app.UseSwagger();
app.UseSwaggerUI();
```

## 生產部署

### Docker 部署

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY bin/Release/net9.0/publish .
ENTRYPOINT ["dotnet", "MemberService.API.dll"]
```

### Kubernetes 部署

使用提供的健康檢查端點：

```yaml
livenessProbe:
  httpGet:
    path: /api/health
    port: 8080
  initialDelaySeconds: 10
  periodSeconds: 10

readinessProbe:
  httpGet:
    path: /api/ready
    port: 8080
  initialDelaySeconds: 5
  periodSeconds: 5
```

## 貢獻

遵循現有代碼風格並運行：
```bash
dotnet format
```

## 許可證

專有 - Auction Service 項目

## 支持

有問題？檢查 `/specs/001-member-service/` 目錄中的完整規範和設計文檔。
