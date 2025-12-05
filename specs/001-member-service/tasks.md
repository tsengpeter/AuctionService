# Tasks: MemberService

**Input**: Design documents from `/specs/001-member-service/`
**Prerequisites**: plan.md, spec.md, data-model.md, contracts/openapi.yaml, research.md

**Tests**: 本文案採用 TDD 開發流程，所有任務都有測試任務,測試覆蓋率目標 >80%。

**Organization**: 任務按使用者故事組織。使用者故事可以獨立實作與測試。

## Format: `[ID] [P?] [Story] Description`

- **[P]**: 可平行執行，不會檔案，無依賴問題
- **[Story]**: 任務所屬的使用者故事（例如：US1, US2, US3, US4）
- 描述包含確切的檔案路徑

## Path Conventions

專案使用單一 MemberService/ 資料夾結構：
- **原始碼**: `MemberService/src/`
- **測試**: `MemberService/tests/`
- **專案根目錄**: `MemberService/` (包含解決方案、Docker、README 等)

---

## Phase 1: Setup (共享基礎設施)

**目的**: 專案結構與基本檔案結構建立

- [X] T001 建立 MemberService/ 專案根目錄與基本檔案結構
- [X] T002 建立 MemberService.sln 解決方案檔案
- [X] T003 [P] 建立 Domain 專案: MemberService/src/MemberService.Domain/MemberService.Domain.csproj (.NET 10)
- [X] T004 [P] 建立 Application 專案: MemberService/src/MemberService.Application/MemberService.Application.csproj (.NET 10)
- [X] T005 [P] 建立 Infrastructure 專案: MemberService/src/MemberService.Infrastructure/MemberService.Infrastructure.csproj (.NET 10)
- [X] T006 [P] 建立 API 專案: MemberService/src/MemberService.API/MemberService.API.csproj (ASP.NET Core 10 Web API)
- [X] T007 [P] 建立 Domain.Tests 專案: MemberService/tests/MemberService.Domain.Tests/MemberService.Domain.Tests.csproj
- [X] T008 [P] 建立 Application.Tests 專案: MemberService/tests/MemberService.Application.Tests/MemberService.Application.Tests.csproj
- [X] T009 [P] 建立 Infrastructure.Tests 專案: MemberService/tests/MemberService.Infrastructure.Tests/MemberService.Infrastructure.Tests.csproj
- [X] T010 [P] 建立 IntegrationTests 專案: MemberService/tests/MemberService.IntegrationTests/MemberService.IntegrationTests.csproj
- [X] T011 設定專案參考（Domain → Application → Infrastructure → API）
- [X] T012 [P] 安裝 IdGen 3.x 至 Infrastructure 專案
- [X] T013 [P] 安裝 BCrypt.Net-Next 4.0.3 至 Infrastructure 專案
- [X] T014 [P] 安裝 System.IdentityModel.Tokens.Jwt 8.0.0 至 Infrastructure 專案
- [X] T015 [P] 安裝 Npgsql.EntityFrameworkCore.PostgreSQL 10.0 至 Infrastructure 專案
- [X] T016 [P] 安裝 FluentValidation.AspNetCore 11.3.0 至 Application 專案
- [X] T017 [P] 安裝 Serilog.AspNetCore 8.0 至 API 專案
- [X] T018 [P] 安裝 xUnit 2.6+、Moq 4.20+、FluentAssertions 6.12+ 到所有測試專案
- [X] T019 [P] 安裝 Testcontainers.PostgreSql 3.7+ 至 IntegrationTests 專案
- [X] T020 [P] 建立 Dockerfile 於 MemberService/Dockerfile
- [X] T021 [P] 建立 docker-compose.yml 於 MemberService/docker-compose.yml
- [X] T022 [P] 建立 README.md 於 MemberService/README.md
- [ ] T023 [P] 建立 .gitignore 於 MemberService/.gitignore
- [ ] T024 [P] 建立 .editorconfig 於 MemberService/.editorconfig
- [ ] T025 [P] 建立 global.json 於 MemberService/global.json (指定 .NET 10 SDK)
- [ ] T026 [P] 建立 docs 資料夾於 MemberService/docs/
- [ ] T027 [P] 建立 architecture.md 於 MemberService/docs/architecture.md
- [ ] T028 [P] 建立 api-guide.md 於 MemberService/docs/api-guide.md
- [ ] T029 [P] 建立 deployment.md 於 MemberService/docs/deployment.md
- [ ] T030 [P] 建立 scripts 資料夾於 MemberService/scripts/
- [ ] T031 [P] 建立 build.sh 於 MemberService/scripts/build.sh
- [ ] T032 [P] 建立 build.ps1 於 MemberService/scripts/build.ps1
- [ ] T033 [P] 建立 init-db.sql 於 MemberService/scripts/init-db.sql
- [ ] T034 [P] 建立 run-tests.sh 於 MemberService/scripts/run-tests.sh
- [ ] T035 [P] 建立 .github 資料夾於 MemberService/.github/
- [ ] T036 [P] 建立 workflows 資料夾於 MemberService/.github/workflows/
- [ ] T037 [P] 建立 build.yml 於 MemberService/.github/workflows/build.yml
- [ ] T038 [P] 建立 test.yml 於 MemberService/.github/workflows/test.yml

