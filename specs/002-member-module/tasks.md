# Tasks: Member 模組 — 註冊、驗證與個人資料

**Input**: Design documents from `/specs/002-member-module/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/openapi.yaml ✅, quickstart.md ✅
**Branch**: `002-member-module`
**Generated**: 2026-04-11

## 格式說明：`[ID] [P?] [Story?] 描述`

- **[P]**：可並行執行（不同檔案、同一 Phase 內無依賴的任務）
- **[Story]**：所屬的 User Story（US1–US7）
- TDD 是**不可妥協的原則** — 每個 User Story Phase 內，先寫測試任務並確認失敗，才能實作 Handler

## 路徑慣例

- 模組原始碼：`src/Modules/Member/`
- API 控制器：`src/AuctionService.Api/Controllers/`
- 單元測試：`tests/AuctionService.UnitTests/Member/`
- 整合測試：`tests/AuctionService.IntegrationTests/Member/`

---

## Phase 1：Setup（共用基礎設施）

**目的**：新增所有 User Story 所需的 NuGet 套件與設定速率限制 Middleware。

- [ ] T001 在 `src/Modules/Member/Member.csproj` 新增 `BCrypt.Net-Next` 與 `Microsoft.AspNetCore.RateLimiting` NuGet 套件參考
- [ ] T002 [P] 在 `src/AuctionService.Api/Program.cs` 設定 `AddRateLimiter`（固定視窗策略 `"login-ip"`，每分鐘每 IP 上限 10 次請求）、`UseRateLimiter`，並新增 `AddMemoryCache` 供登入失敗計數使用

**檢查點**：`dotnet build` 成功，無新編譯錯誤

---

## Phase 2：Foundational（實作所有 User Story 的前置基礎）

**目的**：建立 Domain 實體、Application 抽象介面、EF Core 設定、Infrastructure 服務與測試基礎架構。所有 User Story 均依賴此 Phase。

**⚠️ 重要**：以下所有任務完成後，Phase 3 才可開始。

### Domain 層

- [ ] T003 在 `src/Modules/Member/Domain/MemberRole.cs` 建立 `MemberRole` 列舉（Member = 0, Admin = 1）
- [ ] T004 [P] 在 `src/Modules/Member/Domain/PhoneCountryCode.cs` 建立 `PhoneCountryCode` 實體（int PK、DialCode、CountryName、CountryIso、私有建構子）
- [ ] T005 [P] 在 `src/Modules/Member/Domain/RefreshToken.cs` 建立 `RefreshToken` 實體（Guid PK 繼承 `BaseEntity`、UserId、TokenHash、ExpiresAt、IsRevoked；含 `Create(...)`、`Revoke()`、`IsValid()` 方法）
- [ ] T006 依 data-model.md 全量改寫 `src/Modules/Member/Domain/MemberUser.cs`（Email、Username、UsernameNormalized、PasswordHash、DisplayName、MemberRole、地址欄位、PhoneCountryCodeId、PhoneNumber、EF Core 導覽屬性；含 `Create(...)`、`UpdateProfile(...)`、`ChangePassword(...)` 工廠與方法；09 開頭電話自動去除前導 0）

### Application 抽象介面與 DTO

- [ ] T007 [P] 在 `src/Modules/Member/Application/Abstractions/IPasswordHasher.cs` 建立 `IPasswordHasher` 介面（`Hash`、`Verify` 方法）
- [ ] T008 [P] 在 `src/Modules/Member/Application/Abstractions/IJwtTokenService.cs` 建立 `IJwtTokenService` 介面（`GenerateAccessToken`、`GenerateRefreshToken`、`HashToken(string rawToken): string` 方法）
- [ ] T009 [P] 在 `src/Modules/Member/Application/DTOs/UserDto.cs` 建立 `UserDto` record（Id、Email、Username、DisplayName、Role、AddressCountry、AddressCity、AddressPostalCode、AddressLine、DialCode、PhoneNumber、CreatedAt）；一併建立同目錄下 `TokenDto.cs`（AccessToken、RefreshToken、ExpiresIn）與 `PhoneCountryCodeDto.cs`（Id、DialCode、CountryName、CountryIso）

### EF Core 設定

- [ ] T010 [P] 在 `src/Modules/Member/Infrastructure/Persistence/Configurations/PhoneCountryCodeConfiguration.cs` 建立 `PhoneCountryCodeConfiguration`（資料表 `phone_country_codes`、schema `member`、索引；以 `HasData` 寫入至少 TW / US / JP / GB / AU / CA 的 seed 資料）
- [ ] T011 [P] 在 `src/Modules/Member/Infrastructure/Persistence/Configurations/MemberUserConfiguration.cs` 建立 `MemberUserConfiguration`（資料表 `users`、schema `member`、各欄位長度限制、Email 與 UsernameNormalized 唯一索引、phone_country_codes FK 設定 `DeleteBehavior.Restrict`）
- [ ] T012 [P] 在 `src/Modules/Member/Infrastructure/Persistence/Configurations/RefreshTokenConfiguration.cs` 建立 `RefreshTokenConfiguration`（資料表 `refresh_tokens`、schema `member`；TokenHash 最大長度 88 唯一索引、UserId 索引）
- [ ] T013 改寫 `src/Modules/Member/Infrastructure/Persistence/MemberDbContext.cs`（移除內聯 `OnModelCreating` 個別設定，改用 `ApplyConfiguration`，新增 `DbSet<RefreshToken>` 與 `DbSet<PhoneCountryCode>`）

### Infrastructure 服務

- [ ] T014 [P] 在 `src/Modules/Member/Infrastructure/Services/BcryptPasswordHasher.cs` 實作 `BcryptPasswordHasher`（WorkFactor = 12）
- [ ] T015 [P] 在 `src/Modules/Member/Infrastructure/Services/JwtTokenService.cs` 實作 `JwtTokenService`（HS256 簽名；Access Token 15 分鐘含 sub/email/role claims；Refresh Token 以 `RandomNumberGenerator.GetBytes(64)` 產生 URL-safe Base64 原始值 + SHA-256 雜湊存儲）

### DI 註冊

- [ ] T016 更新 `src/Modules/Member/Infrastructure/DependencyInjection.cs`，註冊 `MemberDbContext`（Npgsql）、`IPasswordHasher → BcryptPasswordHasher`、`IJwtTokenService → JwtTokenService`
- [ ] T016b 在 `src/AuctionService.Api/Program.cs` 呼叫 `builder.Services.AddMemberModule(builder.Configuration)` 掛載 Member 模組（若已存在則確認呼叫位置在 JWT 設定之前）

### Domain 單元測試

- [ ] T017 [P] 在 `tests/AuctionService.UnitTests/Member/Domain/MemberUserTests.cs` 撰寫 `MemberUser` 單元測試（`Create` 設定預設值、09 前綴電話去除前導 0、`DisplayName` 預設使用 username、`UpdateProfile` 正規化 username、`ChangePassword` 更新雜湊）
- [ ] T018 [P] 在 `tests/AuctionService.UnitTests/Member/Domain/RefreshTokenTests.cs` 撰寫 `RefreshToken` 單元測試（`Create` 含有效期限、`Revoke` 設定 IsRevoked、`IsValid` 在已過期時回傳 false、`IsValid` 在已撤銷時回傳 false）

### Migration 與整合測試基礎

- [ ] T019 刪除舊 scaffold migration `InitialCreate`，以 `dotnet ef migrations add AddMemberModuleSchema` 建立新 migration（含 3 個資料表 + phone_country_codes seed 資料）
- [ ] T020 在 `tests/AuctionService.IntegrationTests/Member/MemberIntegrationTestBase.cs` 建立 `MemberIntegrationTestBase`（Testcontainers PostgreSQL 資料庫容器、`OneTimeSetUp` 時呼叫 `context.Database.MigrateAsync()` 建立 schema、seed 輔助方法、取得 Bearer Token 輔助方法）

**檢查點**：`dotnet test` 通過（T017 / T018 測試綠燈），Domain 與 Foundation 就緒，User Story Phase 可開始

---

## Phase 3：User Story 1 — 新訪客完成帳號註冊（Priority: P1）🎯 MVP

**目標**：新訪客提交有效的註冊資料後收到 201 與帳號基本資料；重複 email 回 409；格式錯誤回 422。

**獨立測試**：POST `/api/auth/register` 送有效資料 → 201 UserDto；相同 email 再次註冊 → 409；密碼不符規則 → 422 含欄位說明。

> **TDD**：先寫 T021 單元測試並確認失敗，再實作 T024 Handler。

- [ ] T021 [US1] 在 `tests/AuctionService.UnitTests/Member/Application/RegisterCommandHandlerTests.cs` 撰寫 `RegisterCommandHandler` 單元測試（成功建立帳號、email 重複 → 409、無效 `phoneCountryCodeId` → 422）
- [ ] T022 [P] [US1] 在 `src/Modules/Member/Application/Commands/Register/RegisterCommand.cs` 建立 `RegisterCommand` record（含所有欄位）與 `RegisterCommandResult`（UserDto）
- [ ] T023 [P] [US1] 在 `src/Modules/Member/Application/Commands/Register/RegisterCommandValidator.cs` 實作 `RegisterCommandValidator`（FluentValidation：email 格式、username 3–30 字元、密碼 ≥8 含大/小/數字、phoneCountryCodeId 必填 int、phoneNumber 純數字 Regex、地址子欄最大長度：country / city 100、postalCode 20、addressLine 200）
- [ ] T024 [US1] 在 `src/Modules/Member/Application/Commands/Register/RegisterCommandHandler.cs` 實作 `RegisterCommandHandler`（檢查 email 唯一性 → 409、確認 phoneCountryCodeId 存在 → 422、BCrypt 雜湊密碼、`MemberUser.Create(...)`、存檔、回傳 UserDto）
- [ ] T025 [P] [US1] 在 `src/AuctionService.Api/Controllers/AuthController.cs` 建立 `AuthController`，新增 `POST /api/auth/register` 動作方法（派發 `RegisterCommand`，回傳 201 `ApiResponse<UserDto>`）
- [ ] T026 [US1] 在 `tests/AuctionService.IntegrationTests/Member/RegisterEndpointTests.cs` 撰寫整合測試（201 + UserDto、409 重複 email、422 密碼複雜度不符、422 無效 phoneCountryCodeId）

**檢查點**：US1 完整可獨立測試，MVP 可交付！

---

## Phase 4：User Story 2 — 已註冊使用者登入取得驗證憑證（Priority: P1）

**目標**：已註冊使用者提交正確的 email + password 後收到 JWT Access Token 與 Refresh Token；錯誤憑證回 401；同 IP 超過失敗次數上限回 429。

**獨立測試**：完成 US1 註冊，再登入 → 200 TokenDto；錯誤密碼 → 401（與 email 不存在回應相同）；同 IP 第 6 次失敗 → 429。

> **TDD**：先寫 T027 單元測試並確認失敗，再實作 T030 Handler。

- [ ] T027 [US2] 在 `tests/AuctionService.UnitTests/Member/Application/LoginCommandHandlerTests.cs` 撰寫 `LoginCommandHandler` 單元測試（成功回傳 tokens、錯誤密碼 → 401、不存在 email → 401、同 IP 第 6 次失敗 → 429）
- [ ] T028 [P] [US2] 在 `src/Modules/Member/Application/Commands/Login/LoginCommand.cs` 建立 `LoginCommand` record（email、password、clientIp）與 `LoginCommandResult`（TokenDto）
- [ ] T029 [P] [US2] 在 `src/Modules/Member/Application/Commands/Login/LoginCommandValidator.cs` 實作 `LoginCommandValidator`（email 格式必填、password 非空）
- [ ] T030 [US2] 在 `src/Modules/Member/Application/Commands/Login/LoginCommandHandler.cs` 實作 `LoginCommandHandler`（以 email 查詢使用者、透過 `IMemoryCache` 讀取 IP 失敗計數 → 計數 **> 5**（第 6 次失敗時）則回 429、BCrypt 驗證失敗 → 401 + 累計計數、成功 → 清除計數、產生 JWT + Refresh Token、新增 `RefreshToken` 記錄、回傳 TokenDto）
- [ ] T031 [P] [US2] 在 `src/AuctionService.Api/Controllers/AuthController.cs` 新增 `POST /api/auth/login` 動作方法，套用 `[EnableRateLimiting("login-ip")]`；從 `HttpContext.Connection.RemoteIpAddress?.ToString()` 提取 Client IP 後注入 `LoginCommand.ClientIp`
- [ ] T032 [US2] 在 `tests/AuctionService.IntegrationTests/Member/LoginEndpointTests.cs` 撰寫整合測試（200 + TokenDto、401 錯誤密碼、401 不存在 email、429 超過失敗次數）

**檢查點**：Registration → Login 完整端對端驗證，US1 + US2 均可獨立測試

---

## Phase 5：User Story 3 — 以 Refresh Token 換取新 Access Token（Priority: P2）

**目標**：用戶端送有效 Refresh Token 後收到新的 Access Token + 新的 Refresh Token；舊 Refresh Token 立即撤銷；已過期或已撤銷的 Token 回 401。

**獨立測試**：登入後使用 Refresh Token → 200 新 TokenDto；重送原 Refresh Token → 401；送出過期 Token → 401。

> **TDD**：先寫 T033 單元測試並確認失敗，再實作 T036 Handler。

- [ ] T033 [US3] 在 `tests/AuctionService.UnitTests/Member/Application/RefreshTokenCommandHandlerTests.cs` 撰寫 `RefreshTokenCommandHandler` 單元測試（成功輪換 tokens、已過期 token → 401、已撤銷 token → 401）
- [ ] T034 [P] [US3] 在 `src/Modules/Member/Application/Commands/RefreshToken/RefreshTokenCommand.cs` 建立 `RefreshTokenCommand` record（rawToken string）與 `RefreshTokenCommandResult`（TokenDto）
- [ ] T035 [P] [US3] 在 `src/Modules/Member/Application/Commands/RefreshToken/RefreshTokenCommandValidator.cs` 實作 `RefreshTokenCommandValidator`（refreshToken 非空）
- [ ] T036 [US3] 在 `src/Modules/Member/Application/Commands/RefreshToken/RefreshTokenCommandHandler.cs` 實作 `RefreshTokenCommandHandler`（SHA-256 雜湊原始值後查詢 `TokenHash`、呼叫 `IsValid()` 否則回 401、呼叫 `Revoke()`、發行新 JWT + Refresh Token pair、持久化新 token、回傳 TokenDto）
- [ ] T037 [P] [US3] 在 `src/AuctionService.Api/Controllers/AuthController.cs` 新增 `POST /api/auth/refresh` 動作方法
- [ ] T038 [US3] 在 `tests/AuctionService.IntegrationTests/Member/RefreshTokenEndpointTests.cs` 撰寫整合測試（200 新 tokens、401 已過期、401 重用已撤銷 token）

**檢查點**：Token Rotation 可獨立測試

---

## Phase 6：User Story 4 — 使用者登出（Priority: P2）

**目標**：已登入使用者提交 Refresh Token 登出後 token 被撤銷，回傳 204；以已撤銷 token 再次登出仍回傳 204（冪等）。

**獨立測試**：登入 → 登出 → 204；再次提交相同 token 登出 → 204；以舊 token 嘗試換新 token → 401。

> **TDD**：先寫 T039 單元測試並確認失敗，再實作 T042 Handler。

- [ ] T039 [US4] 在 `tests/AuctionService.UnitTests/Member/Application/LogoutCommandHandlerTests.cs` 撰寫 `LogoutCommandHandler` 單元測試（成功撤銷 token、已撤銷 token 仍回傳 204 冪等、不存在 token 回 204）
- [ ] T040 [P] [US4] 在 `src/Modules/Member/Application/Commands/Logout/LogoutCommand.cs` 建立 `LogoutCommand` record（rawToken string）
- [ ] T041 [P] [US4] 在 `src/Modules/Member/Application/Commands/Logout/LogoutCommandValidator.cs` 實作 `LogoutCommandValidator`（refreshToken 非空）
- [ ] T042 [US4] 在 `src/Modules/Member/Application/Commands/Logout/LogoutCommandHandler.cs` 實作 `LogoutCommandHandler`（SHA-256 雜湊查詢 TokenHash，若存在且未撤銷則呼叫 `Revoke()`，不拋出例外，保證回傳成功）
- [ ] T043 [P] [US4] 在 `src/AuctionService.Api/Controllers/AuthController.cs` 新增 `POST /api/auth/logout` 動作方法（回傳 204）
- [ ] T044 [US4] 在 `tests/AuctionService.IntegrationTests/Member/LogoutEndpointTests.cs` 撰寫整合測試（204 正常登出、204 冪等重複登出、登出後 refresh → 401）

**檢查點**：完整 auth 循環（register → login → refresh → logout）端對端可驗證

---

## Phase 7：User Story 5 — 查詢個人資料 + FR-016 國碼列表（Priority: P3）

**目標**：已登入使用者可取得自己的個人資料（id、email、username、displayName、地址、電話、角色）；電話國碼端點無需驗證即可查詢。

**獨立測試**：登入後 GET `/api/users/me` → 200 UserDto 含所有欄位；無 token → 401。GET `/api/phone-country-codes` → 200 含 seed 資料（無需 auth）。

> **TDD**：先寫 T045 單元測試並確認失敗，再實作 T047 Handler。

- [ ] T045 [US5] 在 `tests/AuctionService.UnitTests/Member/Application/GetMeQueryHandlerTests.cs` 撰寫 `GetMeQueryHandler` 單元測試（成功回傳含 phoneDialCode 的 UserDto、使用者不存在 → 404）- [ ] T045b [P] [US5] 在 `src/Modules/Member/Application/Queries/GetPhoneCountryCodes/GetPhoneCountryCodesQuery.cs` 建立 `GetPhoneCountryCodesQuery` record 與 `GetPhoneCountryCodesQueryHandler`（查詢所有 `PhoneCountryCode` 記錄，對應 `PhoneCountryCodeDto[]` 回傳）- [ ] T046 [P] [US5] 在 `src/Modules/Member/Application/Queries/GetMe/GetMeQuery.cs` 建立 `GetMeQuery` record（userId Guid）與 `GetMeQueryResult`（UserDto）
- [ ] T047 [US5] 在 `src/Modules/Member/Application/Queries/GetMe/GetMeQueryHandler.cs` 實作 `GetMeQueryHandler`（以 id 查詢使用者並 eager-load `CountryCode` 導覽屬性，對應到含 `phoneDialCode` 的 UserDto；不存在回 404）
- [ ] T048 [P] [US5] 在 `src/AuctionService.Api/Controllers/UsersController.cs` 建立 `UsersController`，新增 `[Authorize]` `GET /api/users/me` 動作方法（讀取 `sub` claim 後派發 `GetMeQuery`）
- [ ] T049 [P] [US5] 在 `src/AuctionService.Api/Controllers/PhoneCountryCodesController.cs` 建立 `PhoneCountryCodesController`，新增無需驗證的 `GET /api/phone-country-codes` 動作方法（透過 MediatR 派發 `GetPhoneCountryCodesQuery`，回傳 `ApiResponse<PhoneCountryCodeDto[]>` 200）
- [ ] T050 [US5] 在 `tests/AuctionService.IntegrationTests/Member/ProfileAndPhoneCodesEndpointTests.cs` 撰寫整合測試（GET `/api/users/me` 回 200 UserDto 與 401 無 token；GET `/api/phone-country-codes` 回 200 含 seed 資料，無需 auth）

**檢查點**：個人資料查詢與國碼查詢可獨立測試

---

## Phase 8：User Story 6 — 更新個人資料（Priority: P3）

**目標**：已登入使用者可更新 username、displayName 與地址欄位；username 衝突回 409；嘗試變更 phone/email 回 422；格式錯誤回 422。

**獨立測試**：登入後 PUT `/api/users/me` 傳入新 username → 200 更新後 UserDto；傳入重複 username → 409；body 含 `phoneNumber` 欄位 → 422。

> **TDD**：先寫 T051 單元測試並確認失敗，再實作 T054 Handler。

- [ ] T051 [US6] 在 `tests/AuctionService.UnitTests/Member/Application/UpdateProfileCommandHandlerTests.cs` 撰寫 `UpdateProfileCommandHandler` 單元測試（成功更新並回傳 UserDto、username 衝突 → 409）
- [ ] T052 [P] [US6] 在 `src/Modules/Member/Application/Commands/UpdateProfile/UpdateProfileCommand.cs` 建立 `UpdateProfileCommand` record（userId、username、displayName?、地址子欄?）與 `UpdateProfileCommandResult`（UserDto）
- [ ] T053 [P] [US6] 在 `src/Modules/Member/Application/Commands/UpdateProfile/UpdateProfileCommandValidator.cs` 實作 `UpdateProfileCommandValidator`（username 3–30、displayName ≤50、地址子欄最大長度；拒絕非 null 的 `phone` / `email` 欄位 → 422「欄位不允許透過此端點變更」）
- [ ] T054 [US6] 在 `src/Modules/Member/Application/Commands/UpdateProfile/UpdateProfileCommandHandler.cs` 實作 `UpdateProfileCommandHandler`（以 `UsernameNormalized` 排除自身後查詢唯一性 → 409、呼叫 `user.UpdateProfile(...)`、存檔、回傳更新後 UserDto）
- [ ] T055 [P] [US6] 在 `src/AuctionService.Api/Controllers/UsersController.cs` 新增 `[Authorize]` `PUT /api/users/me` 動作方法（派發 `UpdateProfileCommand`，回傳 200 `ApiResponse<UserDto>`）
- [ ] T056 [US6] 在 `tests/AuctionService.IntegrationTests/Member/UpdateProfileEndpointTests.cs` 撰寫整合測試（200 更新後 UserDto、409 username 衝突、422 username 長度超限、422 嘗試變更 phone、401 無 token）

**檢查點**：個人資料更新可獨立測試

---

## Phase 9：User Story 7 — 變更密碼（Priority: P3）

**目標**：已登入使用者提供舊密碼與新密碼後成功變更；成功後撤銷使用者所有 Refresh Token；舊密碼錯誤回 401；新密碼與舊密碼相同或不符規則回 422。

**獨立測試**：登入 → 變更密碼（正確）→ 204；使用舊 Refresh Token 換新 token → 401；以舊密碼登入 → 401；以新密碼登入 → 200。

> **TDD**：先寫 T057 單元測試並確認失敗，再實作 T060 Handler。

- [ ] T057 [US7] 在 `tests/AuctionService.UnitTests/Member/Application/ChangePasswordCommandHandlerTests.cs` 撰寫 `ChangePasswordCommandHandler` 單元測試（成功撤銷所有使用者 tokens 並更新雜湊、舊密碼錯誤 → 401、新密碼與舊密碼相同 → 422）
- [ ] T058 [P] [US7] 在 `src/Modules/Member/Application/Commands/ChangePassword/ChangePasswordCommand.cs` 建立 `ChangePasswordCommand` record（userId、currentPassword、newPassword）
- [ ] T059 [P] [US7] 在 `src/Modules/Member/Application/Commands/ChangePassword/ChangePasswordCommandValidator.cs` 實作 `ChangePasswordCommandValidator`（currentPassword 非空、newPassword ≥8 含大/小/數字複雜度規則）
- [ ] T060 [US7] 在 `src/Modules/Member/Application/Commands/ChangePassword/ChangePasswordCommandHandler.cs` 實作 `ChangePasswordCommandHandler`（查詢使用者、以 `IPasswordHasher.Verify` 驗證舊密碼 → 否則回 401、以 `Verify` 檢查新舊密碼相同 → 422、雜湊新密碼、呼叫 `user.ChangePassword(newHash)`、批次撤銷 `WHERE user_id = userId AND is_revoked = false` 的所有 refresh tokens、存檔）
- [ ] T061 [P] [US7] 在 `src/AuctionService.Api/Controllers/UsersController.cs` 新增 `[Authorize]` `PUT /api/users/me/password` 動作方法（派發 `ChangePasswordCommand`，回傳 204）
- [ ] T062 [US7] 在 `tests/AuctionService.IntegrationTests/Member/ChangePasswordEndpointTests.cs` 撰寫整合測試（204 + 以舊 Refresh Token refresh 確認已撤銷、401 舊密碼錯誤、422 新舊密碼相同）

**檢查點**：全部 7 個 User Story 完整、可獨立測試

---

## Phase 10：Polish & 橫切關注點

- [ ] T063 在 `src/Modules/Member/Infrastructure/BackgroundServices/RefreshTokenCleanupService.cs` 實作 `RefreshTokenCleanupService`（繼承 `BackgroundService`，24 小時間隔，刪除所有 `expires_at < NOW() OR is_revoked = true` 的 refresh_tokens 記錄）
- [ ] T064 在 `src/Modules/Member/Infrastructure/DependencyInjection.cs` 以 `services.AddHostedService<RefreshTokenCleanupService>()` 註冊清理服務
- [ ] T065 [P] 在 `src/Modules/Member/Application/Commands/Login/LoginCommandHandler.cs` 新增結構化 `ILogger` 記錄（登入失敗含 IP、登入成功含 userId）
- [ ] T065b [P] 在 `src/Modules/Member/Application/Commands/ChangePassword/ChangePasswordCommandHandler.cs` 新增結構化 `ILogger` 記錄（密碼變更成功含 userId、撤銷 token 數量）
- [ ] T065c [P] 在 `src/Modules/Member/Application/Commands/Register/RegisterCommandHandler.cs` 新增結構化 `ILogger` 記錄（帳號建立成功含 userId、email 重複衝突含 email 值）
- [ ] T065d [P] 在 `src/Modules/Member/Application/Commands/RefreshToken/RefreshTokenCommandHandler.cs` 新增結構化 `ILogger` 記錄（Token Rotation 成功含 userId、無效 token 嘗試含失敗原因）
- [ ] T065e [P] 在 `src/Modules/Member/Application/Commands/Logout/LogoutCommandHandler.cs` 新增結構化 `ILogger` 記錄（登出撤銷 token 成功含 userId、已撤銷 token 重複登出的冪等操作）
- [ ] T065f [P] 在 `src/Modules/Member/Application/Queries/GetMe/GetMeQueryHandler.cs` 新增結構化 `ILogger` 記錄（查詢找不到使用者時以 Warning 層級記錄含 userId）
- [ ] T065g [P] 在 `src/Modules/Member/Application/Commands/UpdateProfile/UpdateProfileCommandHandler.cs` 新增結構化 `ILogger` 記錄（個人資料更新成功含 userId、username 衝突含衝突值）
- [ ] T066 執行 `dotnet test --collect:"XPlat Code Coverage"`，確認 `AuctionService.UnitTests` 與 `AuctionService.IntegrationTests` **零失敗**、Application/Domain 層業務邏輯單元測試覆蓋率 **≥ 80%** — 兩項條件均滿足才算功能完成
- [ ] T067 在 `src/AuctionService.Api/Middleware/CorrelationIdMiddleware.cs` 建立 `CorrelationIdMiddleware`（讀取請求 `X-Correlation-Id` header，若無則以 `Guid.NewGuid()` 生成，存入 `HttpContext.Items` 並寫入回應 header）；於 `src/AuctionService.Api/Program.cs` 在 `UseMiddleware<GlobalExceptionMiddleware>()` 之前呼叫 `app.UseMiddleware<CorrelationIdMiddleware>()`

---

## 依賴順序（User Story 完成順序）

```
Phase 2 Foundational（T003–T020）
    └── US1 Register（T021–T026）← MVP 可交付！
            └── US2 Login（T027–T032）← 依賴已註冊使用者
                    ├── US3 RefreshToken（T033–T038）← 依賴 Login 取得 token
                    ├── US4 Logout（T039–T044）       ← 依賴 Login 取得 token
                    ├── US5 GetMe（T045–T050）         ← 依賴 Login Bearer
                    ├── US6 UpdateProfile（T051–T056） ← 依賴 Login Bearer
                    └── US7 ChangePassword（T057–T062）← 依賴 Login Bearer
