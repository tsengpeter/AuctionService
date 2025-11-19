# 研究文件：會員服務技術決策

## 概述

本文件記錄會員服務 (Member Service) 的技術研究結果，包含技術選型、最佳實踐與實作策略。

---

## 決策 1：雪花ID生成套件選擇

### 決策
使用 **IdGen** NuGet 套件實作雪花ID生成。

### 理由
1. **成熟穩定**: IdGen 是 .NET 生態系統中最成熟的雪花ID實作，超過 100 萬次下載
2. **高效能**: 單執行緒每秒可生成超過 200 萬個唯一ID
3. **易於整合**: 提供簡潔的 API，支援 Dependency Injection
4. **配置彈性**: 支援自訂 Worker ID、Datacenter ID 與 Epoch
5. **執行緒安全**: 內建執行緒同步機制，適合多執行緒環境

### 替代方案評估
- **Snowflake.Core**: 功能類似，但社群較小，文件較少
- **自行實作**: 風險高，需處理時鐘回撥、執行緒安全等複雜問題
- **GUID**: 128-bit 過大，無時間排序特性，效能較差

### 實作範例
```csharp
// Program.cs - 註冊服務
builder.Services.AddSingleton(sp => 
{
    var generatorId = new IdGenerator(0); // Worker ID = 0
    return generatorId;
});

// 使用
public class UserService
{
    private readonly IdGenerator _idGenerator;
    
    public UserService(IdGenerator idGenerator)
    {
        _idGenerator = idGenerator;
    }
    
    public long GenerateUserId()
    {
        return _idGenerator.CreateId();
    }
}
```

---

## 決策 2：密碼雜湊實作

### 決策
使用 **BCrypt.Net-Next** NuGet 套件實作 bcrypt 密碼雜湊。

### 理由
1. **業界標準**: bcrypt 是業界公認的密碼雜湊演算法
2. **自適應成本**: Work Factor 可調整，隨硬體進步保持安全性
3. **內建鹽值**: 自動產生隨機鹽值，無需手動管理
4. **抗暴力破解**: 計算成本高，有效延緩暴力破解攻擊
5. **成熟套件**: BCrypt.Net-Next 活躍維護，支援 .NET 9

### 實作策略
```csharp
public class PasswordService
{
    private const int WorkFactor = 12;
    
    public string HashPassword(string password, long snowflakeId)
    {
        // 組合密碼與雪花ID
        string combined = password + snowflakeId.ToString();
        
        // 使用 bcrypt 雜湊
        return BCrypt.Net.BCrypt.HashPassword(combined, WorkFactor);
    }
    
    public bool VerifyPassword(string password, long snowflakeId, string hash)
    {
        string combined = password + snowflakeId.ToString();
        return BCrypt.Net.BCrypt.Verify(combined, hash);
    }
}
```

### 安全增強
- **密碼 + 雪花ID 組合**: 即使資料庫洩漏，攻擊者仍需知道雪花ID才能驗證密碼
- **Work Factor 12**: 平衡安全性與效能（約 250-350ms/次）
- **自動鹽值**: bcrypt 內建隨機鹽值生成

---

## 決策 3：JWT 實作

### 決策
使用 **System.IdentityModel.Tokens.Jwt** (Microsoft 官方套件) 實作 JWT 驗證。

### 理由
1. **官方支援**: Microsoft 官方維護，與 ASP.NET Core 深度整合
2. **標準相容**: 完全符合 RFC 7519 JWT 標準
3. **安全性**: 內建防護常見攻擊（如 none algorithm attack）
4. **效能**: 針對 .NET 最佳化，驗證速度快
5. **擴展性**: 支援自訂 Claims、多種演算法

