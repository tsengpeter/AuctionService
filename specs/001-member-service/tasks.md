# Tasks: MemberService

**Input**: Design documents from `/specs/001-member-service/`
**Prerequisites**: plan.md, spec.md, data-model.md, contracts/openapi.yaml, research.md

**Tests**: 本專案採用 TDD 開發流程，所有任務包含測試任務（測試覆蓋率目標 >80%）

**Organization**: 任務按使用者故事分組，使每個故事可以獨立實作與測試

## Format: `[ID] [P?] [Story] Description`

- **[P]**: 可平行執行（不同檔案，無相依性）
- **[Story]**: 任務所屬的使用者故事（例如：US1, US2, US3, US4）
- 描述包含確切的檔案路徑

## Path Conventions

專案採用單一 MemberService/ 資料夾結構：
- **原始碼**: `MemberService/src/`
- **測試**: `MemberService/tests/`
- **根目錄檔案**: `MemberService/` (解決方案、Docker、README 等)

---

## Phase 1: Setup (共享基礎設施)

**目的**: 專案初始化與基本結構建立

- [ ] T001 建立 MemberService/ 根目錄與基本檔案結構
- [ ] T002 建立 MemberService.sln 解決方案檔案
- [ ] T003 [P] 建立 Domain 專案: MemberService/src/MemberService.Domain/MemberService.Domain.csproj (.NET 10)
- [ ] T004 [P] 建立 Application 專案: MemberService/src/MemberService.Application/MemberService.Application.csproj (.NET 10)
- [ ] T005 [P] 建立 Infrastructure 專案: MemberService/src/MemberService.Infrastructure/MemberService.Infrastructure.csproj (.NET 10)
- [ ] T006 [P] 建立 API 專案: MemberService/src/MemberService.API/MemberService.API.csproj (ASP.NET Core 10 Web API)
- [ ] T007 [P] 建立 Domain.Tests 專案: MemberService/tests/MemberService.Domain.Tests/MemberService.Domain.Tests.csproj
- [ ] T008 [P] 建立 Application.Tests 專案: MemberService/tests/MemberService.Application.Tests/MemberService.Application.Tests.csproj
- [ ] T009 [P] 建立 Infrastructure.Tests 專案: MemberService/tests/MemberService.Infrastructure.Tests/MemberService.Infrastructure.Tests.csproj
- [ ] T010 [P] 建立 IntegrationTests 專案: MemberService/tests/MemberService.IntegrationTests/MemberService.IntegrationTests.csproj
- [ ] T011 設定專案參考（Domain → Application → Infrastructure → API）
- [ ] T012 [P] 安裝 IdGen 3.x 到 Infrastructure 專案
- [ ] T013 [P] 安裝 BCrypt.Net-Next 4.0.3 到 Infrastructure 專案
- [ ] T014 [P] 安裝 System.IdentityModel.Tokens.Jwt 8.0.0 到 Infrastructure 專案
- [ ] T015 [P] 安裝 Npgsql.EntityFrameworkCore.PostgreSQL 10.0 到 Infrastructure 專案
- [ ] T016 [P] 安裝 FluentValidation.AspNetCore 11.3.0 到 Application 專案
- [ ] T017 [P] 安裝 Serilog.AspNetCore 8.0 到 API 專案
- [ ] T018 [P] 安裝 xUnit 2.6+、Moq 4.20+、FluentAssertions 6.12+ 到所有測試專案
- [ ] T019 [P] 安裝 Testcontainers.PostgreSql 3.7+ 到 IntegrationTests 專案
- [ ] T020 [P] 建立 Dockerfile 於 MemberService/Dockerfile
- [ ] T021 [P] 建立 docker-compose.yml 於 MemberService/docker-compose.yml
- [ ] T022 [P] 建立 README.md 於 MemberService/README.md
- [ ] T023 [P] 建立 .gitignore 於 MemberService/.gitignore
- [ ] T024 [P] 建立 .editorconfig 於 MemberService/.editorconfig
- [ ] T025 [P] 建立 global.json 於 MemberService/global.json (鎖定 .NET 10 SDK)

