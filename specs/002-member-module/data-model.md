# 資料模型：Member 模組

**Branch**: `002-member-module`  
**Date**: 2026-04-08  
**Schema**: `member`

---

## 實體關係圖

```
member.phone_country_codes               member.users (1) ───────── (N) member.refresh_tokens
  id (INT, PK, identity)                   id (PK)                        id (PK)
  dial_code (VARCHAR 10, UNIQUE, NOT NULL) email (UNIQUE)                 user_id (FK → users.id, logical only)
  country_name (VARCHAR 100, NOT NULL)     username (VARCHAR 30)          token_hash (VARCHAR 88, UNIQUE)
  country_iso (CHAR 2, NOT NULL)           username_normalized (UNIQUE)   expires_at (TIMESTAMPTZ)
                                           password_hash (TEXT)           is_revoked (BOOLEAN)
member.users (N) ─────────────── (1)       display_name (VARCHAR 50,null) created_at (TIMESTAMPTZ)
member.phone_country_codes                 role (VARCHAR 20)
  phone_country_code_id (INT, FK, NOT NULL)address_country (VARCHAR 100, null)
                                           address_city (VARCHAR 100, null)
                                           address_postal_code (VARCHAR 20, null)
                                           address_line (VARCHAR 200, null)
                                           phone_country_code_id (INT, FK, NOT NULL)
                                           phone_number (VARCHAR 30, NOT NULL)
                                           created_at (TIMESTAMPTZ)
                                           updated_at (TIMESTAMPTZ)
```

---

## Domain 實體

### MemberUser（`src/Modules/Member/Domain/MemberUser.cs`）

```csharp
public class MemberUser : BaseEntity
{
    public string Email { get; private set; } = string.Empty;
    public string Username { get; private set; } = string.Empty;
    public string UsernameNormalized { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string? DisplayName { get; private set; }
    public MemberRole Role { get; private set; } = MemberRole.Member;

    // Address (structured)
    public string? AddressCountry { get; private set; }
    public string? AddressCity { get; private set; }
    public string? AddressPostalCode { get; private set; }
    public string? AddressLine { get; private set; }

    public int PhoneCountryCodeId { get; private set; }
    public PhoneCountryCode? CountryCode { get; private set; }  // EF Core nav
    public string PhoneNumber { get; private set; } = string.Empty;

    private MemberUser() { }   // EF Core

    public static MemberUser Create(
        string email,
        string username,
        string passwordHash,
        int phoneCountryCodeId,
        string phoneNumber,
        string? displayName,
        string? addressCountry,
        string? addressCity,
        string? addressPostalCode,
        string? addressLine)
    {
        var now = DateTimeOffset.UtcNow;
        return new MemberUser
        {
            Id = Guid.NewGuid(),
            Email = email.Trim().ToLowerInvariant(),
            Username = username.Trim(),
            UsernameNormalized = username.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? username.Trim() : displayName.Trim(),
            Role = MemberRole.Member,
            AddressCountry = addressCountry?.Trim(),
            AddressCity = addressCity?.Trim(),
            AddressPostalCode = addressPostalCode?.Trim(),
            AddressLine = addressLine?.Trim(),
            PhoneCountryCodeId = phoneCountryCodeId,
            PhoneNumber = phoneNumber.StartsWith("09")
                ? phoneNumber[1..]
                : phoneNumber,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateProfile(
        string username,
        string? displayName,
        string? addressCountry,
        string? addressCity,
        string? addressPostalCode,
        string? addressLine)
    {
        Username = username.Trim();
        UsernameNormalized = username.Trim().ToLowerInvariant();
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? Username : displayName.Trim();
        AddressCountry = addressCountry?.Trim();
        AddressCity = addressCity?.Trim();
        AddressPostalCode = addressPostalCode?.Trim();
        AddressLine = addressLine?.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ChangePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
```

### RefreshToken（`src/Modules/Member/Domain/RefreshToken.cs`）

```csharp
public class RefreshToken : BaseEntity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }

    private RefreshToken() { }   // EF Core

    public static RefreshToken Create(Guid userId, string tokenHash, DateTimeOffset expiresAt)
    {
        var now = DateTimeOffset.UtcNow;
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            IsRevoked = false,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Revoke()
    {
        IsRevoked = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public bool IsValid() => !IsRevoked && ExpiresAt > DateTimeOffset.UtcNow;
}
```

### PhoneCountryCode（`src/Modules/Member/Domain/PhoneCountryCode.cs`）

```csharp
// 查詢表（Lookup table），以 seed data 初始化，不使用 BaseEntity（int PK）
public class PhoneCountryCode
{
    public int Id { get; private set; }
    public string DialCode { get; private set; } = string.Empty;      // e.g. "886", "1"
    public string CountryName { get; private set; } = string.Empty;   // e.g. "Taiwan", "United States"
    public string CountryIso { get; private set; } = string.Empty;    // e.g. "TW", "US"

    private PhoneCountryCode() { }   // EF Core
}
```

