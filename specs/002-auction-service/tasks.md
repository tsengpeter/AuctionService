# Tasks: Auction Service

**Branch**: `002-auction-service`  
**Input**: Design documents from `/specs/002-auction-service/`  
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/openapi.yaml

**Tests**: TDD workflow enforced - tests MUST be written first (Red-Green-Refactor)

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

---

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3, US4)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [ ] T001 Create solution file `AuctionService.sln` at repository root
- [ ] T002 Create Domain project `src/AuctionService.Domain/AuctionService.Domain.csproj` with .NET 9 target
- [ ] T003 Create Application project `src/AuctionService.Application/AuctionService.Application.csproj` with Domain reference
- [ ] T004 Create Infrastructure project `src/AuctionService.Infrastructure/AuctionService.Infrastructure.csproj` with Application reference
- [ ] T005 Create API project `src/AuctionService.API/AuctionService.API.csproj` with Application and Infrastructure references
- [ ] T006 [P] Create Domain test project `tests/AuctionService.Domain.Tests/AuctionService.Domain.Tests.csproj`
- [ ] T007 [P] Create Application test project `tests/AuctionService.Application.Tests/AuctionService.Application.Tests.csproj`
- [ ] T008 [P] Create Infrastructure test project `tests/AuctionService.Infrastructure.Tests/AuctionService.Infrastructure.Tests.csproj`
- [ ] T009 [P] Create Integration test project `tests/AuctionService.IntegrationTests/AuctionService.IntegrationTests.csproj`
- [ ] T010 Install NuGet packages in Domain: (none - no external dependencies per Clean Architecture)
- [ ] T011 [P] Install NuGet packages in Application: FluentValidation 11.x
- [ ] T012 [P] Install NuGet packages in Infrastructure: EF Core 9.0, Npgsql 9.0, IdGen 3.x, StackExchange.Redis 2.x, AWSSDK.S3 or Azure.Storage.Blobs or Minio 6.x, Polly 8.x
- [ ] T013 [P] Install NuGet packages in API: Serilog 4.x, Serilog.AspNetCore, Serilog.Sinks.Console, Serilog.Sinks.File
- [ ] T014 [P] Install NuGet packages in all test projects: xUnit 2.x, Moq 4.x, FluentAssertions 6.x, Testcontainers.PostgreSQL 3.x, Testcontainers.Redis 3.x
- [ ] T015 Create `.editorconfig` with C# 12 coding standards
- [ ] T016 Create `docker-compose.yml` for local PostgreSQL 16 + Redis 7 + MinIO
- [ ] T017 Create `Dockerfile` for production deployment
- [ ] T018 Create `.github/workflows/ci.yml` for build + test + coverage
- [ ] T019 Create `.github/workflows/cd.yml` for deployment to Azure/AWS
- [ ] T020 Create `appsettings.json` with base configuration
- [ ] T021 Create `appsettings.Development.json` with Docker connection strings
- [ ] T022 Create `appsettings.Production.json` template with Azure/AWS placeholders

**Checkpoint**: Project structure ready for development

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Domain Foundation

- [ ] T023 [P] Create `AuctionStatus` enum in `src/AuctionService.Domain/ValueObjects/AuctionStatus.cs` (Pending, Active, Ended)
- [ ] T024 [P] Create `Money` value object in `src/AuctionService.Domain/ValueObjects/Money.cs` with validation (>0, max 10M)
- [ ] T025 [P] Create `ImageUrl` value object in `src/AuctionService.Domain/ValueObjects/ImageUrl.cs` with URL validation
- [ ] T026 [P] Create `AuctionNotFoundException` in `src/AuctionService.Domain/Exceptions/AuctionNotFoundException.cs`
- [ ] T027 [P] Create `AuctionConcurrentUpdateException` in `src/AuctionService.Domain/Exceptions/AuctionConcurrentUpdateException.cs`
- [ ] T028 [P] Create `CategoryNotFoundException` in `src/AuctionService.Domain/Exceptions/CategoryNotFoundException.cs`
- [ ] T029 [P] Create `InvalidAuctionStateException` in `src/AuctionService.Domain/Exceptions/InvalidAuctionStateException.cs`
- [ ] T030 [P] Create `FollowAlreadyExistsException` in `src/AuctionService.Domain/Exceptions/FollowAlreadyExistsException.cs`
- [ ] T031 [P] Create `CannotFollowOwnAuctionException` in `src/AuctionService.Domain/Exceptions/CannotFollowOwnAuctionException.cs`

### Entity Foundation (Shared Entities)

