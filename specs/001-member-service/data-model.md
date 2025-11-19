# 資料模型：會員服務

## 概述

本文件定義會員服務的資料模型，包含實體定義、關聯關係、驗證規則與資料庫映射。

---

## 核心實體

### User (使用者)

**用途**: 代表系統的註冊會員，包含身份驗證與個人資料。

**屬性**:

| 欄位 | 型別 | 必填 | 說明 | 驗證規則 |
|------|------|------|------|----------|
| Id | long | 是 | 使用者唯一識別碼（雪花ID） | 主鍵，由 IdGenerator 生成 |
| Email | string | 是 | 電子郵件地址 | 唯一，最大長度 255，符合電子郵件格式 |
| PasswordHash | string | 是 | bcrypt 雜湊後的密碼 | 包含 bcrypt(password + snowflakeId) |
| Username | string | 是 | 使用者顯示名稱 | 長度 3-50 字元，僅允許字母與空格 |
| CreatedAt | DateTime | 是 | 帳號建立時間 | UTC 時間，自動設定 |
| UpdatedAt | DateTime | 是 | 最後更新時間 | UTC 時間，自動更新 |

**關聯關係**:
- 一對多: `User` → `RefreshToken` (一個使用者可有多個 Refresh Token)

**索引**:
- 主鍵索引: `Id` (BIGINT)
- 唯一索引: `Email` (用於登入查詢與唯一性驗證)

**Entity Framework 配置**:

```csharp
public class User
{
    public long Id { get; set; }
    
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public required string Email { get; set; }
    
    [Required]
    public required string PasswordHash { get; set; }
    
    [Required]
    [MinLength(3)]
    [MaxLength(50)]
    [RegularExpression(@"^[\p{L}\s]+$")]
    public required string Username { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // 導航屬性
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
```

**資料庫映射** (PostgreSQL):

```sql
CREATE TABLE "Users" (
    "Id" BIGINT PRIMARY KEY,
    "Email" VARCHAR(255) NOT NULL UNIQUE,
    "PasswordHash" TEXT NOT NULL,
    "Username" VARCHAR(50) NOT NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL
);

CREATE INDEX "IX_Users_Email" ON "Users" ("Email");
```

---

### RefreshToken (更新權杖)

**用途**: 代表使用者的長期身份驗證權杖，用於換取新的 JWT Access Token。

**屬性**:

| 欄位 | 型別 | 必填 | 說明 | 驗證規則 |
|------|------|------|------|----------|
| Id | Guid | 是 | 權杖唯一識別碼 | 主鍵，自動生成 GUID |
| Token | string | 是 | 實際的 Refresh Token 字串 | 唯一，使用 Base64 編碼的隨機字節 |
| UserId | long | 是 | 所屬使用者 ID（外鍵） | 關聯到 User.Id |
| ExpiresAt | DateTime | 是 | 權杖過期時間 | UTC 時間，註冊/登入後 7 天 |
| IsRevoked | bool | 是 | 是否已撤銷 | 預設 false，密碼變更時設為 true |
| CreatedAt | DateTime | 是 | 權杖建立時間 | UTC 時間，自動設定 |

**關聯關係**:
- 多對一: `RefreshToken` → `User` (多個 Refresh Token 屬於一個使用者)

**索引**:
- 主鍵索引: `Id` (GUID)
- 唯一索引: `Token` (用於權杖查詢)
- 複合索引: `(UserId, ExpiresAt)` (用於查詢使用者的有效權杖)
- 外鍵索引: `UserId` (關聯查詢最佳化)

**Entity Framework 配置**:

```csharp
public class RefreshToken
{
    public Guid Id { get; set; }
    
    [Required]
    public required string Token { get; set; }
    
    [Required]
    public long UserId { get; set; }
    
    public DateTime ExpiresAt { get; set; }
    
    public bool IsRevoked { get; set; } = false;
    
    public DateTime CreatedAt { get; set; }
    
    // 導航屬性
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
    
    // 計算屬性
    [NotMapped]
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    
    [NotMapped]
    public bool IsValid => !IsRevoked && !IsExpired;
}
```

**資料庫映射** (PostgreSQL):