### MemberRole Enum（`src/Modules/Member/Domain/MemberRole.cs`）

```csharp
public enum MemberRole
{
    Member = 0,
    Admin = 1
}
```

---

## Application 層介面

### `src/Modules/Member/Application/Abstractions/IPasswordHasher.cs`

```csharp
public interface IPasswordHasher
{
    string Hash(string plainPassword);
    bool Verify(string plainPassword, string hash);
}
```

### `src/Modules/Member/Application/Abstractions/IJwtTokenService.cs`

```csharp
public interface IJwtTokenService
{
    string GenerateAccessToken(Guid userId, string email, MemberRole role);
    (string RawToken, string TokenHash, DateTimeOffset ExpiresAt) GenerateRefreshToken();
}
```

---

## EF Core – MemberDbContext

### 欄位對映（`src/Modules/Member/Infrastructure/Persistence/`）

**MemberUserConfiguration**:
```csharp
builder.ToTable("users", "member");
builder.HasKey(u => u.Id);
builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
builder.HasIndex(u => u.Email).IsUnique();
builder.Property(u => u.Username).IsRequired().HasMaxLength(30);
builder.Property(u => u.UsernameNormalized).IsRequired().HasMaxLength(30);
builder.HasIndex(u => u.UsernameNormalized).IsUnique();
builder.Property(u => u.PasswordHash).IsRequired();
builder.Property(u => u.DisplayName).HasMaxLength(50);
builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
builder.Property(u => u.AddressCountry).HasMaxLength(100);
builder.Property(u => u.AddressCity).HasMaxLength(100);
builder.Property(u => u.AddressPostalCode).HasMaxLength(20);
builder.Property(u => u.AddressLine).HasMaxLength(200);
builder.Property(u => u.PhoneCountryCodeId).IsRequired();
builder.HasOne(u => u.CountryCode)
       .WithMany()
       .HasForeignKey(u => u.PhoneCountryCodeId)
       .OnDelete(DeleteBehavior.Restrict);
builder.Property(u => u.PhoneNumber).IsRequired().HasMaxLength(30);
```

**PhoneCountryCodeConfiguration**:
```csharp
builder.ToTable("phone_country_codes", "member");
builder.HasKey(p => p.Id);
builder.Property(p => p.Id).ValueGeneratedOnAdd();
builder.Property(p => p.DialCode).IsRequired().HasMaxLength(10);
builder.HasIndex(p => p.DialCode).IsUnique();
builder.Property(p => p.CountryName).IsRequired().HasMaxLength(100);
builder.Property(p => p.CountryIso).IsRequired().HasMaxLength(2);
```

**RefreshTokenConfiguration**:
```csharp
builder.ToTable("refresh_tokens", "member");
builder.HasKey(t => t.Id);
builder.Property(t => t.TokenHash).IsRequired().HasMaxLength(88);
builder.HasIndex(t => t.TokenHash).IsUnique();
builder.HasIndex(t => t.UserId);
builder.Property(t => t.ExpiresAt).IsRequired();
builder.Property(t => t.IsRevoked).IsRequired().HasDefaultValue(false);
```

---

## Migration 命令

```bash
dotnet ef migrations add AddMemberModuleSchema \
  --project src/Modules/Member/Member.csproj \
  --startup-project src/AuctionService.Api/AuctionService.Api.csproj \
  --output-dir Infrastructure/Persistence/Migrations

dotnet ef database update \
  --project src/Modules/Member/Member.csproj \
  --startup-project src/AuctionService.Api/AuctionService.Api.csproj
```

---

## 驗證規則總覽

| 欄位 | 規則 |
|------|------|
| `email` | 格式驗證（FluentValidation `EmailAddress()`）、唯一（DB UNIQUE） |
| `username` | 3–30 字元、唯一（DB UNIQUE on `username_normalized`） |
| `password` | ≥8 字元，含大寫字母、小寫字母、數字；變更時新密碼不可與舊密碼相同 |
| `display_name` | ≤50 字元（選填，空值預設為 username） |
| `address_country` | ≤100 字元（選填） |
| `address_city` | ≤100 字元（選填） |
| `address_postal_code` | ≤20 字元（選填） |
| `address_line` | ≤200 字元（選填） |
| `phone_country_code_id` | 必填；必須對應 `phone_country_codes.id` 的有效 INT；無效 id 回傳 422 |
| `phone_number` | 僅接受純數字；09 開頭自動去除前導 `0` 後儲存（`0912345678` → `912345678`）；必填 |
| `token_hash` | SHA-256 of 64-byte random → URL-safe Base64（88 字元） |