---

## Phase 2: Foundational (基礎決策層)

**目的**: 所有使用者故事都依賴的核心基礎設施

**並行關鍵**: 所有使用者故事工作都在此階段後才能開始

- [X] T039 [P] 實作 DomainException 基底類別於 MemberService/src/MemberService.Domain/Exceptions/DomainException.cs
- [X] T040 [P] 測試 DomainException 於 MemberService/tests/MemberService.Domain.Tests/Exceptions/DomainExceptionTests.cs
- [X] T041 [P] 實作 Email Value Object 於 MemberService/src/MemberService.Domain/ValueObjects/Email.cs
- [X] T042 [P] 測試 Email Value Object 於 MemberService/tests/MemberService.Domain.Tests/ValueObjects/EmailTests.cs
- [X] T043 [P] 實作 Password Value Object 於 MemberService/src/MemberService.Domain/ValueObjects/Password.cs
- [X] T044 [P] 測試 Password Value Object 於 MemberService/tests/MemberService.Domain.Tests/ValueObjects/PasswordTests.cs
- [X] T045 [P] 實作 Username Value Object 於 MemberService/src/MemberService.Domain/ValueObjects/Username.cs
- [X] T046 [P] 測試 Username Value Object 於 MemberService/tests/MemberService.Domain.Tests/ValueObjects/UsernameTests.cs
- [X] T047 [P] 實作 User 實體於 MemberService/src/MemberService.Domain/Entities/User.cs
- [X] T048 [P] 測試 User 實體於 MemberService/tests/MemberService.Domain.Tests/Entities/UserTests.cs
- [X] T049 [P] 實作 RefreshToken 實體於 MemberService/src/MemberService.Domain/Entities/RefreshToken.cs
- [X] T050 [P] 測試 RefreshToken 實體於 MemberService/tests/MemberService.Domain.Tests/Entities/RefreshTokenTests.cs
- [X] T051 [P] 定義 IUserRepository 介面於 MemberService/src/MemberService.Domain/Interfaces/IUserRepository.cs
- [X] T052 [P] 定義 IRefreshTokenRepository 介面於 MemberService/src/MemberService.Domain/Interfaces/IRefreshTokenRepository.cs
- [X] T053 [P] 定義 IPasswordHasher 介面於 MemberService/src/MemberService.Domain/Interfaces/IPasswordHasher.cs
- [X] T054 [P] 定義 ITokenGenerator 介面於 MemberService/src/MemberService.Domain/Interfaces/ITokenGenerator.cs
- [X] T055 [P] 定義 IIdGenerator 介面於 MemberService/src/MemberService.Domain/Interfaces/IIdGenerator.cs
- [X] T056 實作 SnowflakeIdGenerator 於 MemberService/src/MemberService.Infrastructure/IdGeneration/SnowflakeIdGenerator.cs
- [X] T057 測試 SnowflakeIdGenerator 於 MemberService/tests/MemberService.Infrastructure.Tests/IdGeneration/SnowflakeIdGeneratorTests.cs
- [X] T058 實作 BCryptPasswordHasher 於 MemberService/src/MemberService.Infrastructure/Security/BCryptPasswordHasher.cs
- [X] T059 測試 BCryptPasswordHasher 於 MemberService/tests/MemberService.Infrastructure.Tests/Security/BCryptPasswordHasherTests.cs
- [X] T060 實作 JwtTokenGenerator 於 MemberService/src/MemberService.Infrastructure/Security/JwtTokenGenerator.cs
- [X] T061 測試 JwtTokenGenerator 於 MemberService/tests/MemberService.Infrastructure.Tests/Security/JwtTokenGeneratorTests.cs
- [X] T062 實作 RefreshTokenGenerator 於 MemberService/src/MemberService.Infrastructure/Security/RefreshTokenGenerator.cs
- [X] T063 測試 RefreshTokenGenerator 於 MemberService/tests/MemberService.Infrastructure.Tests/Security/RefreshTokenGeneratorTests.cs
- [X] T064 建立 MemberDbContext 於 MemberService/src/MemberService.Infrastructure/Persistence/MemberDbContext.cs
- [X] T065 [P] 建立 UserConfiguration (EF Core) 於 MemberService/src/MemberService.Infrastructure/Persistence/Configurations/UserConfiguration.cs
- [X] T066 [P] 建立 RefreshTokenConfiguration (EF Core) 於 MemberService/src/MemberService.Infrastructure/Persistence/Configurations/RefreshTokenConfiguration.cs
- [X] T067 實作 UserRepository 於 MemberService/src/MemberService.Infrastructure/Persistence/Repositories/UserRepository.cs
- [X] T068 測試 UserRepository 於 MemberService/tests/MemberService.Infrastructure.Tests/Persistence/UserRepositoryTests.cs
- [X] T069 實作 RefreshTokenRepository 於 MemberService/src/MemberService.Infrastructure/Persistence/Repositories/RefreshTokenRepository.cs
- [X] T070 測試 RefreshTokenRepository 於 MemberService/tests/MemberService.Infrastructure.Tests/Persistence/RefreshTokenRepositoryTests.cs
- [X] T071 建立 EF Core Migration: InitialCreate
- [X] T072 實作 GlobalExceptionHandler 於 MemberService/src/MemberService.API/Middlewares/GlobalExceptionHandler.cs
- [X] T073 實作 RequestLoggingMiddleware 於 MemberService/src/MemberService.API/Middlewares/RequestLoggingMiddleware.cs
- [X] T074 設定 Program.cs 依賴注入與中介軟體管線於 MemberService/src/MemberService.API/Program.cs
- [X] T075 設定 appsettings.json 於 MemberService/src/MemberService.API/appsettings.json
- [X] T076 設定 appsettings.Development.json 於 MemberService/src/MemberService.API/appsettings.Development.json
- [ ] T077 建立 PostgreSqlContainerFixture 於 MemberService/tests/MemberService.IntegrationTests/TestFixtures/PostgreSqlContainerFixture.cs