---

## Phase 2: Foundational (阻塞性先決條件)

**目的**: 所有使用者故事都依賴的核心基礎設施

**⚠️ 關鍵**: 所有使用者故事工作必須在本階段完成後才能開始

- [ ] T026 [P] 實作 DomainException 基底類別於 MemberService/src/MemberService.Domain/Exceptions/DomainException.cs
- [ ] T027 [P] 測試 DomainException 於 MemberService/tests/MemberService.Domain.Tests/Exceptions/DomainExceptionTests.cs
- [ ] T028 [P] 實作 Email Value Object 於 MemberService/src/MemberService.Domain/ValueObjects/Email.cs
- [ ] T029 [P] 測試 Email Value Object 於 MemberService/tests/MemberService.Domain.Tests/ValueObjects/EmailTests.cs
- [ ] T030 [P] 實作 Password Value Object 於 MemberService/src/MemberService.Domain/ValueObjects/Password.cs
- [ ] T031 [P] 測試 Password Value Object 於 MemberService/tests/MemberService.Domain.Tests/ValueObjects/PasswordTests.cs
- [ ] T032 [P] 實作 Username Value Object 於 MemberService/src/MemberService.Domain/ValueObjects/Username.cs
- [ ] T033 [P] 測試 Username Value Object 於 MemberService/tests/MemberService.Domain.Tests/ValueObjects/UsernameTests.cs
- [ ] T034 [P] 實作 User 實體於 MemberService/src/MemberService.Domain/Entities/User.cs
- [ ] T035 [P] 測試 User 實體於 MemberService/tests/MemberService.Domain.Tests/Entities/UserTests.cs
- [ ] T036 [P] 實作 RefreshToken 實體於 MemberService/src/MemberService.Domain/Entities/RefreshToken.cs
- [ ] T037 [P] 測試 RefreshToken 實體於 MemberService/tests/MemberService.Domain.Tests/Entities/RefreshTokenTests.cs
- [ ] T038 [P] 定義 IUserRepository 介面於 MemberService/src/MemberService.Domain/Interfaces/IUserRepository.cs
- [ ] T039 [P] 定義 IRefreshTokenRepository 介面於 MemberService/src/MemberService.Domain/Interfaces/IRefreshTokenRepository.cs
- [ ] T040 [P] 定義 IPasswordHasher 介面於 MemberService/src/MemberService.Domain/Interfaces/IPasswordHasher.cs
- [ ] T041 [P] 定義 ITokenGenerator 介面於 MemberService/src/MemberService.Domain/Interfaces/ITokenGenerator.cs
- [ ] T042 [P] 定義 IIdGenerator 介面於 MemberService/src/MemberService.Domain/Interfaces/IIdGenerator.cs
- [ ] T043 實作 SnowflakeIdGenerator 於 MemberService/src/MemberService.Infrastructure/IdGeneration/SnowflakeIdGenerator.cs
- [ ] T044 測試 SnowflakeIdGenerator 於 MemberService/tests/MemberService.Infrastructure.Tests/IdGeneration/SnowflakeIdGeneratorTests.cs
- [ ] T045 實作 BCryptPasswordHasher 於 MemberService/src/MemberService.Infrastructure/Security/BCryptPasswordHasher.cs
- [ ] T046 測試 BCryptPasswordHasher 於 MemberService/tests/MemberService.Infrastructure.Tests/Security/BCryptPasswordHasherTests.cs
- [ ] T047 實作 JwtTokenGenerator 於 MemberService/src/MemberService.Infrastructure/Security/JwtTokenGenerator.cs
- [ ] T048 測試 JwtTokenGenerator 於 MemberService/tests/MemberService.Infrastructure.Tests/Security/JwtTokenGeneratorTests.cs
- [ ] T049 實作 RefreshTokenGenerator 於 MemberService/src/MemberService.Infrastructure/Security/RefreshTokenGenerator.cs
- [ ] T050 測試 RefreshTokenGenerator 於 MemberService/tests/MemberService.Infrastructure.Tests/Security/RefreshTokenGeneratorTests.cs
- [ ] T051 建立 MemberDbContext 於 MemberService/src/MemberService.Infrastructure/Persistence/MemberDbContext.cs
- [ ] T052 [P] 建立 UserConfiguration (EF Core) 於 MemberService/src/MemberService.Infrastructure/Persistence/Configurations/UserConfiguration.cs
- [ ] T053 [P] 建立 RefreshTokenConfiguration (EF Core) 於 MemberService/src/MemberService.Infrastructure/Persistence/Configurations/RefreshTokenConfiguration.cs
- [ ] T054 實作 UserRepository 於 MemberService/src/MemberService.Infrastructure/Persistence/Repositories/UserRepository.cs
- [ ] T055 測試 UserRepository 於 MemberService/tests/MemberService.Infrastructure.Tests/Persistence/UserRepositoryTests.cs
- [ ] T056 實作 RefreshTokenRepository 於 MemberService/src/MemberService.Infrastructure/Persistence/Repositories/RefreshTokenRepository.cs
- [ ] T057 測試 RefreshTokenRepository 於 MemberService/tests/MemberService.Infrastructure.Tests/Persistence/RefreshTokenRepositoryTests.cs
- [ ] T058 建立 EF Core Migration: InitialCreate
- [ ] T059 實作 ExceptionHandlingMiddleware 於 MemberService/src/MemberService.API/Middlewares/ExceptionHandlingMiddleware.cs
- [ ] T060 實作 RequestLoggingMiddleware 於 MemberService/src/MemberService.API/Middlewares/RequestLoggingMiddleware.cs
- [ ] T061 設定 Program.cs 依賴注入與中介軟體管線於 MemberService/src/MemberService.API/Program.cs
- [ ] T062 設定 appsettings.json 於 MemberService/src/MemberService.API/appsettings.json
- [ ] T063 設定 appsettings.Development.json 於 MemberService/src/MemberService.API/appsettings.Development.json
- [ ] T064 建立 PostgreSqlContainerFixture 於 MemberService/tests/MemberService.IntegrationTests/TestFixtures/PostgreSqlContainerFixture.cs