```

> US3–US7 在 US2 完成後可以任意順序實作。

---

## 可並行機會

同一 Phase 標記 `[P]` 的任務可同時進行：

| Phase | 可並行的任務群 |
|-------|------------------|
| Phase 2 | T004 + T005（Domain 實體）；T007 + T008 + T009（抽象介面 + DTO）；T010 + T011 + T012（EF 設定）；T014 + T015（Infrastructure 服務）；T017 + T018（Domain 測試） |
| Phase 3（US1） | 撰寫完 T021 測試後，T022 + T023 可同時進行 |
| Phase 4–9 | 各 Phase 的 Command record + Validator `[P]` 任務可同時進行 |
| Phase 7（US5） | T048 + T049（UsersController + PhoneCodesController）可同時進行 |

---

## 實作策略

**MVP 範圍（僅 Phase 3）**：US1（Register）可獨立交付，提供完整身份識別基礎。

**遞增交付**：
1. Phase 1–2：基礎設施（無使用者可見價值，但解鎖所有後續功能）
2. Phase 3–4：US1 + US2 → 使用者可註冊與登入（核心 auth 入口）
3. Phase 5–6：US3 + US4 → Token 管理完整（工作階段已鞏固）
4. Phase 7–9：US5 + US6 + US7 → 完整自助式個人資料管理

**總計**：75 個任務，10 個 Phase