### 實作策略
```csharp
// JWT 設定 (appsettings.json)
{
  "Jwt": {
    "SecretKey": "${JWT_SECRET_KEY}", // 從環境變數讀取
    "Issuer": "AuctionService.MemberService",
    "Audience": "AuctionService",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  }
}

// JWT 服務
public class JwtService
{
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    
    public string GenerateAccessToken(long userId, string email)
    {
        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(
            securityKey, SecurityAlgorithms.HmacSha256);
        
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        
        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

### 安全考量
- **HS256 演算法**: 對稱金鑰，驗證速度快，適合內部微服務
- **密鑰管理**: 透過環境變數注入，絕不寫入程式碼或設定檔
- **短期有效**: Access Token 僅 15 分鐘，降低洩漏風險
- **Refresh Token**: 7 天有效期，支援長期登入

---

## 決策 4：資料庫設計

### 決策
使用 **Entity Framework Core 9 Code-First** 搭配 **PostgreSQL 16**。

### 理由
1. **Code-First 優勢**: 從 C# 實體定義資料庫結構，版本控制友善
2. **Migration 支援**: EF Core Migrations 管理資料庫變更歷史
3. **型別安全**: 強型別 LINQ 查詢，編譯期檢查錯誤
4. **PostgreSQL 優勢**: 開源、高效能、支援複雜查詢、JSON 型別
5. **生態系統**: Npgsql 是成熟的 PostgreSQL 驅動，與 EF Core 整合良好

### 環境策略
- **本地開發**: 
  - 使用 Docker 容器或本機安裝的 PostgreSQL 16
  - 連線字串：`Host=localhost;Port=5432;Database=memberservice_dev`
  - 優點：完全本地控制、快速啟動、無雲端成本、支援離線開發
  
- **正式環境**:
  - 使用雲端託管資料庫（Azure Database for PostgreSQL / AWS RDS PostgreSQL）
  - 強制 SSL/TLS 連線 (`SslMode=Require`)
  - 密碼透過 Azure Key Vault / AWS Secrets Manager 管理
  - 啟用自動備份（7-30 天）與異地備援
  - 配置 Private Endpoint 或 IP 白名單限制存取

### Code-First 建置流程
資料庫結構完全由程式碼驅動，無需手動建立資料表：

```bash
# 1. 開發者定義/修改 Entity 類別 (例如 User.cs)
# 2. 建立 Migration 檔案（包含 Up/Down 方法）
dotnet ef migrations add InitialCreate

# 3. 執行 Migration，自動建立/更新資料庫結構
dotnet ef database update