**檢查點**: 基礎設施就緒 - 使用者故事實作現在可以平行開始

---

## Phase 3: User Story 1 - 新使用者註冊與登入 (Priority: P1) 🎯 MVP

**目標**: 使用者可以註冊新帳號並自動登入，取得 JWT 存取權杖以使用其他服務

**獨立測試**: 提供有效的註冊資訊（電子郵件、密碼、使用者名稱），驗證系統建立帳號並回傳 JWT + Refresh Token

**對應端點**: 
- POST /api/auth/register
- POST /api/auth/login

**對應實體**: User, RefreshToken

### 測試 (TDD - 先寫測試)

- [ ] T065 [P] [US1] 撰寫 RegisterRequestValidator 測試於 MemberService/tests/MemberService.Application.Tests/Validators/RegisterRequestValidatorTests.cs
- [ ] T066 [P] [US1] 撰寫 LoginRequestValidator 測試於 MemberService/tests/MemberService.Application.Tests/Validators/LoginRequestValidatorTests.cs
- [ ] T067 [P] [US1] 撰寫 AuthService.Register 測試於 MemberService/tests/MemberService.Application.Tests/Services/AuthServiceTests.cs
- [ ] T068 [P] [US1] 撰寫 AuthService.Login 測試於 MemberService/tests/MemberService.Application.Tests/Services/AuthServiceTests.cs
- [ ] T069 [P] [US1] 撰寫 AuthController.Register 整合測試於 MemberService/tests/MemberService.IntegrationTests/API/AuthControllerTests.cs
- [ ] T070 [P] [US1] 撰寫 AuthController.Login 整合測試於 MemberService/tests/MemberService.IntegrationTests/API/AuthControllerTests.cs

