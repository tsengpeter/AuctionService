# Tasks: Member Service

**Input**: Design documents from `/specs/001-member-service/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/openapi.yaml

**Tests**: TDD approach required (Constitution principle - >80% test coverage target)

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

Based on Clean Architecture from plan.md:
- `src/MemberService/MemberService.Domain/` - Core entities and business logic
- `src/MemberService/MemberService.Application/` - Use cases and DTOs
- `src/MemberService/MemberService.Infrastructure/` - EF Core, security, external dependencies
- `src/MemberService/MemberService.API/` - Controllers and middleware
- `tests/MemberService.Domain.Tests/` - Domain unit tests
- `tests/MemberService.Application.Tests/` - Application unit tests
- `tests/MemberService.Infrastructure.Tests/` - Infrastructure unit tests
- `tests/MemberService.IntegrationTests/` - Integration tests with Testcontainers

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic Clean Architecture structure

- [ ] T001 Create solution file MemberService.sln at repository root
- [ ] T002 Create Domain project: src/MemberService/MemberService.Domain/MemberService.Domain.csproj
- [ ] T003 [P] Create Application project: src/MemberService/MemberService.Application/MemberService.Application.csproj
- [ ] T004 [P] Create Infrastructure project: src/MemberService/MemberService.Infrastructure/MemberService.Infrastructure.csproj
- [ ] T005 [P] Create API project: src/MemberService/MemberService.API/MemberService.API.csproj (ASP.NET Core 9 Web API)
- [ ] T006 [P] Create Domain.Tests project: tests/MemberService.Domain.Tests/MemberService.Domain.Tests.csproj
- [ ] T007 [P] Create Application.Tests project: tests/MemberService.Application.Tests/MemberService.Application.Tests.csproj
- [ ] T008 [P] Create Infrastructure.Tests project: tests/MemberService.Infrastructure.Tests/MemberService.Infrastructure.Tests.csproj
- [ ] T009 [P] Create IntegrationTests project: tests/MemberService.IntegrationTests/MemberService.IntegrationTests.csproj
- [ ] T010 Configure project references (Domain → Application → Infrastructure → API)
- [ ] T011 [P] Install NuGet packages: IdGen 3.x in Domain project
- [ ] T012 [P] Install NuGet packages: BCrypt.Net-Next 4.0.3 in Infrastructure project
- [ ] T013 [P] Install NuGet packages: System.IdentityModel.Tokens.Jwt 7.0.3 in Infrastructure project
- [ ] T014 [P] Install NuGet packages: Npgsql.EntityFrameworkCore.PostgreSQL 9.0 in Infrastructure project
- [ ] T015 [P] Install NuGet packages: FluentValidation.AspNetCore 11.3.0 in Application project
- [ ] T016 [P] Install NuGet packages: Serilog.AspNetCore 8.0 in API project
- [ ] T017 [P] Install NuGet packages: xUnit 2.6, Moq 4.20, FluentAssertions 6.12 in all test projects
- [ ] T018 [P] Install NuGet packages: Testcontainers.PostgreSql 3.6 in IntegrationTests project
- [ ] T019 Create .editorconfig with C# coding standards at repository root
- [ ] T020 Create appsettings.json with configuration structure in MemberService.API/
- [ ] T021 Create appsettings.Development.json in MemberService.API/
- [ ] T022 Create Dockerfile for containerization in MemberService.API/

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Domain Layer Foundation

- [ ] T023 Create DomainException base class in src/MemberService/MemberService.Domain/Exceptions/DomainException.cs
- [ ] T024 [P] Create EmailAlreadyExistsException in src/MemberService/MemberService.Domain/Exceptions/EmailAlreadyExistsException.cs
- [ ] T025 [P] Create InvalidCredentialsException in src/MemberService/MemberService.Domain/Exceptions/InvalidCredentialsException.cs
- [ ] T026 [P] Create InvalidOldPasswordException in src/MemberService/MemberService.Domain/Exceptions/InvalidOldPasswordException.cs
- [ ] T027 [P] Create UserNotFoundException in src/MemberService/MemberService.Domain/Exceptions/UserNotFoundException.cs

### Value Objects (Domain)

- [ ] T028 Write tests for Email value object in tests/MemberService.Domain.Tests/ValueObjects/EmailTests.cs
- [ ] T029 Implement Email value object in src/MemberService/MemberService.Domain/ValueObjects/Email.cs (max 255, validation, lowercase)
- [ ] T030 Write tests for Password value object in tests/MemberService.Domain.Tests/ValueObjects/PasswordTests.cs
- [ ] T031 Implement Password value object in src/MemberService/MemberService.Domain/ValueObjects/Password.cs (8-128 chars, validation)
- [ ] T032 Write tests for Username value object in tests/MemberService.Domain.Tests/ValueObjects/UsernameTests.cs
- [ ] T033 Implement Username value object in src/MemberService/MemberService.Domain/ValueObjects/Username.cs (3-50 chars, letters+spaces only, regex validation)

### Core Entities (Domain)

- [ ] T034 Write tests for User entity in tests/MemberService.Domain.Tests/Entities/UserTests.cs
- [ ] T035 Implement User entity in src/MemberService/MemberService.Domain/Entities/User.cs (Id:long, Email, PasswordHash, Username, CreatedAt, UpdatedAt, RefreshTokens navigation)
- [ ] T036 Write tests for RefreshToken entity in tests/MemberService.Domain.Tests/Entities/RefreshTokenTests.cs
- [ ] T037 Implement RefreshToken entity in src/MemberService/MemberService.Domain/Entities/RefreshToken.cs (Id:Guid, Token, UserId:long FK, ExpiresAt, IsRevoked, CreatedAt, IsExpired computed, IsValid computed)

### Repository Interfaces (Domain)

- [ ] T038 Create IUserRepository interface in src/MemberService/MemberService.Domain/Interfaces/IUserRepository.cs (FindByEmailAsync, FindByIdAsync, AddAsync, UpdateAsync, GetByIdAsync)
- [ ] T039 [P] Create IRefreshTokenRepository interface in src/MemberService/MemberService.Domain/Interfaces/IRefreshTokenRepository.cs (FindByTokenAsync, AddAsync, RevokeAllForUserAsync, RemoveExpiredAsync)
- [ ] T040 [P] Create IPasswordHasher interface in src/MemberService/MemberService.Domain/Interfaces/IPasswordHasher.cs (HashPassword, VerifyPassword)
- [ ] T041 [P] Create ISnowflakeIdGenerator interface in src/MemberService/MemberService.Domain/Interfaces/ISnowflakeIdGenerator.cs (GenerateId)

### Infrastructure - Security

- [ ] T042 Write tests for BCryptPasswordHasher in tests/MemberService.Infrastructure.Tests/Security/BCryptPasswordHasherTests.cs
- [ ] T043 Implement BCryptPasswordHasher in src/MemberService/MemberService.Infrastructure/Security/BCryptPasswordHasher.cs (implements IPasswordHasher, bcrypt + snowflakeId, work factor 12)
- [ ] T044 Write tests for SnowflakeIdGenerator in tests/MemberService.Infrastructure.Tests/IdGeneration/SnowflakeIdGeneratorTests.cs
- [ ] T045 Implement SnowflakeIdGenerator in src/MemberService/MemberService.Infrastructure/IdGeneration/SnowflakeIdGenerator.cs (using IdGen package, WorkerId + DatacenterId config)
- [ ] T046 Write tests for JwtTokenGenerator in tests/MemberService.Infrastructure.Tests/Security/JwtTokenGeneratorTests.cs
- [ ] T047 Implement JwtTokenGenerator in src/MemberService/MemberService.Infrastructure/Security/JwtTokenGenerator.cs (HS256, 15min expiry, claims: UserId+Email)
- [ ] T048 Write tests for RefreshTokenGenerator in tests/MemberService.Infrastructure.Tests/Security/RefreshTokenGeneratorTests.cs
- [ ] T049 Implement RefreshTokenGenerator in src/MemberService/MemberService.Infrastructure/Security/RefreshTokenGenerator.cs (256-bit random Base64, 7 day expiry)

### Infrastructure - Persistence

- [ ] T050 Create MemberDbContext in src/MemberService/MemberService.Infrastructure/Persistence/MemberDbContext.cs (DbSet<User>, DbSet<RefreshToken>, SaveChangesAsync override for UpdatedAt)
- [ ] T051 Create UserConfiguration (EF Core) in src/MemberService/MemberService.Infrastructure/Persistence/Configurations/UserConfiguration.cs (HasKey, HasIndex Email unique, MaxLength 255, relationships)
- [ ] T052 Create RefreshTokenConfiguration (EF Core) in src/MemberService/MemberService.Infrastructure/Persistence/Configurations/RefreshTokenConfiguration.cs (HasKey, HasIndex Token unique, composite index UserId+ExpiresAt, FK cascade)
- [ ] T053 Write tests for UserRepository in tests/MemberService.Infrastructure.Tests/Persistence/UserRepositoryTests.cs (use in-memory DB or Testcontainers)
- [ ] T054 Implement UserRepository in src/MemberService/MemberService.Infrastructure/Persistence/Repositories/UserRepository.cs (implements IUserRepository)
- [ ] T055 Write tests for RefreshTokenRepository in tests/MemberService.Infrastructure.Tests/Persistence/RefreshTokenRepositoryTests.cs
- [ ] T056 Implement RefreshTokenRepository in src/MemberService/MemberService.Infrastructure/Persistence/Repositories/RefreshTokenRepository.cs (implements IRefreshTokenRepository)
- [ ] T057 Create InitialCreate migration using EF Core: dotnet ef migrations add InitialCreate --startup-project src/MemberService/MemberService.API --project src/MemberService/MemberService.Infrastructure

### API - Middleware & Configuration

- [ ] T058 Implement ExceptionHandlingMiddleware in src/MemberService/MemberService.API/Middlewares/ExceptionHandlingMiddleware.cs (catch DomainException, return standard error format with code/message/timestamp/path)
- [ ] T059 [P] Implement RequestLoggingMiddleware in src/MemberService/MemberService.API/Middlewares/RequestLoggingMiddleware.cs (Serilog structured logs with UserId, RequestId, duration)
- [ ] T060 Configure Program.cs in src/MemberService/MemberService.API/Program.cs (DI registration, Serilog, EF Core, FluentValidation, JWT authentication, middleware pipeline)
- [ ] T061 Create Testcontainers fixture in tests/MemberService.IntegrationTests/TestFixtures/PostgreSqlContainerFixture.cs (PostgreSQL 16 container setup for integration tests)

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - 新使用者註冊與登入 (Priority: P1) 🎯 MVP

**Goal**: 新使用者透過提供電子郵件、密碼與使用者名稱完成註冊，註冊成功後自動登入系統，取得存取權杖以使用其他服務功能。

**Independent Test**: 提供有效的註冊資訊（email, password, username）完整測試註冊流程，驗證使用者能成功建立帳號並自動登入，回傳 JWT + Refresh Token。

### DTOs for User Story 1

- [ ] T062 [P] [US1] Create RegisterRequest DTO in src/MemberService/MemberService.Application/DTOs/Auth/RegisterRequest.cs (Email, Password, Username)
- [ ] T063 [P] [US1] Create LoginRequest DTO in src/MemberService/MemberService.Application/DTOs/Auth/LoginRequest.cs (Email, Password)
- [ ] T064 [P] [US1] Create AuthResponse DTO in src/MemberService/MemberService.Application/DTOs/Auth/AuthResponse.cs (UserId, Email, Username, AccessToken, RefreshToken)

### Validators for User Story 1

- [ ] T065 Write tests for RegisterRequestValidator in tests/MemberService.Application.Tests/Validators/RegisterRequestValidatorTests.cs
- [ ] T066 [US1] Implement RegisterRequestValidator in src/MemberService/MemberService.Application/Validators/RegisterRequestValidator.cs (FluentValidation: email format, password >=8 chars, username 3-50 letters+spaces)
- [ ] T067 Write tests for LoginRequestValidator in tests/MemberService.Application.Tests/Validators/LoginRequestValidatorTests.cs
- [ ] T068 [US1] Implement LoginRequestValidator in src/MemberService/MemberService.Application/Validators/LoginRequestValidator.cs (FluentValidation: email required, password required)

### Service Interfaces for User Story 1

- [ ] T069 [US1] Create IAuthService interface in src/MemberService/MemberService.Application/Services/IAuthService.cs (RegisterAsync, LoginAsync, RefreshTokenAsync, LogoutAsync)

### Service Implementation for User Story 1

- [ ] T070 Write tests for AuthService in tests/MemberService.Application.Tests/Services/AuthServiceTests.cs (mock repositories, test register/login scenarios)
- [ ] T071 [US1] Implement AuthService.RegisterAsync in src/MemberService/MemberService.Application/Services/AuthService.cs (check email uniqueness, generate snowflake ID, hash password with bcrypt+ID, create User+RefreshToken, return JWT)
- [ ] T072 [US1] Implement AuthService.LoginAsync in src/MemberService/MemberService.Application/Services/AuthService.cs (find by email, verify password, generate new JWT+RefreshToken, return tokens)

### Controller for User Story 1

- [ ] T073 [US1] Implement AuthController.Register in src/MemberService/MemberService.API/Controllers/AuthController.cs (POST /api/auth/register, validate, call AuthService.RegisterAsync, return 201 with AuthResponse)
- [ ] T074 [US1] Implement AuthController.Login in src/MemberService/MemberService.API/Controllers/AuthController.cs (POST /api/auth/login, validate, call AuthService.LoginAsync, return 200 with AuthResponse)

### Integration Tests for User Story 1

- [ ] T075 [US1] Write integration test for register endpoint in tests/MemberService.IntegrationTests/API/AuthControllerTests.cs (use Testcontainers, test successful register, duplicate email, validation errors)
- [ ] T076 [US1] Write integration test for login endpoint in tests/MemberService.IntegrationTests/API/AuthControllerTests.cs (test successful login, invalid credentials, wrong password)

**Checkpoint**: At this point, User Story 1 should be fully functional - users can register and login independently

---

## Phase 4: User Story 2 - 權杖更新 (Priority: P2)

**Goal**: 使用者的 JWT 存取權杖過期後，可使用 Refresh Token 換取新的 JWT，無需重新輸入密碼，提供流暢的使用體驗。

**Independent Test**: 透過取得有效的 Refresh Token，模擬 JWT 過期情境，驗證系統能成功換發新的 JWT，確保使用者在 Refresh Token 有效期限（7 天）內無需重新登入。

### DTOs for User Story 2

- [ ] T077 [P] [US2] Create RefreshTokenRequest DTO in src/MemberService/MemberService.Application/DTOs/Auth/RefreshTokenRequest.cs (RefreshToken string)
- [ ] T078 [P] [US2] Create RefreshTokenResponse DTO in src/MemberService/MemberService.Application/DTOs/Auth/RefreshTokenResponse.cs (AccessToken string)
- [ ] T079 [P] [US2] Create LogoutRequest DTO in src/MemberService/MemberService.Application/DTOs/Auth/LogoutRequest.cs (RefreshToken string)

### Service Implementation for User Story 2

- [ ] T080 Write tests for AuthService.RefreshTokenAsync in tests/MemberService.Application.Tests/Services/AuthServiceTests.cs (mock repositories, test valid token, expired token, revoked token)
- [ ] T081 [US2] Implement AuthService.RefreshTokenAsync in src/MemberService/MemberService.Application/Services/AuthService.cs (validate token not expired/revoked, generate new JWT, return new access token)
- [ ] T082 Write tests for AuthService.LogoutAsync in tests/MemberService.Application.Tests/Services/AuthServiceTests.cs
- [ ] T083 [US2] Implement AuthService.LogoutAsync in src/MemberService/MemberService.Application/Services/AuthService.cs (find refresh token, mark as revoked, save)

### Controller for User Story 2

- [ ] T084 [US2] Implement AuthController.RefreshToken in src/MemberService/MemberService.API/Controllers/AuthController.cs (POST /api/auth/refresh-token, call AuthService.RefreshTokenAsync, return 200 with new JWT)
- [ ] T085 [US2] Implement AuthController.Logout in src/MemberService/MemberService.API/Controllers/AuthController.cs (POST /api/auth/logout, [Authorize], call AuthService.LogoutAsync, return 204)

### Integration Tests for User Story 2

- [ ] T086 [US2] Write integration test for refresh-token endpoint in tests/MemberService.IntegrationTests/API/AuthControllerTests.cs (test valid token refresh, expired token, revoked token)
- [ ] T087 [US2] Write integration test for logout endpoint in tests/MemberService.IntegrationTests/API/AuthControllerTests.cs (test successful logout, token revocation)

**Checkpoint**: At this point, User Stories 1 AND 2 should both work - users can register, login, refresh tokens, and logout

---

## Phase 5: User Story 3 - 個人資料查詢 (Priority: P2)

**Goal**: 使用者可查詢自己的完整個人資料（包含電子郵件、使用者名稱等），也可查詢其他使用者的公開資料（僅使用者名稱），用於顯示商品賣家資訊等場景。

**Independent Test**: 透過已登入的使用者查詢自己的資料，並查詢其他使用者的公開資料，驗證系統正確區分私密資訊與公開資訊。

### DTOs for User Story 3

- [ ] T088 [P] [US3] Create UserProfileResponse DTO in src/MemberService/MemberService.Application/DTOs/Users/UserProfileResponse.cs (UserId, Email, Username, CreatedAt, UpdatedAt)
- [ ] T089 [P] [US3] Create PublicUserProfileResponse DTO in src/MemberService/MemberService.Application/DTOs/Users/PublicUserProfileResponse.cs (UserId, Username, CreatedAt only)

### Service Interface & Implementation for User Story 3

- [ ] T090 [US3] Create IUserService interface in src/MemberService/MemberService.Application/Services/IUserService.cs (GetMyProfileAsync, GetUserProfileAsync, UpdateProfileAsync, ChangePasswordAsync)
- [ ] T091 Write tests for UserService in tests/MemberService.Application.Tests/Services/UserServiceTests.cs (mock repository, test get my profile, get other user profile)
- [ ] T092 [US3] Implement UserService.GetMyProfileAsync in src/MemberService/MemberService.Application/Services/UserService.cs (find user by ID from JWT claims, return full profile)
- [ ] T093 [US3] Implement UserService.GetUserProfileAsync in src/MemberService/MemberService.Application/Services/UserService.cs (find user by ID, return public profile only, throw UserNotFoundException if not found)

### Controller for User Story 3

- [ ] T094 [US3] Create UsersController in src/MemberService/MemberService.API/Controllers/UsersController.cs
- [ ] T095 [US3] Implement UsersController.GetMyProfile in src/MemberService/MemberService.API/Controllers/UsersController.cs (GET /api/users/me, [Authorize], call UserService.GetMyProfileAsync, return 200 with UserProfileResponse)
- [ ] T096 [US3] Implement UsersController.GetUserProfile in src/MemberService/MemberService.API/Controllers/UsersController.cs (GET /api/users/{id}, [Authorize], call UserService.GetUserProfileAsync, return 200 with PublicUserProfileResponse or 404)

### Integration Tests for User Story 3

- [ ] T097 [US3] Write integration test for GET /api/users/me in tests/MemberService.IntegrationTests/API/UsersControllerTests.cs (test authenticated user gets full profile, unauthorized returns 401)
- [ ] T098 [US3] Write integration test for GET /api/users/{id} in tests/MemberService.IntegrationTests/API/UsersControllerTests.cs (test get public profile, user not found returns 404)

**Checkpoint**: User Stories 1, 2, AND 3 should all work - users can register, login, manage tokens, and query profiles

---

## Phase 6: User Story 4 - 個人資料更新與密碼變更 (Priority: P3)

**Goal**: 使用者可更新自己的使用者名稱與電子郵件，也可變更密碼以維護帳號安全。密碼變更時撤銷所有 Refresh Token。

**Independent Test**: 透過已登入的使用者更新資料欄位，驗證系統正確更新並驗證資料唯一性；透過密碼變更流程驗證舊密碼驗證與新密碼設定。

### DTOs for User Story 4

- [ ] T099 [P] [US4] Create UpdateProfileRequest DTO in src/MemberService/MemberService.Application/DTOs/Users/UpdateProfileRequest.cs (Username optional, Email optional)
- [ ] T100 [P] [US4] Create ChangePasswordRequest DTO in src/MemberService/MemberService.Application/DTOs/Users/ChangePasswordRequest.cs (OldPassword, NewPassword)

### Validators for User Story 4

- [ ] T101 Write tests for UpdateProfileRequestValidator in tests/MemberService.Application.Tests/Validators/UpdateProfileRequestValidatorTests.cs
- [ ] T102 [US4] Implement UpdateProfileRequestValidator in src/MemberService/MemberService.Application/Validators/UpdateProfileRequestValidator.cs (FluentValidation: email format if provided, username 3-50 letters+spaces if provided)
- [ ] T103 Write tests for ChangePasswordRequestValidator in tests/MemberService.Application.Tests/Validators/ChangePasswordRequestValidatorTests.cs
- [ ] T104 [US4] Implement ChangePasswordRequestValidator in src/MemberService/MemberService.Application/Validators/ChangePasswordRequestValidator.cs (FluentValidation: old password required, new password >=8 chars)

### Service Implementation for User Story 4

- [ ] T105 Write tests for UserService.UpdateProfileAsync in tests/MemberService.Application.Tests/Services/UserServiceTests.cs (test update username, update email, duplicate email)
- [ ] T106 [US4] Implement UserService.UpdateProfileAsync in src/MemberService/MemberService.Application/Services/UserService.cs (find user, check email uniqueness if changed, update fields, save, return updated profile)
- [ ] T107 Write tests for UserService.ChangePasswordAsync in tests/MemberService.Application.Tests/Services/UserServiceTests.cs (test successful change, wrong old password, validation)
- [ ] T108 [US4] Implement UserService.ChangePasswordAsync in src/MemberService/MemberService.Application/Services/UserService.cs (verify old password, hash new password with bcrypt+snowflakeId, update, revoke ALL refresh tokens, save)

### Controller for User Story 4

- [ ] T109 [US4] Implement UsersController.UpdateProfile in src/MemberService/MemberService.API/Controllers/UsersController.cs (PUT /api/users/me, [Authorize], validate, call UserService.UpdateProfileAsync, return 200 with updated UserProfileResponse)
- [ ] T110 [US4] Implement UsersController.ChangePassword in src/MemberService/MemberService.API/Controllers/UsersController.cs (PUT /api/users/me/password, [Authorize], validate, call UserService.ChangePasswordAsync, return 204)

### Integration Tests for User Story 4

- [ ] T111 [US4] Write integration test for PUT /api/users/me in tests/MemberService.IntegrationTests/API/UsersControllerTests.cs (test update username, update email, duplicate email error, validation errors)
- [ ] T112 [US4] Write integration test for PUT /api/users/me/password in tests/MemberService.IntegrationTests/API/UsersControllerTests.cs (test successful password change, wrong old password, verify all refresh tokens revoked)

**Checkpoint**: All user stories should now be independently functional - complete member service with registration, authentication, profile management

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T113 [P] Add XML documentation comments to all public APIs in MemberService.API/Controllers/
- [ ] T114 [P] Add Swagger/OpenAPI generation configuration in Program.cs with JWT bearer authentication support
- [ ] T115 Performance optimization: Add response caching for public user profiles
- [ ] T116 Performance optimization: Configure connection pooling for PostgreSQL in appsettings.json
- [ ] T117 Security hardening: Add rate limiting middleware for authentication endpoints
- [ ] T118 Security hardening: Configure CORS policy in Program.cs
- [ ] T119 [P] Add health check endpoint in MemberService.API/Controllers/HealthController.cs (GET /health)
- [ ] T120 [P] Add readiness check endpoint in MemberService.API/Controllers/HealthController.cs (GET /ready, check DB connection)
- [ ] T121 Run database migrations on startup: dotnet ef database update --startup-project src/MemberService/MemberService.API
- [ ] T122 Validate quickstart.md: Follow setup steps in specs/001-member-service/quickstart.md and verify all endpoints work
- [ ] T123 Generate test coverage report: dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
- [ ] T124 Verify >80% test coverage target achieved (Constitution requirement)
- [ ] T125 Code cleanup: Run dotnet format and fix any styling issues
- [ ] T126 Update README.md with Member Service API documentation and getting started guide

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-6)**: All depend on Foundational phase completion
  - User stories can then proceed in parallel (if staffed)
  - Or sequentially in priority order (US1 → US2 → US3 → US4)
- **Polish (Phase 7)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories ✅ MVP
- **User Story 2 (P2)**: Can start after Foundational (Phase 2) - Requires JWT from US1 but independently testable
- **User Story 3 (P2)**: Can start after Foundational (Phase 2) - Requires authentication from US1 but independently testable
- **User Story 4 (P3)**: Can start after Foundational (Phase 2) - Requires authentication from US1 but independently testable

### Within Each User Story (TDD Workflow)

1. Tests MUST be written FIRST and FAIL before implementation
2. DTOs and validators before services
3. Service interfaces before implementations
4. Services before controllers
5. Unit tests before integration tests
6. Story complete and independently testable before moving to next priority

### Parallel Opportunities

#### Setup Phase (Phase 1)
- T003-T009: All test project creation can run in parallel
- T011-T018: All NuGet package installations can run in parallel

#### Foundational Phase (Phase 2)
- T024-T027: All exception classes can be created in parallel
- T028-T033: Value object tests and implementations can be done in pairs (test → implement)
- T038-T041: All repository/service interfaces can be created in parallel
- T042-T049: Infrastructure tests and implementations can be done in parallel
- T051-T052: EF Core configurations can be created in parallel
- T058-T059: Middleware implementations can be done in parallel

#### User Story 1 (Phase 3)
- T062-T064: All DTOs can be created in parallel
- T065-T068: Validator tests and implementations in parallel

#### User Story 2 (Phase 4)
- T077-T079: All DTOs can be created in parallel
- T080-T083: Service tests and implementations in parallel

#### User Story 3 (Phase 5)
- T088-T089: DTOs can be created in parallel
- T092-T093: Service implementations in parallel

#### User Story 4 (Phase 6)
- T099-T100: DTOs can be created in parallel
- T101-T104: Validator tests and implementations in parallel

#### Polish Phase (Phase 7)
- T113-T114: Documentation tasks can run in parallel
- T115-T116: Performance optimization tasks in parallel
- T117-T118: Security hardening tasks in parallel
- T119-T120: Health check endpoints in parallel

### Critical Path

The minimum viable product (MVP) requires:
1. Phase 1: Setup (T001-T022)
2. Phase 2: Foundational (T023-T061)
3. Phase 3: User Story 1 (T062-T076)

This delivers registration and login functionality - the core value proposition.

---

## Parallel Example: User Story 1

```bash
# After Foundational phase completes, these can run simultaneously:

# Developer A: DTOs
T062, T063, T064

# Developer B: Validators (wait for DTOs)
T065-T066 (RegisterRequestValidator test + implementation)
T067-T068 (LoginRequestValidator test + implementation)

# Developer C: Service (wait for validators)
T070 (AuthService tests)
T071-T072 (RegisterAsync + LoginAsync implementation)

# Developer D: Controller (wait for service)
T073-T074 (AuthController Register + Login)

# Developer E: Integration tests (wait for controller)
T075-T076 (Integration tests)
```

---

## Implementation Strategy

### MVP First (Phase 1-3)
- Focus: User Story 1 only
- Deliverable: Working registration and login system
- Timeline: Minimal viable product for initial deployment
- Value: Core authentication service operational

### Incremental Delivery (Phase 4-6)
- Add User Story 2: Token refresh for better UX
- Add User Story 3: Profile queries for marketplace integration
- Add User Story 4: Profile management for user autonomy
- Each story independently deployable

### Polish (Phase 7)
- Performance, security, documentation
- Production-ready hardening
- Operational excellence

---

## Task Summary

- **Total Tasks**: 126
- **Setup Phase**: 22 tasks
- **Foundational Phase**: 39 tasks (BLOCKING)
- **User Story 1 (P1)**: 15 tasks (MVP) 🎯
- **User Story 2 (P2)**: 11 tasks
- **User Story 3 (P2)**: 11 tasks
- **User Story 4 (P3)**: 14 tasks
- **Polish Phase**: 14 tasks

**Parallel Opportunities**: 45+ tasks marked [P] can run in parallel

**Independent Test Criteria**: Each user story phase includes specific acceptance tests and can be validated independently

**Suggested MVP Scope**: Phase 1 (Setup) + Phase 2 (Foundational) + Phase 3 (User Story 1) = 76 tasks for core registration & login

**TDD Compliance**: All implementation tasks preceded by corresponding test tasks (>80% coverage target per Constitution)