**檢查點**: 基礎設施就緒 - 使用者故事實作現在可以平行開始

---

## Phase 3: User Story 1 - 讓使用者註冊與登入 (Priority: P1) 【含 MVP】

**目的**: 使用者可以註冊新帳號並自動登入，取得 JWT 存取權杖以使用其他功能

**驗收測試**: 提交完整註冊訊息（電子郵件、密碼、使用者名稱），驗證系統建立帳號並返傳 JWT + Refresh Token

**對應端點**: 
- POST /api/auth/register
- POST /api/auth/login

**對應實體**: User, RefreshToken

### 測試 (TDD - 先寫測試)

- [X] T078 [P] [US1] 先寫 RegisterRequestValidator 測試於 MemberService/tests/MemberService.Application.Tests/Validators/RegisterRequestValidatorTests.cs
- [X] T079 [P] [US1] 先寫 LoginRequestValidator 測試於 MemberService/tests/MemberService.Application.Tests/Validators/LoginRequestValidatorTests.cs
- [X] T080 [P] [US1] 先寫 AuthService.Register 測試於 MemberService/tests/MemberService.Application.Tests/Services/AuthServiceTests.cs
- [X] T081 [P] [US1] 先寫 AuthService.Login 測試於 MemberService/tests/MemberService.Application.Tests/Services/AuthServiceTests.cs
- [ ] T082 [P] [US1] 先寫 AuthController.Register 整合測試於 MemberService/tests/MemberService.IntegrationTests/API/AuthControllerTests.cs
- [ ] T083 [P] [US1] 先寫 AuthController.Login 整合測試於 MemberService/tests/MemberService.IntegrationTests/API/AuthControllerTests.cs