### DTOs

- [ ] T071 [P] [US1] 建立 RegisterRequest DTO 於 MemberService/src/MemberService.Application/DTOs/Auth/RegisterRequest.cs
- [ ] T072 [P] [US1] 建立 LoginRequest DTO 於 MemberService/src/MemberService.Application/DTOs/Auth/LoginRequest.cs
- [ ] T073 [P] [US1] 建立 AuthResponse DTO 於 MemberService/src/MemberService.Application/DTOs/Auth/AuthResponse.cs

### Validators (FluentValidation)

- [ ] T074 [US1] 實作 RegisterRequestValidator 於 MemberService/src/MemberService.Application/Validators/RegisterRequestValidator.cs (依賴 T071)
- [ ] T075 [US1] 實作 LoginRequestValidator 於 MemberService/src/MemberService.Application/Validators/LoginRequestValidator.cs (依賴 T072)

### 領域例外

- [ ] T076 [P] [US1] 實作 EmailAlreadyExistsException 於 MemberService/src/MemberService.Domain/Exceptions/EmailAlreadyExistsException.cs
- [ ] T077 [P] [US1] 實作 InvalidCredentialsException 於 MemberService/src/MemberService.Domain/Exceptions/InvalidCredentialsException.cs

### Services

- [ ] T078 [US1] 定義 IAuthService 介面於 MemberService/src/MemberService.Application/Services/IAuthService.cs
- [ ] T079 [US1] 實作 AuthService.Register 方法於 MemberService/src/MemberService.Application/Services/AuthService.cs (依賴 T074, T076)
- [ ] T080 [US1] 實作 AuthService.Login 方法於 MemberService/src/MemberService.Application/Services/AuthService.cs (依賴 T075, T077)

### Controllers

- [ ] T081 [US1] 實作 AuthController.Register 端點於 MemberService/src/MemberService.API/Controllers/AuthController.cs (依賴 T079)
- [ ] T082 [US1] 實作 AuthController.Login 端點於 MemberService/src/MemberService.API/Controllers/AuthController.cs (依賴 T080)

### 驗證

- [ ] T083 [US1] 執行所有 US1 測試並確保通過 (覆蓋率 >80%)
- [ ] T084 [US1] 手動測試註冊與登入流程（Postman/curl）
- [ ] T085 [US1] 驗證錯誤情境：重複電子郵件、密碼過短、無效電子郵件格式、錯誤密碼

**檢查點**: 此時 User Story 1 應完全可運作並可獨立測試

---

## Phase 4: User Story 2 - 權杖更新 (Priority: P2)

**目標**: JWT 過期後使用 Refresh Token 換取新 JWT，無需重新登入

**獨立測試**: 使用有效 Refresh Token 請求新 JWT，驗證系統回傳新權杖

**對應端點**: 
- POST /api/auth/refresh-token
- POST /api/auth/logout

**對應實體**: RefreshToken

### 測試 (TDD - 先寫測試)

- [ ] T086 [P] [US2] 撰寫 RefreshTokenRequestValidator 測試於 MemberService/tests/MemberService.Application.Tests/Validators/RefreshTokenRequestValidatorTests.cs
- [ ] T087 [P] [US2] 撰寫 AuthService.RefreshToken 測試於 MemberService/tests/MemberService.Application.Tests/Services/AuthServiceTests.cs
- [ ] T088 [P] [US2] 撰寫 AuthService.Logout 測試於 MemberService/tests/MemberService.Application.Tests/Services/AuthServiceTests.cs
- [ ] T089 [P] [US2] 撰寫 AuthController.RefreshToken 整合測試於 MemberService/tests/MemberService.IntegrationTests/API/AuthControllerTests.cs
- [ ] T090 [P] [US2] 撰寫 AuthController.Logout 整合測試於 MemberService/tests/MemberService.IntegrationTests/API/AuthControllerTests.cs