```sql
CREATE TABLE "RefreshTokens" (
    "Id" UUID PRIMARY KEY,
    "Token" VARCHAR(512) NOT NULL UNIQUE,
    "UserId" BIGINT NOT NULL,
    "ExpiresAt" TIMESTAMP WITH TIME ZONE NOT NULL,
    "IsRevoked" BOOLEAN NOT NULL DEFAULT FALSE,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL,
    
    CONSTRAINT "FK_RefreshTokens_Users_UserId" 
        FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") 
        ON DELETE CASCADE
);

CREATE INDEX "IX_RefreshTokens_Token" ON "RefreshTokens" ("Token");
CREATE INDEX "IX_RefreshTokens_UserId" ON "RefreshTokens" ("UserId");
CREATE INDEX "IX_RefreshTokens_UserId_ExpiresAt" ON "RefreshTokens" ("UserId", "ExpiresAt");
```

---

## Value Objects (值物件)

### Email

**用途**: 封裝電子郵件地址的驗證邏輯。

```csharp
public class Email : ValueObject
{
    public string Value { get; }
    
    private Email(string value)
    {
        Value = value;
    }
    
    public static Result<Email> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<Email>.Failure("電子郵件為必填欄位");
        
        if (value.Length > 255)
            return Result<Email>.Failure("電子郵件長度不可超過 255 字元");
        
        if (!IsValidEmailFormat(value))
            return Result<Email>.Failure("請提供有效的電子郵件地址");
        
        return Result<Email>.Success(new Email(value.ToLowerInvariant()));
    }
    
    private static bool IsValidEmailFormat(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
```

---

### Password

**用途**: 封裝密碼的驗證邏輯（明文密碼驗證，不包含雜湊）。

```csharp
public class Password : ValueObject
{
    public string Value { get; }
    
    private Password(string value)
    {
        Value = value;
    }
    
    public static Result<Password> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<Password>.Failure("密碼為必填欄位");
        
        if (value.Length < 8)
            return Result<Password>.Failure("密碼必須至少 8 個字元");
        
        if (value.Length > 128)
            return Result<Password>.Failure("密碼長度不可超過 128 字元");
        
        return Result<Password>.Success(new Password(value));
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
```

---

### Username

**用途**: 封裝使用者名稱的驗證邏輯。

```csharp
public class Username : ValueObject
{
    public string Value { get; }
    
    private Username(string value)
    {
        Value = value;
    }
    
    public static Result<Username> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<Username>.Failure("使用者名稱為必填欄位");
        
        if (value.Length < 3)
            return Result<Username>.Failure("使用者名稱必須至少 3 個字元");
        
        if (value.Length > 50)
            return Result<Username>.Failure("使用者名稱不可超過 50 個字元");
        
        // 僅允許字母（Unicode）與空格
        if (!System.Text.RegularExpressions.Regex.IsMatch(value, @"^[\p{L}\s]+$"))
            return Result<Username>.Failure("使用者名稱僅允許字母和空格");
        
        return Result<Username>.Success(new Username(value.Trim()));
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
```

---

## 資料庫上下文

### MemberDbContext

```csharp
public class MemberDbContext : DbContext
{
    public MemberDbContext(DbContextOptions<MemberDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // User 實體配置
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(255);
            
            entity.HasIndex(e => e.Email)
                .IsUnique();
            
            entity.Property(e => e.PasswordHash)
                .IsRequired();
            
            entity.Property(e => e.Username)
                .IsRequired()
                .HasMaxLength(50);
            
            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()");
            
            entity.Property(e => e.UpdatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()");
            
            // 關聯關係
            entity.HasMany(e => e.RefreshTokens)
                .WithOne(rt => rt.User)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // RefreshToken 實體配置
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Token)
                .IsRequired()
                .HasMaxLength(512);
            
            entity.HasIndex(e => e.Token)
                .IsUnique();
            
            entity.HasIndex(e => e.UserId);
            
            entity.HasIndex(e => new { e.UserId, e.ExpiresAt });
            
            entity.Property(e => e.ExpiresAt)
                .IsRequired();
            
            entity.Property(e => e.IsRevoked)
                .IsRequired()
                .HasDefaultValue(false);
            
            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()");
        });
    }
    
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // 自動更新 UpdatedAt
        var entries = ChangeTracker.Entries<User>()
            .Where(e => e.State == EntityState.Modified);
        
        foreach (var entry in entries)
        {
            entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
        
        return base.SaveChangesAsync(cancellationToken);
    }
}
```

---

## 資料遷移

### 初始遷移

```bash
# 建立遷移
dotnet ef migrations add InitialCreate --project MemberService.Infrastructure --startup-project MemberService.API

# 套用遷移
dotnet ef database update --project MemberService.Infrastructure --startup-project MemberService.API
```