### DTOs

- [X] T084 [P] [US1] 建立 RegisterRequest DTO 於 MemberService/src/MemberService.Application/DTOs/Auth/RegisterRequest.cs
- [X] T085 [P] [US1] 建立 LoginRequest DTO 於 MemberService/src/MemberService.Application/DTOs/Auth/LoginRequest.cs
- [X] T086 [P] [US1] 建立 AuthResponse DTO 於 MemberService/src/MemberService.Application/DTOs/Auth/AuthResponse.cs

### Validators (FluentValidation)

- [X] T087 [US1] 實作 RegisterRequestValidator 於 MemberService/src/MemberService.Application/Validators/RegisterRequestValidator.cs (依賴 T084)
- [X] T088 [US1] 實作 LoginRequestValidator 於 MemberService/src/MemberService.Application/Validators/LoginRequestValidator.cs (依賴 T085)

### 異常例外

- [X] T089 [P] [US1] 實作 EmailAlreadyExistsException 於 MemberService/src/MemberService.Domain/Exceptions/EmailAlreadyExistsException.cs
- [X] T090 [P] [US1] 實作 InvalidCredentialsException 於 MemberService/src/MemberService.Domain/Exceptions/InvalidCredentialsException.cs

### Services

- [X] T091 [US1] 定義 IAuthService 介面於 MemberService/src/MemberService.Application/Services/IAuthService.cs
- [X] T092 [US1] 實作 AuthService.Register 方法於 MemberService/src/MemberService.Application/Services/AuthService.cs (依賴 T087, T089)
- [X] T093 [US1] 實作 AuthService.Login 方法於 MemberService/src/MemberService.Application/Services/AuthService.cs (依賴 T088, T090)

### Controllers

- [X] T094 [US1] 實作 AuthController.Register 端點於 MemberService/src/MemberService.API/Controllers/AuthController.cs (依賴 T092)
- [X] T095 [US1] 實作 AuthController.Login 端點於 MemberService/src/MemberService.API/Controllers/AuthController.cs (依賴 T093)

### 驗證

- [ ] T096 [US1] 執行所有 US1 測試並確保通過 (覆蓋率 >80%)
- [ ] T097 [US1] 手動測試註冊與登入流程（Postman/curl）
- [ ] T098 [US1] 驗證錯誤處理：重複電子郵件、密碼太短、無效電子郵件格式、錯誤憑證

**檢查點**: 此時 User Story 1 功能可運行並可進行測試

---

## Phase 4: User Story 2 - 權杖更新 (Priority: P2)

**目的**: JWT 過期後使用 Refresh Token 取得新 JWT，無需重新登入

**驗收測試**: 使用有效 Refresh Token 請求新 JWT，驗證系統返回新權杖

**對應端點**: 
- POST /api/auth/refresh-token
- POST /api/auth/logout

**對應實體**: RefreshToken

### 測試 (TDD - 先寫測試)