### DTOs

- [ ] T091 [P] [US2] 建立 RefreshTokenRequest DTO 於 MemberService/src/MemberService.Application/DTOs/Auth/RefreshTokenRequest.cs

### Validators

- [ ] T092 [US2] 實作 RefreshTokenRequestValidator 於 MemberService/src/MemberService.Application/Validators/RefreshTokenRequestValidator.cs (依賴 T091)

### 領域例外

- [ ] T093 [P] [US2] 實作 InvalidRefreshTokenException 於 MemberService/src/MemberService.Domain/Exceptions/InvalidRefreshTokenException.cs
- [ ] T094 [P] [US2] 實作 RefreshTokenExpiredException 於 MemberService/src/MemberService.Domain/Exceptions/RefreshTokenExpiredException.cs

### Services

- [ ] T095 [US2] 實作 AuthService.RefreshToken 方法於 MemberService/src/MemberService.Application/Services/AuthService.cs (依賴 T092, T093, T094)
- [ ] T096 [US2] 實作 AuthService.Logout 方法於 MemberService/src/MemberService.Application/Services/AuthService.cs

### Controllers

- [ ] T097 [US2] 實作 AuthController.RefreshToken 端點於 MemberService/src/MemberService.API/Controllers/AuthController.cs (依賴 T095)
- [ ] T098 [US2] 實作 AuthController.Logout 端點於 MemberService/src/MemberService.API/Controllers/AuthController.cs (依賴 T096)

### 驗證

- [ ] T099 [US2] 執行所有 US2 測試並確保通過 (覆蓋率 >80%)
- [ ] T100 [US2] 手動測試權杖更新流程
- [ ] T101 [US2] 驗證錯誤情境：過期 Token、被撤銷 Token、無效 Token

**檢查點**: 此時 User Stories 1 與 2 應都能獨立運作

---

## Phase 5: User Story 3 - 個人資料查詢 (Priority: P2)

**目標**: 使用者可查詢自己的完整資料，以及其他使用者的公開資料

**獨立測試**: 已登入使用者查詢自己的資料（完整），查詢其他使用者資料（僅公開欄位）

**對應端點**: 
- GET /api/users/me
- GET /api/users/{id}

**對應實體**: User

### 測試 (TDD - 先寫測試)

- [ ] T102 [P] [US3] 撰寫 UserService.GetCurrentUser 測試於 MemberService/tests/MemberService.Application.Tests/Services/UserServiceTests.cs
- [ ] T103 [P] [US3] 撰寫 UserService.GetUserById 測試於 MemberService/tests/MemberService.Application.Tests/Services/UserServiceTests.cs
- [ ] T104 [P] [US3] 撰寫 UsersController.GetMe 整合測試於 MemberService/tests/MemberService.IntegrationTests/API/UsersControllerTests.cs
- [ ] T105 [P] [US3] 撰寫 UsersController.GetUserById 整合測試於 MemberService/tests/MemberService.IntegrationTests/API/UsersControllerTests.cs

### DTOs

- [ ] T106 [P] [US3] 建立 UserProfileResponse DTO 於 MemberService/src/MemberService.Application/DTOs/Users/UserProfileResponse.cs
- [ ] T107 [P] [US3] 建立 UserPublicProfileResponse DTO 於 MemberService/src/MemberService.Application/DTOs/Users/UserPublicProfileResponse.cs

### 領域例外

- [ ] T108 [P] [US3] 實作 UserNotFoundException 於 MemberService/src/MemberService.Domain/Exceptions/UserNotFoundException.cs

### Services

- [ ] T109 [US3] 定義 IUserService 介面於 MemberService/src/MemberService.Application/Services/IUserService.cs
- [ ] T110 [US3] 實作 UserService.GetCurrentUser 方法於 MemberService/src/MemberService.Application/Services/UserService.cs (依賴 T106)
- [ ] T111 [US3] 實作 UserService.GetUserById 方法於 MemberService/src/MemberService.Application/Services/UserService.cs (依賴 T107, T108)