- [ ] T032 Create `Category` entity in `src/AuctionService.Domain/Entities/Category.cs` (Id, Name, DisplayOrder, IsEnabled)
- [ ] T033 Create `ResponseCode` entity in `src/AuctionService.Domain/Entities/ResponseCode.cs` (Code, Name, MessageZhTw, MessageEn, Category, Severity)
- [ ] T034 Create `Auction` entity in `src/AuctionService.Domain/Entities/Auction.cs` with Snowflake ID, RowVersion, SearchVector (tsvector), ImageUrls (List<string>), computed Status property
- [ ] T035 Create `Follow` entity in `src/AuctionService.Domain/Entities/Follow.cs` (Id, UserId, AuctionId, CreatedAt)

### Repository Interfaces

- [ ] T036 [P] Create `IAuctionRepository` in `src/AuctionService.Domain/Repositories/IAuctionRepository.cs` (GetByIdAsync, GetAllAsync, GetByCreatorIdAsync, Add, Update, Delete, HasBidsAsync)
- [ ] T037 [P] Create `ICategoryRepository` in `src/AuctionService.Domain/Repositories/ICategoryRepository.cs` (GetByIdAsync, GetAllAsync, ExistsAsync)
- [ ] T038 [P] Create `IFollowRepository` in `src/AuctionService.Domain/Repositories/IFollowRepository.cs` (GetByUserAndAuctionAsync, GetByUserIdAsync, Add, Delete, IsFollowingAsync)
- [ ] T039 [P] Create `IResponseCodeRepository` in `src/AuctionService.Domain/Repositories/IResponseCodeRepository.cs` (GetByCodeAsync, GetAllAsync)

### Application Foundation

- [ ] T040 [P] Create `StandardResponse<T>` wrapper in `src/AuctionService.Application/DTOs/Responses/StandardResponse.cs`
- [ ] T041 [P] Create `ResponseMetadata` in `src/AuctionService.Application/DTOs/Responses/ResponseMetadata.cs` (StatusCode, StatusName, Message)
- [ ] T042 [P] Create `ISnowflakeIdGenerator` interface in `src/AuctionService.Application/Interfaces/ISnowflakeIdGenerator.cs`
- [ ] T043 [P] Create `ICacheService` interface in `src/AuctionService.Application/Interfaces/ICacheService.cs` (GetAsync, SetAsync, RemoveAsync)

### Infrastructure Foundation

- [ ] T044 Create `AuctionDbContext` in `src/AuctionService.Infrastructure/Persistence/AuctionDbContext.cs` with DbSet properties and SaveChangesAsync override for UpdatedAt
- [ ] T045 [P] Create `AuctionConfiguration` in `src/AuctionService.Infrastructure/Persistence/Configurations/AuctionConfiguration.cs` (Fluent API: indexes, RowVersion, JSON ImageUrls, tsvector SearchVector)
- [ ] T046 [P] Create `CategoryConfiguration` in `src/AuctionService.Infrastructure/Persistence/Configurations/CategoryConfiguration.cs`
- [ ] T047 [P] Create `FollowConfiguration` in `src/AuctionService.Infrastructure/Persistence/Configurations/FollowConfiguration.cs` (unique constraint on UserId+AuctionId)
- [ ] T048 [P] Create `ResponseCodeConfiguration` in `src/AuctionService.Infrastructure/Persistence/Configurations/ResponseCodeConfiguration.cs`
- [ ] T049 Create Initial Migration `InitialCreate` with `dotnet ef migrations add InitialCreate` (includes Categories/ResponseCodes seed data, SearchVector trigger function)
- [ ] T050 Create `SnowflakeIdGenerator` implementation in `src/AuctionService.Infrastructure/IdGeneration/SnowflakeIdGenerator.cs` using IdGen package
- [ ] T051 Create `RedisCacheService` implementation in `src/AuctionService.Infrastructure/Caching/RedisCacheService.cs` using IDistributedCache
- [ ] T052 [P] Create `AuctionRepository` in `src/AuctionService.Infrastructure/Persistence/Repositories/AuctionRepository.cs` with EF Core queries (Include Category, tsvector search, EndTime index filtering)
- [ ] T053 [P] Create `CategoryRepository` in `src/AuctionService.Infrastructure/Persistence/Repositories/CategoryRepository.cs`
- [ ] T054 [P] Create `FollowRepository` in `src/AuctionService.Infrastructure/Persistence/Repositories/FollowRepository.cs`
- [ ] T055 [P] Create `ResponseCodeRepository` in `src/AuctionService.Infrastructure/Persistence/Repositories/ResponseCodeRepository.cs`

### API Foundation