- [X] T099 [P] [US2] 先寫 RefreshTokenRequestValidator 測試於 MemberService/tests/MemberService.Application.Tests/Validators/RefreshTokenRequestValidatorTests.cs
- [ ] T100 [P] [US2] 先寫 AuthService.RefreshToken 測試於 MemberService/tests/MemberService.Application.Tests/Services/AuthServiceTests.cs
- [ ] T101 [P] [US2] 先寫 AuthService.Logout 測試於 MemberService/tests/MemberService.Application.Tests/Services/AuthServiceTests.cs
- [ ] T102 [P] [US2] 先寫 AuthController.RefreshToken 整合測試於 MemberService/tests/MemberService.IntegrationTests/API/AuthControllerTests.cs
- [ ] T103 [P] [US2] 先寫 AuthController.Logout 整合測試於 MemberService/tests/MemberService.IntegrationTests/API/AuthControllerTests.cs

### DTOs

- [X] T104 [P] [US2] 建立 RefreshTokenRequest DTO 於 MemberService/src/MemberService.Application/DTOs/Auth/RefreshTokenRequest.cs

### Validators

- [X] T105 [US2] 實作 RefreshTokenRequestValidator 於 MemberService/src/MemberService.Application/Validators/RefreshTokenRequestValidator.cs (依賴 T104)

### 異常例外

- [X] T106 [P] [US2] 實作 InvalidRefreshTokenException 於 MemberService/src/MemberService.Domain/Exceptions/InvalidRefreshTokenException.cs
- [X] T107 [P] [US2] 實作 RefreshTokenExpiredException 於 MemberService/src/MemberService.Domain/Exceptions/RefreshTokenExpiredException.cs

### Services

- [X] T108 [US2] 實作 AuthService.RefreshToken 方法於 MemberService/src/MemberService.Application/Services/AuthService.cs (依賴 T105, T106, T107)
- [X] T109 [US2] 實作 AuthService.Logout 方法於 MemberService/src/MemberService.Application/Services/AuthService.cs

### Controllers

- [X] T110 [US2] 實作 AuthController.RefreshToken 端點於 MemberService/src/MemberService.API/Controllers/AuthController.cs (依賴 T108)
- [X] T111 [US2] 實作 AuthController.Logout 端點於 MemberService/src/MemberService.API/Controllers/AuthController.cs (依賴 T109)

### 驗證

- [ ] T112 [US2] 執行所有 US2 測試並確保通過 (覆蓋率 >80%)
- [ ] T113 [US2] 手動測試權杖更新流程
- [ ] T114 [US2] 驗證錯誤處理：過期 Token、被撤銷 Token、無效 Token

**檢查點**: 此時 User Stories 1 和 2 均已獨立完成

---

## Phase 5: User Story 3 - 個人資訊查詢 (Priority: P2)

**目的**: 使用者可查詢自己的完整資訊以及查看其他使用者的公開資訊

**驗收測試**: 已登入使用者查詢自己的資訊（完整），查詢其他使用者的完整公開欄位

**對應端點**: 
- GET /api/users/me
- GET /api/users/{id}

**對應實體**: User

### 測試 (TDD - 先寫測試)

- [ ] T115 [P] [US3] 先寫 UserService.GetCurrentUser 測試於 MemberService/tests/MemberService.Application.Tests/Services/UserServiceTests.cs
- [ ] T116 [P] [US3] 先寫 UserService.GetUserById 測試於 MemberService/tests/MemberService.Application.Tests/Services/UserServiceTests.cs
- [ ] T117 [P] [US3] 先寫 UsersController.GetMe 整合測試於 MemberService/tests/MemberService.IntegrationTests/API/UsersControllerTests.cs
- [ ] T118 [P] [US3] 先寫 UsersController.GetUserById 整合測試於 MemberService/tests/MemberService.IntegrationTests/API/UsersControllerTests.cs

### DTOs

- [ ] T119 [P] [US3] 建立 UserProfileResponse DTO 於 MemberService/src/MemberService.Application/DTOs/Users/UserProfileResponse.cs
- [ ] T120 [P] [US3] 建立 UserPublicProfileResponse DTO 於 MemberService/src/MemberService.Application/DTOs/Users/UserPublicProfileResponse.cs

### 異常例外

- [ ] T121 [P] [US3] 實作 UserNotFoundException 於 MemberService/src/MemberService.Domain/Exceptions/UserNotFoundException.cs

### Services