### Controllers

- [ ] T112 [US3] 實作 UsersController.GetMe 端點於 MemberService/src/MemberService.API/Controllers/UsersController.cs (依賴 T110)
- [ ] T113 [US3] 實作 UsersController.GetUserById 端點於 MemberService/src/MemberService.API/Controllers/UsersController.cs (依賴 T111)

### 驗證

- [ ] T114 [US3] 執行所有 US3 測試並確保通過 (覆蓋率 >80%)
- [ ] T115 [US3] 手動測試資料查詢流程
- [ ] T116 [US3] 驗證錯誤情境：未登入查詢、查詢不存在的使用者

**檢查點**: 所有使用者故事現在應該都能獨立運作

---

## Phase 6: User Story 4 - 個人資料更新與密碼變更 (Priority: P3)

**目標**: 使用者可更新自己的使用者名稱與電子郵件，以及變更密碼

**獨立測試**: 已登入使用者更新資料欄位，驗證系統正確更新並維持資料唯一性；透過密碼變更流程驗證舊密碼與新密碼設定

**對應端點**: 
- PUT /api/users/me
- PUT /api/users/me/password

**對應實體**: User, RefreshToken (密碼變更時撤銷所有 Token)

### 測試 (TDD - 先寫測試)

- [ ] T117 [P] [US4] 撰寫 UpdateProfileRequestValidator 測試於 MemberService/tests/MemberService.Application.Tests/Validators/UpdateProfileRequestValidatorTests.cs
- [ ] T118 [P] [US4] 撰寫 ChangePasswordRequestValidator 測試於 MemberService/tests/MemberService.Application.Tests/Validators/ChangePasswordRequestValidatorTests.cs
- [ ] T119 [P] [US4] 撰寫 UserService.UpdateProfile 測試於 MemberService/tests/MemberService.Application.Tests/Services/UserServiceTests.cs
- [ ] T120 [P] [US4] 撰寫 UserService.ChangePassword 測試於 MemberService/tests/MemberService.Application.Tests/Services/UserServiceTests.cs
- [ ] T121 [P] [US4] 撰寫 UsersController.UpdateProfile 整合測試於 MemberService/tests/MemberService.IntegrationTests/API/UsersControllerTests.cs
- [ ] T122 [P] [US4] 撰寫 UsersController.ChangePassword 整合測試於 MemberService/tests/MemberService.IntegrationTests/API/UsersControllerTests.cs

### DTOs

- [ ] T123 [P] [US4] 建立 UpdateProfileRequest DTO 於 MemberService/src/MemberService.Application/DTOs/Users/UpdateProfileRequest.cs
- [ ] T124 [P] [US4] 建立 ChangePasswordRequest DTO 於 MemberService/src/MemberService.Application/DTOs/Users/ChangePasswordRequest.cs

### Validators

- [ ] T125 [US4] 實作 UpdateProfileRequestValidator 於 MemberService/src/MemberService.Application/Validators/UpdateProfileRequestValidator.cs (依賴 T123)
- [ ] T126 [US4] 實作 ChangePasswordRequestValidator 於 MemberService/src/MemberService.Application/Validators/ChangePasswordRequestValidator.cs (依賴 T124)

### 領域例外

- [ ] T127 [P] [US4] 實作 InvalidPasswordException 於 MemberService/src/MemberService.Domain/Exceptions/InvalidPasswordException.cs

### Services

- [ ] T128 [US4] 實作 UserService.UpdateProfile 方法於 MemberService/src/MemberService.Application/Services/UserService.cs (依賴 T125)
- [ ] T129 [US4] 實作 UserService.ChangePassword 方法於 MemberService/src/MemberService.Application/Services/UserService.cs (依賴 T126, T127)

### Controllers