- [ ] T056 Create `Program.cs` in `src/AuctionService.API/Program.cs` with WebApplicationBuilder, DI registration, middleware pipeline
- [ ] T057 Create `ServiceCollectionExtensions` in `src/AuctionService.API/Extensions/ServiceCollectionExtensions.cs` for DI registration
- [ ] T058 Create `ExceptionHandlingMiddleware` in `src/AuctionService.API/Middleware/ExceptionHandlingMiddleware.cs` (catch domain exceptions, return StandardResponse with ResponseMetadata)
- [ ] T059 Create `CorrelationIdMiddleware` in `src/AuctionService.API/Middleware/CorrelationIdMiddleware.cs` (generate/propagate correlation ID)
- [ ] T060 Create `ResponseMetadataMiddleware` in `src/AuctionService.API/Middleware/ResponseMetadataMiddleware.cs` (wrap all responses in StandardResponse)
- [ ] T061 Create `ValidateModelStateFilter` in `src/AuctionService.API/Filters/ValidateModelStateFilter.cs` (FluentValidation integration)
- [ ] T062 Create `AuthorizationFilter` in `src/AuctionService.API/Filters/AuthorizationFilter.cs` (JWT validation, extract UserId from claims)
- [ ] T063 Create `HealthController` in `src/AuctionService.API/Controllers/HealthController.cs` with GET /health and GET /ready endpoints

### Test Foundation (Testcontainers Setup)

- [ ] T064 [P] Create `DatabaseFixture` in `tests/AuctionService.IntegrationTests/Fixtures/DatabaseFixture.cs` (Testcontainers.PostgreSQL lifecycle)
- [ ] T065 [P] Create `RedisFixture` in `tests/AuctionService.IntegrationTests/Fixtures/RedisFixture.cs` (Testcontainers.Redis lifecycle)
- [ ] T066 [P] Create `WebApplicationFactory` in `tests/AuctionService.IntegrationTests/Fixtures/WebApplicationFactory.cs` (configure test server)

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - 瀏覽與搜尋拍賣商品 (Priority: P1) 🎯 MVP

**Goal**: 使用者可瀏覽所有進行中的拍賣商品，透過分類篩選、關鍵字搜尋快速找到感興趣的商品，並查看商品詳細資訊與目前出價。

**Independent Test**: 可透過查詢商品清單、使用篩選與搜尋功能、查看商品詳細資訊來完整測試此功能，驗證使用者能成功找到並了解商品資訊。

### Tests for User Story 1 (TDD - Write First)

- [ ] T067 [P] [US1] Unit test for `AuctionStatus` computed property in `tests/AuctionService.Domain.Tests/Entities/AuctionTests.cs` (test Pending/Active/Ended calculation)
- [ ] T068 [P] [US1] Unit test for `CategoryService.GetAllAsync` in `tests/AuctionService.Application.Tests/Services/CategoryServiceTests.cs`
- [ ] T069 [P] [US1] Unit test for `AuctionService.GetAllAsync` with filters in `tests/AuctionService.Application.Tests/Services/AuctionServiceTests.cs` (mock repository)
- [ ] T070 [P] [US1] Unit test for `AuctionMapper.ToAuctionListItemResponse` in `tests/AuctionService.Application.Tests/Mappers/AuctionMapperTests.cs`
- [ ] T071 [P] [US1] Unit test for `AuctionMapper.ToAuctionDetailResponse` in `tests/AuctionService.Application.Tests/Mappers/AuctionMapperTests.cs`
- [ ] T072 [P] [US1] Integration test for `GET /api/auctions` in `tests/AuctionService.IntegrationTests/Controllers/AuctionsControllerTests.cs` (test pagination, category filter, keyword search, sorting)
- [ ] T073 [P] [US1] Integration test for `GET /api/auctions/{id}` in `tests/AuctionService.IntegrationTests/Controllers/AuctionsControllerTests.cs`
- [ ] T074 [P] [US1] Integration test for `GET /api/auctions/{id}/current-bid` in `tests/AuctionService.IntegrationTests/Controllers/AuctionsControllerTests.cs` (test cache fallback)
- [ ] T075 [P] [US1] Integration test for `GET /api/categories` in `tests/AuctionService.IntegrationTests/Controllers/CategoriesControllerTests.cs`

### Implementation for User Story 1

#### DTOs

