# 技術研究報告：Member 模組

**Branch**: `002-member-module`  
**Date**: 2026-04-08  
**Spec**: [spec.md](./spec.md)

---

## 1. 密碼雜湊：BCrypt.Net-Next

**Decision**: 使用 `BCrypt.Net-Next`（NuGet），WorkFactor = 12  
**Rationale**:  
- WorkFactor 12 在現代硬體約需 250–400ms，足以抵禦暴力破解，對使用者感知影響極低  
- BCrypt 內建 salt（每次雜湊自動產生），不需手動管理 salt 欄位  
- WorkFactor 可升級而不破壞舊密碼（舊雜湊仍可驗證）  
**Alternatives Considered**:  
- Argon2id（`Isopoh.Cryptography.Argon2`）：記憶體硬化，安全性更高，但 .NET 生態系整合較不成熟  
- PBKDF2（ASP.NET Core 內建）：不需額外套件，但 Work Factor 調整彈性不如 BCrypt  

**Implementation**:
```csharp
// Hash
BCrypt.Net.BCrypt.HashPassword(plainPassword, workFactor: 12);
// Verify
BCrypt.Net.BCrypt.Verify(plainPassword, storedHash);
```

---

## 2. JWT：HS256 / Access Token + Refresh Token

**Decision**: `Microsoft.AspNetCore.Authentication.JwtBearer`（已安裝），HS256 簽名  

**Access Token**:
- 有效期：15 分鐘（`exp` claim）
- Claims：`sub`（UserId Guid）、`email`、`role`
- 簽名：HMAC-SHA256，Secret 從環境變數 `JWT_SECRET`（最少 32 字元）

**Refresh Token**:
- 生成：`RandomNumberGenerator.GetBytes(64)` → URL-safe Base64（`Convert.ToBase64String` + 替換 `+/=`）
- 儲存：SHA-256 雜湊值（`token_hash`），原始值僅在發行時回傳一次
- 有效期：7 天
- Token Rotation：每次 refresh 立即撤銷舊 token，發行新 token

**Alternatives Considered**:
- RS256（非對稱）：適合多服務驗證，本專案為 Modular Monolith，HS256 已足夠且效能更好  
- Opaque Token：需要資料庫查詢才能驗證，放棄無狀態優勢

---

## 3. IP 速率限制：ASP.NET Core Built-in Rate Limiting

**Decision**: `Microsoft.AspNetCore.RateLimiting`（.NET 7+ 內建）—— Fixed Window Policy，套用於登入端點  
**Rationale**:  
- .NET 10 內建，不需額外套件  
- Fixed Window（每分鐘視窗）實作簡單，符合規格：同 IP 每分鐘 ≤5 次失敗回傳 429  
- 失敗計數僅在登入失敗時觸發（成功不計入）需在 Handler 層處理

**Implementation Note**: 標準 RateLimiting middleware 是基於請求數計算，而非失敗數。本設計採用 **middleware + 內部計數** 的混合方案：在 HTTP pipeline 層對登入端點套用每分鐘 10 次請求上限（寬鬆保護），Handler 層維護失敗計數（可使用 `IMemoryCache`）。

**Alternatives Considered**:
- `AspNetCoreRateLimit`（第三方）：功能豐富但需額外 NuGet 套件  
- Redis-based 計數器：適合多實例部署，本期 Monolith 單節點不需

---

## 4. Refresh Token 定期清理：IHostedService

**Decision**: 實作 `IHostedService`（`BackgroundService`）定期清理過期及已撤銷的 Refresh Token  
**Rationale**:  
- ASP.NET Core 內建，不需 Hangfire / Quartz 等排程套件  
- 每日執行一次 `DELETE FROM member.refresh_tokens WHERE expires_at < NOW() OR is_revoked = true`  
- 透過 DI 注入，確保與模組生命週期一致

**Implementation**:
```csharp
public class RefreshTokenCleanupService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await CleanupExpiredTokensAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
```

---

## 5. EF Core：Username 大小寫不敏感唯一約束

**Decision**: PostgreSQL `citext` 擴充套件 或 `LOWER()` 函式索引  
**Rationale**:  
- `citext` 欄位類型：整個欄位自動大小寫不敏感，Npgsql 支援，需 `CREATE EXTENSION IF NOT EXISTS citext`  
- 替代方案：儲存 `username_normalized`（lowercase），應用層統一轉換  
- 本期採用 **username_normalized 方案**（儲存正規化後的小寫 username），避免 PostgreSQL 擴充套件依賴，EF Core 更易處理

**Implementation**:
```csharp
// 儲存時
entity.UsernameNormalized = command.Username.ToLowerInvariant();
// 查詢唯一性
await dbContext.Users.AnyAsync(u => u.UsernameNormalized == newUsername.ToLowerInvariant());
```

---

## 6. 密碼變更後撤銷所有 Refresh Token

**Decision**: 密碼變更成功後，批次更新 `is_revoked = true` WHERE `user_id = {userId} AND is_revoked = false`  
**Rationale**:  
- 確保密碼洩漏後，攻擊者持有的 Refresh Token 立即失效  
- 單一 SQL UPDATE，效能影響極低  
- 合法使用者需重新登入（即 token 失效），符合安全設計預期

---

## 7. 地址結構化儲存

**Decision**: 在 `users` 表新增四個 nullable 字串欄位：`address_country`、`address_city`、`address_postal_code`、`address_line`  
**Rationale**:  
- 無需額外的 Address 實體或關聯表，降低 JOIN 複雜度  
- 各欄位均為 nullable（全部選填），符合規格  
- EF Core Owned Entity（Value Object）或直接欄位均可；本期直接欄位（簡單優先）

---

## 8. 結論：無待解 NEEDS CLARIFICATION

所有技術決策已確認，可直接進入 Phase 1 設計。