- [ ] T130 [US4] 實作 UsersController.UpdateProfile 端點於 MemberService/src/MemberService.API/Controllers/UsersController.cs (依賴 T128)
- [ ] T131 [US4] 實作 UsersController.ChangePassword 端點於 MemberService/src/MemberService.API/Controllers/UsersController.cs (依賴 T129)

### 驗證

- [ ] T132 [US4] 執行所有 US4 測試並確保通過 (覆蓋率 >80%)
- [ ] T133 [US4] 手動測試資料更新流程
- [ ] T134 [US4] 手動測試密碼變更流程
- [ ] T135 [US4] 驗證錯誤情境：更新為重複電子郵件、錯誤舊密碼、新密碼過短
- [ ] T136 [US4] 驗證密碼變更後所有 Refresh Token 被撤銷

**檢查點**: 所有核心功能現在應完整且可獨立測試

---

## Phase 7: Polish & Cross-Cutting Concerns (打磨與跨領域關注)

**目的**: 影響多個使用者故事的改進

- [ ] T137 [P] 建立 HealthController 於 MemberService/src/MemberService.API/Controllers/HealthController.cs
- [ ] T138 [P] 撰寫完整的 README.md 文件於 MemberService/README.md
- [ ] T139 [P] 撰寫 Docker 部署說明於 MemberService/README.md
- [ ] T140 [P] 撰寫環境變數設定指南於 MemberService/README.md
- [ ] T141 執行完整測試套件並確認覆蓋率 >80%
- [ ] T142 執行 quickstart.md 驗證（確保文件與實作一致）
- [ ] T143 [P] 效能測試：設定效能測試框架（BenchmarkDotNet 或 k6）
- [ ] T144 [P] 效能測試：定義負載情境（1000 並發使用者，常規操作混合）
- [ ] T145 [P] 效能測試：建立基準指標（JWT 驗證 <50ms p95，API 端點 <200ms p95）
- [ ] T146 效能測試：執行並驗證 JWT 驗證延遲 <50ms p95
- [ ] T147 效能測試：執行並驗證 API 端點回應時間 <200ms p95
- [ ] T148 [P] 程式碼審查與重構
- [ ] T149 [P] 安全性檢查（JWT 密鑰管理、SQL 注入防護、CORS 設定）
- [ ] T150 [P] 日誌完整性檢查（所有關鍵操作都有結構化日誌，包含 UserId、執行時間、錯誤堆疊）
- [ ] T151 建立 CI/CD Pipeline 設定檔
- [ ] T152 建立 Kubernetes 部署檔案（如需要）
- [ ] T153 最終整合測試（所有使用者故事端到端測試）

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: 無相依性 - 可立即開始
- **Foundational (Phase 2)**: 依賴 Setup 完成 - 阻塞所有使用者故事
- **User Stories (Phase 3-6)**: 全部依賴 Foundational 完成
  - 使用者故事可平行進行（如有足夠人力）
  - 或依優先順序循序執行（P1 → P2 → P3）
- **Polish (Phase 7)**: 依賴所有期望的使用者故事完成

### User Story Dependencies

- **User Story 1 (P1)**: Foundational 完成後可開始 - 無其他故事相依性 ✅ MVP
- **User Story 2 (P2)**: Foundational 完成後可開始 - 與 US1 整合但可獨立測試
- **User Story 3 (P2)**: Foundational 完成後可開始 - 與 US1 整合但可獨立測試
- **User Story 4 (P3)**: Foundational 完成後可開始 - 與 US1/US2 整合但可獨立測試

### Within Each User Story

- 測試必須先撰寫並失敗（紅燈）才能實作
- DTOs 在 Validators 之前
- Validators 在 Services 之前
- Services 在 Controllers 之前
- 核心實作在整合之前
- 故事完成後才移到下一優先順序

### Parallel Opportunities

- 所有標記 [P] 的 Setup 任務可平行執行
- 所有標記 [P] 的 Foundational 任務可在 Phase 2 內平行執行
- Foundational 完成後，所有使用者故事可平行開始（如有團隊容量）
- 每個故事內標記 [P] 的測試可平行執行
- 每個故事內標記 [P] 的 DTOs/例外可平行執行
- 不同使用者故事可由不同團隊成員平行處理