- [ ] T076 [P] [US1] Create `AuctionQueryParameters` request DTO in `src/AuctionService.Application/DTOs/Requests/AuctionQueryParameters.cs` (PageIndex, PageSize, CategoryId, Keyword, SortBy, SortOrder)
- [ ] T077 [P] [US1] Create `AuctionListItemResponse` DTO in `src/AuctionService.Application/DTOs/Responses/AuctionListItemResponse.cs` (Id, Title, StartingPrice, CategoryName, ImageUrl, EndTime, Status)
- [ ] T078 [P] [US1] Create `AuctionDetailResponse` DTO in `src/AuctionService.Application/DTOs/Responses/AuctionDetailResponse.cs` (all Auction properties + CategoryName)
- [ ] T079 [P] [US1] Create `CurrentBidResponse` DTO in `src/AuctionService.Application/DTOs/Responses/CurrentBidResponse.cs` (AuctionId, CurrentBid object with Amount/BidderId/BidTime/DataSource)
- [ ] T080 [P] [US1] Create `CategoryResponse` DTO in `src/AuctionService.Application/DTOs/Responses/CategoryResponse.cs` (Id, Name, DisplayOrder, IsEnabled)

#### Services & Mappers

- [ ] T081 [P] [US1] Create `ICategoryService` interface in `src/AuctionService.Application/Services/ICategoryService.cs`
- [ ] T082 [US1] Create `CategoryService` implementation in `src/AuctionService.Application/Services/CategoryService.cs` (GetAllAsync, GetByIdAsync)
- [ ] T083 [P] [US1] Create `IAuctionService` interface in `src/AuctionService.Application/Services/IAuctionService.cs`
- [ ] T084 [US1] Create `AuctionService` implementation in `src/AuctionService.Application/Services/AuctionService.cs` (GetAllAsync with pagination/filter/search/sort, GetByIdAsync)
- [ ] T085 [P] [US1] Create `IBiddingServiceClient` interface in `src/AuctionService.Application/Services/IBiddingServiceClient.cs`
- [ ] T086 [US1] Create `BiddingServiceClient` implementation in `src/AuctionService.Infrastructure/ExternalServices/BiddingServiceHttpClient.cs` (Polly retry policy, 3s timeout, cache fallback with Redis)
- [ ] T087 [P] [US1] Create `CategoryMapper` in `src/AuctionService.Application/Mappers/CategoryMapper.cs` (ToCategoryResponse)
- [ ] T088 [P] [US1] Create `AuctionMapper` in `src/AuctionService.Application/Mappers/AuctionMapper.cs` (ToAuctionListItemResponse, ToAuctionDetailResponse, manual POCO mapping)

#### Controllers

- [ ] T089 [US1] Create `CategoriesController` in `src/AuctionService.API/Controllers/CategoriesController.cs` with GET /api/categories endpoint
- [ ] T090 [US1] Create `AuctionsController` in `src/AuctionService.API/Controllers/AuctionsController.cs` with GET /api/auctions endpoint (pagination, filters, search, sort)
- [ ] T091 [US1] Add GET /api/auctions/{id} endpoint to `AuctionsController` (returns AuctionDetailResponse)
- [ ] T092 [US1] Add GET /api/auctions/{id}/current-bid endpoint to `AuctionsController` (calls BiddingServiceClient with cache fallback)

**Checkpoint**: At this point, User Story 1 should be fully functional and testable independently. Users can browse, search, filter auctions and view details with current bid.

---

## Phase 4: User Story 2 - 建立與管理拍賣商品 (Priority: P1) 🎯 MVP

**Goal**: 賣家可建立新的拍賣商品，設定商品資訊與結束時間，並在符合條件時編輯或刪除自己的商品。

**Independent Test**: 可透過建立新商品、編輯商品、刪除商品來完整測試此功能，驗證賣家能成功管理自己的拍賣商品。

### Tests for User Story 2 (TDD - Write First)