- [ ] T122 [US3] 定義 IUserService 介面於 MemberService/src/MemberService.Application/Services/IUserService.cs
- [ ] T123 [US3] 實作 UserService.GetCurrentUser 方法於 MemberService/src/MemberService.Application/Services/UserService.cs (依賴 T119)
- [ ] T124 [US3] 實作 UserService.GetUserById 方法於 MemberService/src/MemberService.Application/Services/UserService.cs (依賴 T120, T121)

### Controllers

- [ ] T125 [US3] 實作 UsersController.GetMe 端點於 MemberService/src/MemberService.API/Controllers/UsersController.cs (依賴 T123)
- [ ] T126 [US3] 實作 UsersController.GetUserById 端點於 MemberService/src/MemberService.API/Controllers/UsersController.cs (依賴 T124)

### 驗證

- [ ] T127 [US3] 執行所有 US3 測試並確保通過 (覆蓋率 >80%)
- [ ] T128 [US3] 手動測試資訊查詢流程
- [ ] T129 [US3] 驗證錯誤處理：未登入查詢、查詢不存在的使用者

**檢查點**: 所有使用者故事現在均應都獨立完成

---

## Phase 6: User Story 4 - 個人資訊更新與密碼變更 (Priority: P3)

**目的**: 使用者可更新自己的使用者名稱與電子郵件，以及變更密碼

**驗收測試**: 已登入使用者更新個人欄位。驗證系統正確更新並維持唯一性約束。密碼變更流程驗證舊密碼、設定新密碼

**對應端點**: 
- PUT /api/users/me
- PUT /api/users/me/password

**對應實體**: User, RefreshToken (密碼變更時撤銷所有 Token)

### 測試 (TDD - 先寫測試)

- [ ] T130 [P] [US4] 先寫 UpdateProfileRequestValidator 測試於 MemberService/tests/MemberService.Application.Tests/Validators/UpdateProfileRequestValidatorTests.cs
- [ ] T131 [P] [US4] 先寫 ChangePasswordRequestValidator 測試於 MemberService/tests/MemberService.Application.Tests/Validators/ChangePasswordRequestValidatorTests.cs
- [ ] T132 [P] [US4] 先寫 UserService.UpdateProfile 測試於 MemberService/tests/MemberService.Application.Tests/Services/UserServiceTests.cs
- [ ] T133 [P] [US4] 先寫 UserService.ChangePassword 測試於 MemberService/tests/MemberService.Application.Tests/Services/UserServiceTests.cs
- [ ] T134 [P] [US4] 先寫 UsersController.UpdateProfile 整合測試於 MemberService/tests/MemberService.IntegrationTests/API/UsersControllerTests.cs
- [ ] T135 [P] [US4] 先寫 UsersController.ChangePassword 整合測試於 MemberService/tests/MemberService.IntegrationTests/API/UsersControllerTests.cs

### DTOs

- [ ] T136 [P] [US4] 建立 UpdateProfileRequest DTO 於 MemberService/src/MemberService.Application/DTOs/Users/UpdateProfileRequest.cs
- [ ] T137 [P] [US4] 建立 ChangePasswordRequest DTO 於 MemberService/src/MemberService.Application/DTOs/Users/ChangePasswordRequest.cs

### Validators

- [ ] T138 [US4] 實作 UpdateProfileRequestValidator 於 MemberService/src/MemberService.Application/Validators/UpdateProfileRequestValidator.cs (依賴 T136)
- [ ] T139 [US4] 實作 ChangePasswordRequestValidator 於 MemberService/src/MemberService.Application/Validators/ChangePasswordRequestValidator.cs (依賴 T137)

### 異常例外

- [ ] T140 [P] [US4] 實作 InvalidPasswordException 於 MemberService/src/MemberService.Domain/Exceptions/InvalidPasswordException.cs

### Services

- [ ] T141 [US4] 實作 UserService.UpdateProfile 方法於 MemberService/src/MemberService.Application/Services/UserService.cs (依賴 T138)
- [ ] T142 [US4] 實作 UserService.ChangePassword 方法於 MemberService/src/MemberService.Application/Services/UserService.cs (依賴 T139, T140)