---

## Parallel Example: User Story 1

```bash
# 同時啟動 User Story 1 的所有測試：
Task: "撰寫 RegisterRequestValidator 測試" (T065)
Task: "撰寫 LoginRequestValidator 測試" (T066)
Task: "撰寫 AuthService.Register 測試" (T067)
Task: "撰寫 AuthService.Login 測試" (T068)
Task: "撰寫 AuthController.Register 整合測試" (T069)
Task: "撰寫 AuthController.Login 整合測試" (T070)

# 同時建立 User Story 1 的所有 DTOs：
Task: "建立 RegisterRequest DTO" (T071)
Task: "建立 LoginRequest DTO" (T072)
Task: "建立 AuthResponse DTO" (T073)

# 同時建立 User Story 1 的所有領域例外：
Task: "實作 EmailAlreadyExistsException" (T076)
Task: "實作 InvalidCredentialsException" (T077)
```

---

## Implementation Strategy

### MVP First (僅 User Story 1)

1. 完成 Phase 1: Setup
2. 完成 Phase 2: Foundational（關鍵 - 阻塞所有故事）
3. 完成 Phase 3: User Story 1
4. **停止並驗證**: 獨立測試 User Story 1
5. 如就緒則部署/展示

### Incremental Delivery (增量交付)

1. 完成 Setup + Foundational → 基礎就緒
2. 加入 User Story 1 → 獨立測試 → 部署/展示（MVP！）
3. 加入 User Story 2 → 獨立測試 → 部署/展示
4. 加入 User Story 3 → 獨立測試 → 部署/展示
5. 加入 User Story 4 → 獨立測試 → 部署/展示
6. 每個故事都增加價值而不破壞先前故事

### Parallel Team Strategy (平行團隊策略)

多位開發者時：

1. 團隊一起完成 Setup + Foundational
2. Foundational 完成後：
   - 開發者 A: User Story 1
   - 開發者 B: User Story 2
   - 開發者 C: User Story 3
   - 開發者 D: User Story 4
3. 故事獨立完成並整合

---

## Task Summary

**總任務數**: 153 個任務
**依使用者故事分布**:
- Setup (Phase 1): 25 個任務
- Foundational (Phase 2): 39 個任務（T026-T064）
- User Story 1 (P1): 21 個任務（T065-T085）- 註冊與登入 🎯 MVP
- User Story 2 (P2): 16 個任務（T086-T101）- 權杖更新
- User Story 3 (P2): 15 個任務（T102-T116）- 個人資料查詢
- User Story 4 (P3): 20 個任務（T117-T136）- 資料更新與密碼變更
- Polish (Phase 7): 17 個任務（T137-T153）

**平行機會**: 
- Phase 1 中 18 個任務可平行執行
- Phase 2 中 22 個任務可平行執行
- 每個使用者故事內的測試任務可平行執行
- 每個使用者故事內的 DTOs 與例外可平行執行
- Foundational 完成後，4 個使用者故事可由不同開發者平行處理

**MVP 範圍建議**: 
- Phase 1 (Setup) + Phase 2 (Foundational) + Phase 3 (User Story 1)
- 約 85 個任務
- 提供完整的註冊與登入功能
- 可獨立部署並展示價值

**測試覆蓋率目標**: >80% (符合 Constitution 要求)

---

## Notes

- [P] 任務 = 不同檔案，無相依性，可平行執行
- [Story] 標籤將任務映射到特定使用者故事以便追蹤
- 每個使用者故事應該可獨立完成並測試
- TDD 強制執行：先寫測試（紅燈），再實作（綠燈），最後重構
- 在實作前驗證測試失敗
- 每個任務或邏輯群組後提交
- 在每個檢查點停下來獨立驗證故事
- 避免：模糊任務、相同檔案衝突、破壞獨立性的跨故事相依性