- [ ] T093 [P] [US2] Unit test for `CreateAuctionRequestValidator` in `tests/AuctionService.Application.Tests/Validators/CreateAuctionRequestValidatorTests.cs` (test Title 3-200, Description 10-2000, StartingPrice >0 <10M, EndTime future +1h)
- [ ] T094 [P] [US2] Unit test for `UpdateAuctionRequestValidator` in `tests/AuctionService.Application.Tests/Validators/UpdateAuctionRequestValidatorTests.cs`
- [ ] T095 [P] [US2] Unit test for `AuctionService.CreateAsync` in `tests/AuctionService.Application.Tests/Services/AuctionServiceTests.cs` (test Snowflake ID generation, CreatorId assignment)
- [ ] T096 [P] [US2] Unit test for `AuctionService.UpdateAsync` in `tests/AuctionService.Application.Tests/Services/AuctionServiceTests.cs` (test RowVersion concurrency check)
- [ ] T097 [P] [US2] Unit test for `AuctionService.DeleteAsync` in `tests/AuctionService.Application.Tests/Services/AuctionServiceTests.cs` (test HasBidsAsync check)
- [ ] T098 [P] [US2] Unit test for `AuctionMapper.ToAuction` in `tests/AuctionService.Application.Tests/Mappers/AuctionMapperTests.cs` (CreateAuctionRequest → Auction entity)
- [ ] T099 [P] [US2] Integration test for `POST /api/auctions` in `tests/AuctionService.IntegrationTests/Controllers/AuctionsControllerTests.cs` (test authentication, validation, Snowflake ID)
- [ ] T100 [P] [US2] Integration test for `PUT /api/auctions/{id}` in `tests/AuctionService.IntegrationTests/Controllers/AuctionsControllerTests.cs` (test concurrency 409 Conflict, owner-only)
- [ ] T101 [P] [US2] Integration test for `DELETE /api/auctions/{id}` in `tests/AuctionService.IntegrationTests/Controllers/AuctionsControllerTests.cs` (test owner-only, no bids check)
- [ ] T102 [P] [US2] Integration test for `POST /api/auctions/{id}/images` in `tests/AuctionService.IntegrationTests/Controllers/AuctionsControllerTests.cs` (test file upload, max 5 images, 5MB limit)
- [ ] T103 [P] [US2] Integration test for `GET /api/auctions/users/{userId}` in `tests/AuctionService.IntegrationTests/Controllers/AuctionsControllerTests.cs`

### Implementation for User Story 2

#### DTOs & Validators

- [ ] T104 [P] [US2] Create `CreateAuctionRequest` DTO in `src/AuctionService.Application/DTOs/Requests/CreateAuctionRequest.cs` (Title, Description, StartingPrice, CategoryId, EndTime)
- [ ] T105 [P] [US2] Create `UpdateAuctionRequest` DTO in `src/AuctionService.Application/DTOs/Requests/UpdateAuctionRequest.cs` (same as Create + RowVersion)
- [ ] T106 [P] [US2] Create `CreateAuctionRequestValidator` in `src/AuctionService.Application/Validators/CreateAuctionRequestValidator.cs` (FluentValidation rules)
- [ ] T107 [P] [US2] Create `UpdateAuctionRequestValidator` in `src/AuctionService.Application/Validators/UpdateAuctionRequestValidator.cs`

#### Services