### Controllers

- [ ] T143 [US4] 實作 UsersController.UpdateProfile 端點於 MemberService/src/MemberService.API/Controllers/UsersController.cs (依賴 T141)
- [ ] T144 [US4] 實作 UsersController.ChangePassword 端點於 MemberService/src/MemberService.API/Controllers/UsersController.cs (依賴 T142)

### 驗證

- [ ] T145 [US4] 執行所有 US4 測試並確保通過 (覆蓋率 >80%)
- [ ] T146 [US4] 手動測試資訊更新流程
- [ ] T147 [US4] 手動測試密碼變更流程
- [ ] T148 [US4] 驗證錯誤處理：更新為已存在電子郵件、錯誤舊密碼、新密碼太短
- [ ] T149 [US4] 驗證密碼變更後所有 Refresh Token 被撤銷

**檢查點**: 所有核心功能現已完整且可進行測試

---

## Phase 7: Polish & Cross-Cutting Concerns (打磨與跨領域關注)

**目的**: 影響多個使用者故事的功能

- [ ] T150 [P] 建立 HealthController 於 MemberService/src/MemberService.API/Controllers/HealthController.cs
- [ ] T151 [P] 撰寫完整的 README.md 文件於 MemberService/README.md
- [ ] T152 [P] 撰寫 Docker 部署說明於 MemberService/README.md
- [ ] T153 [P] 撰寫環境變數設定指南於 MemberService/README.md
- [ ] T154 執行完整測試套件並確認總覆蓋率 >80%
- [ ] T155 驗證 quickstart.md 驗證（確保文件與實作一致）
- [ ] T156 [P] 性能測試：設定性能測試工具（BenchmarkDotNet 或 k6）
- [ ] T157 [P] 性能測試：定義負載環境（1000 並發使用者、正常操作混合）
- [ ] T158 [P] 性能測試：建立基準指標（JWT 驗證 <50ms p95，API 端點 <200ms p95）
- [ ] T159 性能測試：執行並驗證 JWT 驗證延遲 <50ms p95
- [ ] T160 性能測試：執行並驗證 API 端點響應時間 <200ms p95
- [ ] T161 [P] 程式碼審查準備
- [ ] T162 [P] 安全性檢查（JWT 密鑰管理、SQL 注入保護、CORS 設定）
- [ ] T163 [P] 完整性檢查（所有操作都記錄結構化日誌，含 UserId、執行時間、錯誤訊息）
- [ ] T164 建立 CI/CD Pipeline 設定
- [ ] T165 建立 Kubernetes 部署檔案（如需要）
- [ ] T166 最終整合測試（所有使用者故事端到端測試）

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: 無相依性 - 可以立即開始
- **Foundational (Phase 2)**: 依賴 Setup 完成 - 可以大部分平行開始使用者故事
- **User Stories (Phase 3-6)**: 全部依賴 Foundational 完成
  - 使用者故事可平行開發（如有足夠人力）
  - 建議按優先循序開發（P1 → P2 → P3）
- **Polish (Phase 7)**: 依賴所有核心使用者故事完成

### User Story Dependencies

- **User Story 1 (P1)**: Foundational 完成後可開始 - 與其他故事相依性最低。MVP
- **User Story 2 (P2)**: Foundational 完成後可開始 - 和 US1 弱依賴但可獨立測試
- **User Story 3 (P2)**: Foundational 完成後可開始 - 和 US1 弱依賴但可獨立測試
- **User Story 4 (P3)**: Foundational 完成後可開始 - 和 US1/US2 弱依賴但可獨立測試

### Within Each User Story

- 測試必須先撰寫並失敗（紅燈）才能實作
- DTOs 與 Validators 之間
- Validators 與 Services 之間
- Services 與 Controllers 之間
- 完整實作後整合驗證
- 驗收完成後才移到下一階段

### Parallel Opportunities