# 部署到正式環境時，CI/CD Pipeline 自動執行 Migration
```

**優點**：
- 資料庫結構版本控制（Migration 檔案提交到 Git）
- 支援多環境自動部署（Dev/Staging/Production）
- 可回滾到任意 Migration 版本
- 團隊協作時避免手動 SQL 不一致

### 實體設計
```csharp
public class User
{
    public long Id { get; set; } // 雪花ID (BIGINT)
    public required string Email { get; set; } // 唯一索引
    public required string PasswordHash { get; set; }
    public required string Username { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // 導航屬性
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}

public class RefreshToken
{
    public Guid Id { get; set; } // GUID 主鍵
    public required string Token { get; set; } // 唯一索引
    public long UserId { get; set; } // 外鍵 (雪花ID)
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // 導航屬性
    public User User { get; set; } = null!;
}
```

### 索引策略
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<User>(entity =>
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.Email).IsUnique();
        entity.Property(e => e.Email).HasMaxLength(255);
        entity.Property(e => e.Username).HasMaxLength(50);
    });
    
    modelBuilder.Entity<RefreshToken>(entity =>
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.Token).IsUnique();
        entity.HasIndex(e => e.UserId);
        entity.HasIndex(e => new { e.UserId, e.ExpiresAt });
    });
}
```

---

## 決策 5：專案架構

### 決策
採用 **Clean Architecture** 分層架構。

### 理由
1. **關注點分離**: 業務邏輯與基礎設施解耦
2. **可測試性**: 核心邏輯不依賴外部框架，易於單元測試
3. **維護性**: 清晰的分層邊界，降低變更影響範圍
4. **DDD 友善**: 支援領域驅動設計實踐
5. **ASP.NET Core 最佳實踐**: 微軟官方推薦架構

### 專案結構
```
src/
├── MemberService.Domain/          # 領域層（核心業務邏輯）
│   ├── Entities/
│   │   ├── User.cs
│   │   └── RefreshToken.cs
│   ├── ValueObjects/
│   │   ├── Email.cs
│   │   └── Password.cs
│   ├── Interfaces/
│   │   ├── IUserRepository.cs
│   │   ├── IPasswordService.cs
│   │   └── IJwtService.cs
│   └── Exceptions/
│       ├── UserAlreadyExistsException.cs
│       └── InvalidCredentialsException.cs
│
├── MemberService.Application/      # 應用層（用例編排）
│   ├── DTOs/
│   │   ├── RegisterRequest.cs
│   │   ├── LoginRequest.cs
│   │   └── UserResponse.cs
│   ├── Services/
│   │   ├── AuthService.cs
│   │   └── UserService.cs
│   └── Validators/
│       ├── RegisterRequestValidator.cs
│       └── LoginRequestValidator.cs
│
├── MemberService.Infrastructure/   # 基礎設施層（外部依賴實作）
│   ├── Persistence/
│   │   ├── MemberDbContext.cs
│   │   ├── Repositories/
│   │   │   └── UserRepository.cs
│   │   └── Migrations/
│   ├── Security/
│   │   ├── BcryptPasswordService.cs
│   │   └── JwtService.cs
│   └── IdGeneration/
│       └── SnowflakeIdGenerator.cs
│
└── MemberService.API/              # API 層（HTTP 端點）
    ├── Controllers/
    │   ├── AuthController.cs
    │   └── UsersController.cs
    ├── Middlewares/
    │   ├── JwtAuthenticationMiddleware.cs
    │   └── ExceptionHandlingMiddleware.cs
    ├── Program.cs
    └── appsettings.json
```

### 依賴規則
- **Domain**: 無任何外部依賴（純 C# 邏輯）
- **Application**: 僅依賴 Domain
- **Infrastructure**: 依賴 Domain + Application + 外部套件
- **API**: 依賴所有層，負責組裝

---

## 決策 6：API 設計規範

### 決策
遵循 **RESTful API 最佳實踐** + **標準化錯誤回應格式**。

### 端點設計
```
POST   /api/auth/register        # 註冊
POST   /api/auth/login            # 登入
POST   /api/auth/refresh-token    # 更新權杖
POST   /api/auth/logout           # 登出（撤銷 Refresh Token）

GET    /api/users/me              # 查詢自己的完整資料
PUT    /api/users/me              # 更新個人資料
PUT    /api/users/me/password     # 變更密碼
GET    /api/users/{id}            # 查詢其他使用者公開資料
```

### 標準化回應格式
```csharp
// 成功回應
{
  "success": true,
  "data": {
    "userId": 123456789012345,
    "email": "user@example.com",
    "username": "John Doe",
    "accessToken": "eyJhbGc...",
    "refreshToken": "c4e5a8b..."
  }
}

// 錯誤回應
{
  "success": false,
  "error": {
    "code": "EMAIL_ALREADY_EXISTS",
    "message": "此電子郵件已被使用",
    "details": null,
    "timestamp": "2025-11-18T10:30:00Z",
    "path": "/api/auth/register"
  }
}
```

### 錯誤碼定義
- `EMAIL_ALREADY_EXISTS`: 註冊時電子郵件重複
- `INVALID_CREDENTIALS`: 登入失敗（電子郵件或密碼錯誤）
- `INVALID_TOKEN`: JWT 無效或過期
- `REFRESH_TOKEN_EXPIRED`: Refresh Token 過期
- `REFRESH_TOKEN_REVOKED`: Refresh Token 已撤銷
- `VALIDATION_ERROR`: 輸入驗證失敗
- `USER_NOT_FOUND`: 使用者不存在

---

## 決策 7：驗證策略

### 決策
使用 **FluentValidation** 實作輸入驗證。

### 理由
1. **表達力強**: 流暢的 API，驗證規則易讀易寫
2. **可重用**: 驗證器可在多個地方重用
3. **可測試**: 驗證邏輯可獨立單元測試
4. **國際化**: 內建多語言錯誤訊息支援
5. **ASP.NET Core 整合**: 自動整合 Model Validation

### 實作範例
```csharp
public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("電子郵件為必填欄位")
            .EmailAddress().WithMessage("請提供有效的電子郵件地址")
            .MaximumLength(255).WithMessage("電子郵件長度不可超過 255 字元");
        
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("密碼為必填欄位")
            .MinimumLength(8).WithMessage("密碼必須至少 8 個字元");
        
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("使用者名稱為必填欄位")
            .MinimumLength(3).WithMessage("使用者名稱必須至少 3 個字元")
            .MaximumLength(50).WithMessage("使用者名稱不可超過 50 個字元")
            .Matches(@"^[\p{L}\s]+$").WithMessage("使用者名稱僅允許字母和空格");
    }
}
```

---

## 決策 8：測試策略

### 決策
採用 **三層測試金字塔**: 單元測試 + 整合測試 + 契約測試。

### 測試框架
- **xUnit**: .NET 官方推薦測試框架
- **Moq**: 模擬框架
- **FluentAssertions**: 斷言庫
- **Testcontainers**: 整合測試用 PostgreSQL 容器

### 測試結構
```
tests/
├── MemberService.Domain.Tests/           # 單元測試（領域邏輯）
│   ├── Entities/
│   │   └── UserTests.cs
│   └── ValueObjects/
│       └── EmailTests.cs
│
├── MemberService.Application.Tests/      # 單元測試（應用邏輯）
│   ├── Services/
│   │   ├── AuthServiceTests.cs
│   │   └── UserServiceTests.cs
│   └── Validators/
│       └── RegisterRequestValidatorTests.cs
│
├── MemberService.Infrastructure.Tests/   # 單元測試（基礎設施）
│   ├── Security/
│   │   ├── BcryptPasswordServiceTests.cs
│   │   └── JwtServiceTests.cs
│   └── IdGeneration/
│       └── SnowflakeIdGeneratorTests.cs
│
├── MemberService.API.Tests/              # 整合測試（API 端點）
│   ├── Controllers/
│   │   ├── AuthControllerTests.cs
│   │   └── UsersControllerTests.cs
│   └── TestFixtures/
│       └── MemberServiceWebApplicationFactory.cs
│
└── MemberService.Contract.Tests/         # 契約測試（API 契約）
    └── OpenApiContractTests.cs
```

### 測試覆蓋率目標
- **單元測試**: > 80% 程式碼覆蓋率
- **整合測試**: 100% API 端點覆蓋
- **契約測試**: OpenAPI 規格驗證

---

## 決策 9：日誌與監控

### 決策
使用 **Serilog** 結構化日誌 + **Application Insights** (可選)。

### 理由
1. **結構化日誌**: 支援 JSON 格式，易於查詢分析
2. **豐富的 Sinks**: 支援檔案、資料庫、雲端服務
3. **效能**: 非同步寫入，不阻塞主執行緒
4. **ASP.NET Core 整合**: 無縫整合，自動記錄 HTTP 請求
5. **彈性配置**: 可透過 appsettings.json 動態調整日誌層級

### 實作配置
```csharp
// Program.cs
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .WriteTo.Console(new JsonFormatter())
        .WriteTo.File(
            path: "logs/member-service-.log",
            rollingInterval: RollingInterval.Day,
            formatter: new JsonFormatter());
});

// 記錄範例
_logger.LogInformation(
    "User {UserId} logged in from IP {IpAddress}",
    userId, ipAddress);

_logger.LogWarning(
    "Failed login attempt for email {Email} from IP {IpAddress}",
    email, ipAddress);
```

---

## 決策 10：部署與設定管理

### 決策
使用 **環境變數** + **Docker** 容器化部署。

### 設定管理
```json
// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "${DB_CONNECTION_STRING}"
  },
  "Jwt": {
    "SecretKey": "${JWT_SECRET_KEY}",
    "Issuer": "AuctionService.MemberService",
    "Audience": "AuctionService",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  },
  "Snowflake": {
    "WorkerId": "${SNOWFLAKE_WORKER_ID}",
    "DatacenterId": "${SNOWFLAKE_DATACENTER_ID}"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    }
  }
}
```

### Docker 容器化
```dockerfile
# Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["MemberService.API/MemberService.API.csproj", "MemberService.API/"]
RUN dotnet restore "MemberService.API/MemberService.API.csproj"
COPY . .
RUN dotnet build "MemberService.API/MemberService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MemberService.API/MemberService.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MemberService.API.dll"]
```

---

## 總結

所有技術決策已確定，沒有 NEEDS CLARIFICATION 項目。本文件提供完整的技術實作指引，可直接進入資料模型設計與 API 契約定義階段。