- [ ] T108 [US2] Add `CreateAsync` method to `AuctionService` in `src/AuctionService.Application/Services/AuctionService.cs` (generate Snowflake ID, assign CreatorId from JWT claims, validate CategoryId exists)
- [ ] T109 [US2] Add `UpdateAsync` method to `AuctionService` (check owner, check HasBidsAsync, handle DbUpdateConcurrencyException)
- [ ] T110 [US2] Add `DeleteAsync` method to `AuctionService` (check owner, check HasBidsAsync)
- [ ] T111 [US2] Add `GetByCreatorIdAsync` method to `AuctionService` (query user's auctions with pagination)
- [ ] T112 [P] [US2] Create `IImageStorageService` interface in `src/AuctionService.Application/Services/IImageStorageService.cs`
- [ ] T113 [US2] Create `MinIOImageStorageClient` in `src/AuctionService.Infrastructure/ExternalServices/ImageStorageClients/MinIOImageStorageClient.cs` (upload, delete, generate URL, validate file size/format)
- [ ] T114 [P] [US2] Create `S3ImageStorageClient` in `src/AuctionService.Infrastructure/ExternalServices/ImageStorageClients/S3ImageStorageClient.cs` (optional, AWS S3 implementation)
- [ ] T115 [P] [US2] Create `BlobImageStorageClient` in `src/AuctionService.Infrastructure/ExternalServices/ImageStorageClients/BlobImageStorageClient.cs` (optional, Azure Blob implementation)
- [ ] T116 [US2] Add `UploadImagesAsync` method to `AuctionService` (call IImageStorageService, update ImageUrls array, max 5 images)

#### Controllers

- [ ] T117 [US2] Add POST /api/auctions endpoint to `AuctionsController` (authenticate, validate, call CreateAsync)
- [ ] T118 [US2] Add PUT /api/auctions/{id} endpoint to `AuctionsController` (authenticate, validate owner, call UpdateAsync, handle 409 Conflict)
- [ ] T119 [US2] Add DELETE /api/auctions/{id} endpoint to `AuctionsController` (authenticate, validate owner, call DeleteAsync)
- [ ] T120 [US2] Add POST /api/auctions/{id}/images endpoint to `AuctionsController` (authenticate, multipart/form-data, validate max 5 images + 5MB + jpg/png/webp)
- [ ] T121 [US2] Add GET /api/auctions/users/{userId} endpoint to `AuctionsController` (pagination)

**Checkpoint**: At this point, User Stories 1 AND 2 should both work independently. Users can browse auctions AND sellers can create/manage auctions.

---

## Phase 5: User Story 3 - 追蹤感興趣的商品 (Priority: P2)

**Goal**: 使用者可將感興趣的商品加入追蹤清單，方便隨時查看這些商品的最新狀態，也可取消不再關注的商品。

**Independent Test**: 可透過追蹤商品、取消追蹤、查看追蹤清單來完整測試此功能，驗證使用者能成功管理追蹤清單。

### Tests for User Story 3 (TDD - Write First)

- [ ] T122 [P] [US3] Unit test for `FollowRequestValidator` in `tests/AuctionService.Application.Tests/Validators/FollowRequestValidatorTests.cs`
- [ ] T123 [P] [US3] Unit test for `FollowService.FollowAuctionAsync` in `tests/AuctionService.Application.Tests/Services/FollowServiceTests.cs` (test duplicate check, own auction check)
- [ ] T124 [P] [US3] Unit test for `FollowService.UnfollowAuctionAsync` in `tests/AuctionService.Application.Tests/Services/FollowServiceTests.cs`
- [ ] T125 [P] [US3] Unit test for `FollowService.GetUserFollowsAsync` in `tests/AuctionService.Application.Tests/Services/FollowServiceTests.cs`
- [ ] T126 [P] [US3] Unit test for `FollowMapper.ToFollowItemResponse` in `tests/AuctionService.Application.Tests/Mappers/FollowMapperTests.cs`
- [ ] T127 [P] [US3] Integration test for `GET /api/follows` in `tests/AuctionService.IntegrationTests/Controllers/FollowsControllerTests.cs` (test authentication, pagination)
- [ ] T128 [P] [US3] Integration test for `POST /api/follows` in `tests/AuctionService.IntegrationTests/Controllers/FollowsControllerTests.cs` (test duplicate, own auction rejection)
- [ ] T129 [P] [US3] Integration test for `DELETE /api/follows/{auctionId}` in `tests/AuctionService.IntegrationTests/Controllers/FollowsControllerTests.cs`

### Implementation for User Story 3

#### DTOs & Validators

- [ ] T130 [P] [US3] Create `FollowRequest` DTO in `src/AuctionService.Application/DTOs/Requests/FollowRequest.cs` (AuctionId)
- [ ] T131 [P] [US3] Create `FollowItemResponse` DTO in `src/AuctionService.Application/DTOs/Responses/FollowItemResponse.cs` (Id, UserId, AuctionId, Auction nested object with detail, CreatedAt)
- [ ] T132 [P] [US3] Create `FollowRequestValidator` in `src/AuctionService.Application/Validators/FollowRequestValidator.cs`

#### Services & Mappers

- [ ] T133 [P] [US3] Create `IFollowService` interface in `src/AuctionService.Application/Services/IFollowService.cs`
- [ ] T134 [US3] Create `FollowService` implementation in `src/AuctionService.Application/Services/FollowService.cs` (FollowAuctionAsync, UnfollowAuctionAsync, GetUserFollowsAsync with pagination)
- [ ] T135 [US3] Add business rule to `FollowService.FollowAuctionAsync`: Check auction exists, check UserId != CreatorId (CannotFollowOwnAuctionException), check duplicate (FollowAlreadyExistsException)
- [ ] T136 [P] [US3] Create `FollowMapper` in `src/AuctionService.Application/Mappers/FollowMapper.cs` (ToFollowItemResponse with nested Auction detail)

#### Controllers

- [ ] T137 [US3] Create `FollowsController` in `src/AuctionService.API/Controllers/FollowsController.cs` with GET /api/follows endpoint (authenticate, pagination)
- [ ] T138 [US3] Add POST /api/follows endpoint to `FollowsController` (authenticate, validate, call FollowAuctionAsync)
- [ ] T139 [US3] Add DELETE /api/follows/{auctionId} endpoint to `FollowsController` (authenticate, call UnfollowAuctionAsync)

**Checkpoint**: At this point, User Stories 1, 2, AND 3 should all work independently. Users can browse, sellers can manage, and users can track auctions.

---

## Phase 6: User Story 4 - 商品狀態自動管理 (Priority: P3)

**Goal**: 系統根據時間自動更新商品狀態，將已結束的商品標記為「已結束」，確保商品清單顯示正確的狀態。

**Independent Test**: 可透過建立即將結束的商品，等待時間過期後驗證狀態是否自動更新為「已結束」。

**Note**: 根據 research.md Decision 6，商品狀態採用計算式設計（查詢時動態計算），無需背景任務。此 Phase 主要驗證計算邏輯正確性與效能優化。

### Tests for User Story 4 (TDD - Write First)

- [ ] T140 [P] [US4] Unit test for `Auction.Status` computed property in `tests/AuctionService.Domain.Tests/Entities/AuctionTests.cs` (edge cases: exactly StartTime, exactly EndTime, timezone handling)
- [ ] T141 [P] [US4] Integration test for status filtering in `tests/AuctionService.IntegrationTests/Controllers/AuctionsControllerTests.cs` (verify "Active" filter excludes Pending/Ended auctions)
- [ ] T142 [P] [US4] Performance test for status calculation in `tests/AuctionService.IntegrationTests/Performance/AuctionStatusPerformanceTests.cs` (10k auctions, verify <500ms query time)

### Implementation for User Story 4

- [ ] T143 [US4] Verify `Auction.Status` computed property in `src/AuctionService.Domain/Entities/Auction.cs` handles all edge cases (Pending: NOW < StartTime, Active: StartTime <= NOW < EndTime, Ended: NOW >= EndTime)
- [ ] T144 [US4] Add EndTime index optimization to `AuctionConfiguration` in `src/AuctionService.Infrastructure/Persistence/Configurations/AuctionConfiguration.cs` (CREATE INDEX idx_auction_endtime)
- [ ] T145 [US4] Update `AuctionRepository.GetAllAsync` to support status filtering using computed CASE WHEN in SQL query
- [ ] T146 [US4] Add monitoring/logging for status calculation performance in `AuctionService.Application/Services/AuctionService.cs`

**Checkpoint**: All user stories should now be independently functional. Status management is passive and accurate.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T147 [P] Add XML documentation comments to all public APIs in Domain/Application layers
- [ ] T148 [P] Add Swagger/OpenAPI documentation in `Program.cs` (use contracts/openapi.yaml as reference)
- [ ] T149 [P] Add correlation ID logging to all service methods (inject ILogger<T>)
- [ ] T150 [P] Add performance logging (Stopwatch) for Bidding Service calls in `BiddingServiceClient`
- [ ] T151 [P] Add performance logging for image upload operations in `ImageStorageService`
- [ ] T152 [P] Add cache hit/miss rate logging in `RedisCacheService`
- [ ] T153 Code review: Verify all repository methods use async/await correctly
- [ ] T154 Code review: Verify all controllers return StandardResponse<T> with ResponseMetadata
- [ ] T155 Code review: Verify all DTOs have correct validation attributes
- [ ] T156 Security: Add rate limiting middleware for API endpoints
- [ ] T157 Security: Add input sanitization for search keywords (prevent SQL injection)
- [ ] T158 Security: Add CORS policy configuration in `Program.cs`
- [ ] T159 [P] Performance: Add response compression middleware
- [ ] T160 [P] Performance: Add ETag support for GET /api/auctions responses
- [ ] T161 [P] Documentation: Update README.md with architecture diagram
- [ ] T162 [P] Documentation: Update quickstart.md with final setup steps
- [ ] T163 Run `dotnet test` across all test projects, verify >80% coverage
- [ ] T164 Run integration tests with Testcontainers, verify all scenarios pass
- [ ] T165 Run quickstart.md validation: Start docker-compose, run migrations, verify API responds
- [ ] T166 Generate test coverage report with `dotnet test /p:CollectCoverage=true`
- [ ] T167 Final code cleanup: Remove unused imports, fix compiler warnings
- [ ] T168 Final commit: Prepare for merge to master

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-6)**: All depend on Foundational phase completion
  - US1 (P1): Can start after Foundational - No dependencies on other stories
  - US2 (P1): Can start after Foundational - No dependencies on other stories (can run parallel with US1)
  - US3 (P2): Depends on US1 + US2 (needs Auction entity + endpoints for integration)
  - US4 (P3): Depends on US1 (needs Auction entity + status property)