- 所有標記 [P] 的 Setup 任務可平行執行
- 所有標記 [P] 的 Foundational 任務在 Phase 2 可平行執行
- Foundational 完成後，所有使用者故事可平行開發（如有適當資源）
- 每個故事內標記 [P] 的測試可平行撰寫
- 每個故事內標記 [P] 的 DTOs/例外可平行執行
- 不同使用者故事可由不同開發者平行開發

---

## Parallel Example: User Story 1

```bash
# 可以同時撰寫 User Story 1 的所有測試：
Task: "先寫 RegisterRequestValidator 測試" (T078)
Task: "先寫 LoginRequestValidator 測試" (T079)
Task: "先寫 AuthService.Register 測試" (T080)
Task: "先寫 AuthService.Login 測試" (T081)
Task: "先寫 AuthController.Register 整合測試" (T082)
Task: "先寫 AuthController.Login 整合測試" (T083)

# 可以同時建立 User Story 1 的所有 DTOs：
Task: "建立 RegisterRequest DTO" (T084)
Task: "建立 LoginRequest DTO" (T085)
Task: "建立 AuthResponse DTO" (T086)

# 可以同時建立 User Story 1 的所有異常例外：
Task: "實作 EmailAlreadyExistsException" (T089)
Task: "實作 InvalidCredentialsException" (T090)
```

---

## Implementation Strategy

### MVP First (即 User Story 1)

1. 完成 Phase 1: Setup
2. 完成 Phase 2: Foundational（關鍵 - 釋放所有故事）
3. 完成 Phase 3: User Story 1
4. **暫停並演示**: 可以測試 User Story 1
5. 如就緒，可部署/展示

### Incremental Delivery (增量交付)

1. 完成 Setup + Foundational → 可以就緒
2. 加入 User Story 1 → 可以測試 → 可部署/展示（MVP！）
3. 加入 User Story 2 → 可以測試 → 可部署/展示
4. 加入 User Story 3 → 可以測試 → 可部署/展示
5. 加入 User Story 4 → 可以測試 → 可部署/展示
6. 每個故事都增加價值而不需完全完成

### Parallel Team Strategy (平行團隊策略)

多個開發者可以：

1. 一起完成 Setup + Foundational
2. Foundational 完成後：
   - 開發者 A: User Story 1
   - 開發者 B: User Story 2
   - 開發者 C: User Story 3
   - 開發者 D: User Story 4
3. 各自完成並整合

---

## Task Summary

**總任務數**: 166 個任務
**依使用者故事分類**:
- Setup (Phase 1): 38 個任務
- Foundational (Phase 2): 39 個任務（T039-T077）
- User Story 1 (P1): 21 個任務（T078-T098）註冊與登入 【含 MVP】
- User Story 2 (P2): 16 個任務（T099-T114）權杖更新
- User Story 3 (P2): 15 個任務（T115-T129）個人資訊查詢
- User Story 4 (P3): 20 個任務（T130-T149）資訊更新與密碼變更
- Polish (Phase 7): 17 個任務（T150-T166）

**平行機會**: 
- Phase 1 有 31 個任務可平行開發
- Phase 2 有 22 個任務可平行開發
- 每個使用者故事內的測試任務可平行撰寫
- 每個使用者故事內的 DTOs 與例外可平行執行
- Foundational 完成後，4 個使用者故事可由不同開發者平行開發

**MVP 範圍建議**: 
- Phase 1 (Setup) + Phase 2 (Foundational) + Phase 3 (User Story 1)
- 共 98 個任務
- 提供完整的註冊與登入功能
- 可獨立部署並展示價值

**測試覆蓋率目標**: >80% (符合 Constitution 要求)

---

## Notes

- [P] 任務 = 不會檔案，無依賴問題，可平行執行
- [Story] 標籤將任務映射到特定使用者故事以便追蹤
- 每個使用者故事應該可獨立完成並測試
- TDD 強制流程：先寫測試（紅燈）→ 實作（綠燈）→ 重構
- 在實作前驗證測試失敗
- 每個任務邏輯群組後驗收
- 檢查點用來獨立驗證進度
- 風險：模糊任務、相同檔案衝突、破壞獨立性、跨故事相依性