### 遷移腳本範例

```csharp
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false),
                Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                PasswordHash = table.Column<string>(type: "text", nullable: false),
                Username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "RefreshTokens",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Token = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                UserId = table.Column<long>(type: "bigint", nullable: false),
                ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                IsRevoked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                table.ForeignKey(
                    name: "FK_RefreshTokens_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Users_Email",
            table: "Users",
            column: "Email",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_Token",
            table: "RefreshTokens",
            column: "Token",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_UserId",
            table: "RefreshTokens",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_UserId_ExpiresAt",
            table: "RefreshTokens",
            columns: new[] { "UserId", "ExpiresAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "RefreshTokens");
        migrationBuilder.DropTable(name: "Users");
    }
}
```

---

## 狀態轉換

### User 生命週期

```
[未註冊] --註冊--> [已註冊]
                       |
                       +--登入--> [已登入]
                       |             |
                       |             +--登出--> [已註冊]
                       |             |
                       |             +--更新資料--> [已登入]
                       |             |
                       |             +--變更密碼--> [已註冊] (撤銷所有 Token)
                       |
                       +--查詢資料--> [已註冊]
```

### RefreshToken 生命週期

```
[建立] --過期--> [過期]
  |
  +--撤銷--> [已撤銷]
  |
  +--使用--> [建立新Token] --過期--> [過期]
```

**撤銷觸發條件**:
1. 使用者變更密碼 → 撤銷該使用者所有 RefreshToken
2. 使用者登出 → 撤銷該 RefreshToken
3. 管理員手動撤銷（範圍外功能）

---

## 資料驗證總結

### User 驗證規則

| 欄位 | 規則 | 錯誤訊息 |
|------|------|----------|
| Email | 必填 | "電子郵件為必填欄位" |
| Email | 格式驗證 | "請提供有效的電子郵件地址" |
| Email | 最大長度 255 | "電子郵件長度不可超過 255 字元" |
| Email | 唯一性 | "此電子郵件已被使用" |
| Password | 必填 | "密碼為必填欄位" |
| Password | 最小長度 8 | "密碼必須至少 8 個字元" |
| Username | 必填 | "使用者名稱為必填欄位" |
| Username | 長度 3-50 | "使用者名稱必須 3-50 字元" |
| Username | 僅字母與空格 | "使用者名稱僅允許字母和空格" |

### RefreshToken 驗證規則

| 欄位 | 規則 | 錯誤訊息 |
|------|------|----------|
| Token | 必填 | 系統錯誤（不應發生） |
| Token | 唯一性 | 系統錯誤（不應發生） |
| Token | 未過期 | "Refresh Token 已過期，請重新登入" |
| Token | 未撤銷 | "權杖無效，請重新登入" |
| UserId | 外鍵存在 | "使用者不存在" |

---

## 效能考量

### 索引策略

1. **Email 唯一索引**: 
   - 用途: 登入查詢、註冊唯一性驗證
   - 預期 QPS: 100-500
   - 回應時間目標: < 10ms

2. **RefreshToken.Token 唯一索引**:
   - 用途: 權杖驗證查詢
   - 預期 QPS: 50-200
   - 回應時間目標: < 5ms

3. **RefreshToken (UserId, ExpiresAt) 複合索引**:
   - 用途: 清理過期權杖、查詢使用者有效權杖
   - 預期 QPS: 10-50
   - 回應時間目標: < 20ms

### 查詢最佳化

- **避免 N+1 查詢**: 使用 `.Include()` 預載入關聯資料
- **分頁支援**: 大量資料查詢使用 `Skip()` + `Take()`
- **唯讀查詢**: 使用 `.AsNoTracking()` 提升效能

---

## 資料保留政策

- **User**: 永久保留（除非使用者要求刪除，範圍外功能）
- **RefreshToken**: 過期後 30 天自動清理
- **PasswordHash**: 永久保留，密碼變更時覆寫

---

## 安全考量

1. **密碼雜湊**: bcrypt(password + snowflakeId)，work factor 12
2. **Refresh Token**: 使用 256-bit 隨機字節，Base64 編碼
3. **級聯刪除**: 刪除 User 時自動刪除相關 RefreshToken
4. **索引洩漏**: Email 唯一索引可能洩漏帳號存在性（註冊時接受，登入時模糊處理）

---

## 總結

資料模型已完整定義，包含實體、關聯、驗證、索引與遷移腳本。所有設計符合 Clean Architecture 原則，支援 EF Core Code-First 工作流程。