- **Polish (Phase 7)**: Depends on all MVP user stories (US1 + US2) completion

### MVP Scope

**Minimum Viable Product includes**:
- Phase 1: Setup (22 tasks)
- Phase 2: Foundational (44 tasks)
- Phase 3: User Story 1 - 瀏覽與搜尋 (26 tasks)
- Phase 4: User Story 2 - 建立與管理 (29 tasks)
- Phase 7: Polish (selected tasks: T147, T148, T163-T168)

**Total MVP Tasks**: ~128 tasks

**Post-MVP Enhancement**:
- Phase 5: User Story 3 - 追蹤商品 (18 tasks)
- Phase 6: User Story 4 - 狀態管理 (7 tasks)
- Phase 7: Polish (remaining tasks: T149-T162)

**Total Post-MVP Tasks**: ~40 tasks

### Parallel Opportunities

**Phase 1 (Setup)**: Tasks T006-T009, T011-T014 can run in parallel (different projects)

**Phase 2 (Foundational)**:
- Tasks T023-T031 can run in parallel (domain exceptions/value objects)
- Tasks T036-T039 can run in parallel (repository interfaces)
- Tasks T040-T043 can run in parallel (application interfaces)
- Tasks T045-T048 can run in parallel (EF Core configurations)
- Tasks T052-T055 can run in parallel (repository implementations)
- Tasks T064-T066 can run in parallel (test fixtures)

**Phase 3 (User Story 1)**:
- All test tasks T067-T075 can run in parallel (different test files)
- All DTO tasks T076-T080 can run in parallel (different files)
- Mapper tasks T087-T088 can run in parallel
- Controller tasks T089-T092 can run sequentially (same controller file)

**Phase 4 (User Story 2)**:
- All test tasks T093-T103 can run in parallel
- All DTO/Validator tasks T104-T107 can run in parallel
- Image storage client tasks T113-T115 can run in parallel (different cloud providers)

**Phase 5 (User Story 3)**:
- All test tasks T122-T129 can run in parallel
- All DTO tasks T130-T132 can run in parallel

**Phase 6 (User Story 4)**:
- All test tasks T140-T142 can run in parallel

**Phase 7 (Polish)**:
- Most documentation/logging tasks T147-T152 can run in parallel

### Recommended Team Allocation

**Single Developer** (Sequential Execution):
1. Phase 1: Setup (2-3 days)
2. Phase 2: Foundational (1-2 weeks)
3. Phase 3: US1 瀏覽與搜尋 (1 week)
4. Phase 4: US2 建立與管理 (1 week)
5. Phase 7: Polish MVP (3-5 days)
6. **MVP Complete** (6-7 weeks)
7. Phase 5: US3 追蹤商品 (3-4 days)
8. Phase 6: US4 狀態管理 (1-2 days)
9. Phase 7: Polish Post-MVP (2-3 days)

**Total Estimated Time**: 7-8 weeks for full implementation

**Two Developers** (Parallel Execution):
- Dev A: Phase 1 + Phase 2 (Domain/Application) → Phase 3 (US1)
- Dev B: Phase 2 (Infrastructure/API) → Phase 4 (US2)
- Both: Phase 7 (Polish)
- **MVP Complete**: 4-5 weeks

**Three Developers** (Maximum Parallelization):
- Dev A: Phase 3 (US1)
- Dev B: Phase 4 (US2)
- Dev C: Phase 5 (US3) + Phase 6 (US4)
- All: Phase 1 + Phase 2 (Foundational) must complete first
- **MVP Complete**: 3-4 weeks
- **Full Implementation**: 5-6 weeks

---

## Validation Checklist

Before marking implementation complete, verify:

- [ ] All 4 user stories have passing integration tests
- [ ] Test coverage >80% per constitution requirement
- [ ] All API endpoints match contracts/openapi.yaml specification
- [ ] All ResponseCodes table entries are seeded
- [ ] All 6 Categories are pre-seeded (電子產品, 家具, 收藏品, 服飾, 書籍, 其他)
- [ ] Snowflake ID generation works correctly with configured WorkerId/DatacenterId
- [ ] RowVersion concurrency control returns 409 Conflict on concurrent edits
- [ ] Bidding Service cache fallback works when service unavailable
- [ ] PostgreSQL full-text search returns relevant results
- [ ] Image upload to MinIO/S3/Blob works with 5MB/5 images limits
- [ ] Health check endpoints return correct status
- [ ] Correlation IDs are logged for all requests
- [ ] Serilog structured logging is configured
- [ ] Docker Compose starts all services successfully
- [ ] EF Core migrations apply cleanly to empty database
- [ ] quickstart.md instructions work for new developers

---

**Total Tasks**: 168  
**MVP Tasks**: ~128  
**Post-MVP Tasks**: ~40  
**Parallel Opportunities**: ~60 tasks marked [P]  
**Estimated MVP Timeline**: 6-7 weeks (single developer), 4-5 weeks (two developers), 3-4 weeks (three developers)

**Last Updated**: 2025-11-20  
**Author**: GitHub Copilot (Claude Sonnet 4.5)  
**Status**: Ready for Implementation - TDD Workflow Enforced
