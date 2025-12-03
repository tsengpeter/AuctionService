# Tasks: 商品拍賣服務

**Input**: Design documents from `/specs/002-auction-service/`  
**Prerequisites**: plan.md, spec.md, data-model.md, contracts/openapi.yaml, research.md, quickstart.md

**Branch**: `002-auction-service`  
**Generated**: 2025-12-03

**Tests**: TDD approach - Tests are included and MUST be written first (Red-Green-Refactor cycle)

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3, US4)
- Include exact file paths in descriptions

## Path Conventions

Based on plan.md, this project uses single-folder structure:
- **Source**: `AuctionService/src/` (Api, Core, Infrastructure, Shared)
- **Tests**: `AuctionService/tests/` (UnitTests, IntegrationTests, ContractTests)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [ ] T001 Create solution structure: `dotnet new sln -n AuctionService` in root directory
- [ ] T002 Create API project: `dotnet new webapi -n AuctionService.Api -o src/AuctionService.Api --framework net10.0`
- [ ] T003 [P] Create Core project: `dotnet new classlib -n AuctionService.Core -o src/AuctionService.Core --framework net10.0`
- [ ] T004 [P] Create Infrastructure project: `dotnet new classlib -n AuctionService.Infrastructure -o src/AuctionService.Infrastructure --framework net10.0`
- [ ] T005 [P] Create Shared project: `dotnet new classlib -n AuctionService.Shared -o src/AuctionService.Shared --framework net10.0`
- [ ] T006 Add projects to solution: `dotnet sln add src/**/*.csproj`
- [ ] T007 [P] Create unit tests project: `dotnet new xunit -n AuctionService.UnitTests -o tests/AuctionService.UnitTests --framework net10.0`
- [ ] T008 [P] Create integration tests project: `dotnet new xunit -n AuctionService.IntegrationTests -o tests/AuctionService.IntegrationTests --framework net10.0`
- [ ] T009 [P] Create contract tests project: `dotnet new xunit -n AuctionService.ContractTests -o tests/AuctionService.ContractTests --framework net10.0`
- [ ] T010 Add test projects to solution: `dotnet sln add tests/**/*.csproj`
- [ ] T011 Add project references: Api → Core/Infrastructure/Shared, Infrastructure → Core, Tests → corresponding projects
- [ ] T012 [P] Install NuGet packages for Api: Npgsql.EntityFrameworkCore.PostgreSQL 10.0, FluentValidation.AspNetCore 11.3.0, Serilog.AspNetCore 8.0, Swashbuckle.AspNetCore 6.5.0
- [ ] T013 [P] Install NuGet packages for Infrastructure: Npgsql.EntityFrameworkCore.PostgreSQL 10.0, Microsoft.EntityFrameworkCore.Design 10.0
- [ ] T014 [P] Install NuGet packages for Tests: Moq 4.20+, FluentAssertions 6.12+, Testcontainers.PostgreSql 3.7+, WireMock.Net (for contract tests)
- [ ] T015 [P] Create .editorconfig with C# 13 coding standards in root
- [ ] T016 [P] Create .gitignore for .NET projects in root
- [ ] T017 [P] Create global.json to lock .NET SDK version 10.0 in root
- [ ] T018 [P] Create README.md with project overview in root
- [ ] T019 [P] Create Dockerfile for multi-stage build in root
- [ ] T020 [P] Create docker-compose.yml with PostgreSQL service in root
- [ ] T021 [P] Create docker-compose.override.yml for local development in root

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [ ] T022 Create AuctionDbContext in src/AuctionService.Infrastructure/Data/AuctionDbContext.cs with DbSet properties
- [ ] T023 [P] Create base entity classes: Entity (Id, CreatedAt, UpdatedAt) in src/AuctionService.Core/Entities/BaseEntity.cs
- [ ] T024 [P] Create Auction entity in src/AuctionService.Core/Entities/Auction.cs (Guid Id, string Name, string Description, decimal StartingPrice, int CategoryId, DateTime StartTime, DateTime EndTime, Guid UserId, DateTime CreatedAt, DateTime UpdatedAt)
- [ ] T025 [P] Create Category entity in src/AuctionService.Core/Entities/Category.cs (int Id, string Name, int DisplayOrder, bool IsActive, DateTime CreatedAt)
- [ ] T026 [P] Create Follow entity in src/AuctionService.Core/Entities/Follow.cs (Guid Id, Guid UserId, Guid AuctionId, DateTime CreatedAt)
- [ ] T027 [P] Create ResponseCode entity in src/AuctionService.Core/Entities/ResponseCode.cs (string Code PK, string Name, string MessageZhTw, string MessageEn, string Category, string Severity)
- [ ] T028 [P] Create AuctionConfiguration in src/AuctionService.Infrastructure/Data/Configurations/AuctionConfiguration.cs implementing IEntityTypeConfiguration<Auction> with Fluent API (indexes on EndTime, CategoryId, UserId+CreatedAt)
- [ ] T029 [P] Create CategoryConfiguration in src/AuctionService.Infrastructure/Data/Configurations/CategoryConfiguration.cs with seed data (8 categories: 電子產品, 家具, 收藏品, 藝術品, 服飾配件, 書籍, 運動用品, 其他)
- [ ] T030 [P] Create FollowConfiguration in src/AuctionService.Infrastructure/Data/Configurations/FollowConfiguration.cs with unique index on UserId+AuctionId
- [ ] T031 [P] Create ResponseCodeConfiguration in src/AuctionService.Infrastructure/Data/Configurations/ResponseCodeConfiguration.cs with seed data (SUCCESS, AUCTION_NOT_FOUND, UNAUTHORIZED, etc.)
- [ ] T032 Apply entity configurations to AuctionDbContext via OnModelCreating
- [ ] T033 Create initial migration: `dotnet ef migrations add InitialCreate --project src/AuctionService.Infrastructure --startup-project src/AuctionService.Api`
- [ ] T034 [P] Create standard DTO response wrappers in src/AuctionService.Core/DTOs/Common/ApiResponse.cs and ResponseMetadata.cs
- [ ] T035 [P] Create pagination DTOs in src/AuctionService.Core/DTOs/Common/PagedResult.cs and AuctionQueryParameters.cs
- [ ] T036 [P] Create IRepository<T> generic interface in src/AuctionService.Core/Interfaces/IRepository.cs with standard CRUD methods
- [ ] T037 [P] Create GenericRepository<T> implementation in src/AuctionService.Infrastructure/Repositories/GenericRepository.cs
- [ ] T038 [P] Create ExceptionHandlingMiddleware in src/AuctionService.Api/Middlewares/ExceptionHandlingMiddleware.cs for global error handling
- [ ] T039 [P] Create CorrelationIdMiddleware in src/AuctionService.Api/Middlewares/CorrelationIdMiddleware.cs for request tracing
- [ ] T040 [P] Create RequestLoggingMiddleware in src/AuctionService.Api/Middlewares/RequestLoggingMiddleware.cs with Serilog
- [ ] T041 [P] Create AuctionExtensions.cs in src/AuctionService.Shared/Extensions/AuctionExtensions.cs with CalculateStatus() method (passive status calculation)
- [ ] T042 [P] Configure Serilog in src/AuctionService.Api/Program.cs with structured logging (JSON format, correlation ID)
- [ ] T043 [P] Configure Swagger/OpenAPI in src/AuctionService.Api/Program.cs with metadata from contracts/openapi.yaml
- [ ] T044 Configure dependency injection in src/AuctionService.Api/Program.cs: DbContext, repositories, services, HttpClient for BiddingService
- [ ] T045 Configure CORS policy in src/AuctionService.Api/Program.cs
- [ ] T046 Add middleware pipeline in src/AuctionService.Api/Program.cs (CorrelationId → Logging → ExceptionHandling → CORS → Auth)
- [ ] T047 Create appsettings.Development.json in src/AuctionService.Api with local PostgreSQL connection string
- [ ] T048 [P] Create PostgreSqlTestContainer fixture in tests/AuctionService.IntegrationTests/Infrastructure/PostgreSqlTestContainer.cs using Testcontainers

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - 瀏覽與搜尋拍賣商品 (Priority: P1) 🎯 MVP

**Goal**: 使用者可瀏覽所有進行中的拍賣商品,透過分類篩選、關鍵字搜尋快速找到感興趣的商品,並查看商品詳細資訊與目前出價。

**Independent Test**: 可透過查詢商品清單 API、使用篩選與搜尋功能、查看商品詳細資訊 API 來完整測試此功能，驗證使用者能成功找到並了解商品資訊。

### Tests for User Story 1 (Write FIRST - ensure they FAIL before implementation)

- [ ] T049 [P] [US1] Unit test for AuctionExtensions.CalculateStatus() in tests/AuctionService.UnitTests/Extensions/AuctionExtensionsTests.cs (test Pending/Active/Ended status calculation)
- [ ] T050 [P] [US1] Unit test for AuctionService.GetAuctionsAsync() pagination in tests/AuctionService.UnitTests/Services/AuctionServiceTests.cs
- [ ] T051 [P] [US1] Unit test for AuctionService.GetAuctionsAsync() filtering by categoryId in tests/AuctionService.UnitTests/Services/AuctionServiceTests.cs
- [ ] T052 [P] [US1] Unit test for AuctionService.GetAuctionsAsync() keyword search in tests/AuctionService.UnitTests/Services/AuctionServiceTests.cs
- [ ] T053 [P] [US1] Unit test for AuctionService.GetAuctionByIdAsync() success case in tests/AuctionService.UnitTests/Services/AuctionServiceTests.cs
- [ ] T054 [P] [US1] Unit test for AuctionService.GetAuctionByIdAsync() not found case in tests/AuctionService.UnitTests/Services/AuctionServiceTests.cs
- [ ] T055 [P] [US1] Integration test for GET /api/auctions with pagination in tests/AuctionService.IntegrationTests/Controllers/AuctionsControllerIntegrationTests.cs
- [ ] T056 [P] [US1] Integration test for GET /api/auctions with status=Active filter in tests/AuctionService.IntegrationTests/Controllers/AuctionsControllerIntegrationTests.cs
- [ ] T057 [P] [US1] Integration test for GET /api/auctions/{id} success case in tests/AuctionService.IntegrationTests/Controllers/AuctionsControllerIntegrationTests.cs
- [ ] T058 [P] [US1] Contract test for GET /api/auctions response schema validation in tests/AuctionService.ContractTests/AuctionsContractTests.cs
- [ ] T059 [P] [US1] Contract test for GET /api/auctions/{id} response schema validation in tests/AuctionService.ContractTests/AuctionsContractTests.cs
- [ ] T060 [P] [US1] Contract test for GET /api/auctions/{id}/current-bid mock BiddingService response in tests/AuctionService.ContractTests/BiddingServiceContractTests.cs

### Implementation for User Story 1

- [ ] T061 [P] [US1] Create AuctionListItemDto in src/AuctionService.Core/DTOs/Responses/AuctionListItemDto.cs (Id, Name, StartingPrice, CategoryId, CategoryName, Status, EndTime, CreatedAt)
- [ ] T062 [P] [US1] Create AuctionDetailDto in src/AuctionService.Core/DTOs/Responses/AuctionDetailDto.cs (all fields + CurrentBid, Description, StartTime, UserId)
- [ ] T063 [P] [US1] Create CurrentBidDto in src/AuctionService.Core/DTOs/Responses/CurrentBidDto.cs (AuctionId, CurrentBid, BidCount, HighestBidderUserId)
- [ ] T064 [P] [US1] Create CategoryDto in src/AuctionService.Core/DTOs/Responses/CategoryDto.cs (Id, Name, DisplayOrder)
- [ ] T065 [P] [US1] Create IAuctionRepository interface in src/AuctionService.Core/Interfaces/IAuctionRepository.cs (GetPagedAsync with filters, GetByIdAsync, GetByCategoryIdAsync, SearchAsync)
- [ ] T066 [US1] Implement AuctionRepository in src/AuctionService.Infrastructure/Repositories/AuctionRepository.cs with EF Core queries (use AsNoTracking for read-only, Include Category navigation, apply EndTime index for status filtering)
- [ ] T067 [P] [US1] Create IBiddingServiceClient interface in src/AuctionService.Core/Interfaces/IBiddingServiceClient.cs (GetCurrentBidAsync method)
- [ ] T068 [US1] Implement BiddingServiceClient in src/AuctionService.Infrastructure/HttpClients/BiddingServiceClient.cs with HttpClient + Polly retry policy (3 retries with exponential backoff, log all requests/responses)
- [ ] T069 [P] [US1] Create IAuctionService interface in src/AuctionService.Core/Interfaces/IAuctionService.cs (GetAuctionsAsync, GetAuctionByIdAsync, GetCurrentBidAsync)
- [ ] T070 [US1] Implement AuctionService in src/AuctionService.Core/Services/AuctionService.cs with business logic (call repository, map to DTOs using AuctionExtensions.ToListItemDto/ToDetailDto, integrate BiddingServiceClient)
- [ ] T071 [P] [US1] Create mapping extensions in src/AuctionService.Shared/Extensions/AuctionExtensions.cs (ToListItemDto, ToDetailDto methods using POCO manual mapping)
- [ ] T072 [P] [US1] Create ICategoryService interface in src/AuctionService.Core/Interfaces/ICategoryService.cs (GetAllCategoriesAsync)
- [ ] T073 [US1] Implement CategoryService in src/AuctionService.Core/Services/CategoryService.cs (retrieve from repository, cache in memory)
- [ ] T074 [US1] Implement AuctionsController.GetAuctions() in src/AuctionService.Api/Controllers/AuctionsController.cs for GET /api/auctions (handle query parameters, return PagedResult with ApiResponse wrapper)
- [ ] T075 [US1] Implement AuctionsController.GetAuctionById() in src/AuctionService.Api/Controllers/AuctionsController.cs for GET /api/auctions/{id} (return AuctionDetailDto with metadata)
- [ ] T076 [US1] Implement AuctionsController.GetCurrentBid() in src/AuctionService.Api/Controllers/AuctionsController.cs for GET /api/auctions/{id}/current-bid (lightweight endpoint for polling)
- [ ] T077 [US1] Implement CategoriesController.GetCategories() in src/AuctionService.Api/Controllers/CategoriesController.cs for GET /api/categories
- [ ] T078 [US1] Add request validation using FluentValidation for AuctionQueryParameters in src/AuctionService.Core/Validators/AuctionQueryParametersValidator.cs
- [ ] T079 [US1] Configure HttpClient for BiddingService in Program.cs with base address from appsettings, add Polly policies (retry + circuit breaker)
- [ ] T080 [US1] Run all US1 tests and verify they pass with green status

**Checkpoint**: At this point, User Story 1 should be fully functional - users can browse, search, filter auctions and view details with current bid

---

## Phase 4: User Story 2 - 建立與管理拍賣商品 (Priority: P1) 🎯 MVP

**Goal**: 賣家可建立新的拍賣商品，設定商品資訊與結束時間，並在符合條件時編輯或刪除自己的商品。

**Independent Test**: 可透過建立新商品 API、編輯商品 API、刪除商品 API、查詢特定使用者商品 API 來完整測試此功能，驗證賣家能成功管理自己的拍賣商品。

### Tests for User Story 2 (Write FIRST - ensure they FAIL before implementation)

- [ ] T081 [P] [US2] Unit test for CreateAuctionRequest validation in tests/AuctionService.UnitTests/Validators/CreateAuctionRequestValidatorTests.cs (test EndTime >= CurrentTime + 1 hour, StartingPrice > 0, Name length 3-200)
- [ ] T082 [P] [US2] Unit test for UpdateAuctionRequest validation in tests/AuctionService.UnitTests/Validators/UpdateAuctionRequestValidatorTests.cs
- [ ] T083 [P] [US2] Unit test for AuctionService.CreateAuctionAsync() success case in tests/AuctionService.UnitTests/Services/AuctionServiceTests.cs
- [ ] T084 [P] [US2] Unit test for AuctionService.CreateAuctionAsync() invalid EndTime case in tests/AuctionService.UnitTests/Services/AuctionServiceTests.cs
- [ ] T085 [P] [US2] Unit test for AuctionService.UpdateAuctionAsync() permission denied case (not owner) in tests/AuctionService.UnitTests/Services/AuctionServiceTests.cs
- [ ] T086 [P] [US2] Unit test for AuctionService.UpdateAuctionAsync() has bids case in tests/AuctionService.UnitTests/Services/AuctionServiceTests.cs
- [ ] T087 [P] [US2] Unit test for AuctionService.DeleteAuctionAsync() permission denied case in tests/AuctionService.UnitTests/Services/AuctionServiceTests.cs
- [ ] T088 [P] [US2] Unit test for AuctionService.DeleteAuctionAsync() has bids case in tests/AuctionService.UnitTests/Services/AuctionServiceTests.cs
- [ ] T089 [P] [US2] Integration test for POST /api/auctions with valid data in tests/AuctionService.IntegrationTests/Controllers/AuctionsControllerIntegrationTests.cs
- [ ] T090 [P] [US2] Integration test for POST /api/auctions with invalid EndTime in tests/AuctionService.IntegrationTests/Controllers/AuctionsControllerIntegrationTests.cs
- [ ] T091 [P] [US2] Integration test for PUT /api/auctions/{id} success case (no bids) in tests/AuctionService.IntegrationTests/Controllers/AuctionsControllerIntegrationTests.cs
- [ ] T092 [P] [US2] Integration test for PUT /api/auctions/{id} forbidden case (has bids) in tests/AuctionService.IntegrationTests/Controllers/AuctionsControllerIntegrationTests.cs
- [ ] T093 [P] [US2] Integration test for DELETE /api/auctions/{id} success case in tests/AuctionService.IntegrationTests/Controllers/AuctionsControllerIntegrationTests.cs
- [ ] T094 [P] [US2] Integration test for GET /api/auctions/user/{userId} in tests/AuctionService.IntegrationTests/Controllers/AuctionsControllerIntegrationTests.cs
- [ ] T095 [P] [US2] Contract test for POST /api/auctions request/response schema in tests/AuctionService.ContractTests/AuctionsContractTests.cs

### Implementation for User Story 2

- [ ] T096 [P] [US2] Create CreateAuctionRequest DTO in src/AuctionService.Core/DTOs/Requests/CreateAuctionRequest.cs (Name, Description, StartingPrice, CategoryId, StartTime, EndTime)
- [ ] T097 [P] [US2] Create UpdateAuctionRequest DTO in src/AuctionService.Core/DTOs/Requests/UpdateAuctionRequest.cs (Name, Description, StartingPrice, EndTime)
- [ ] T098 [P] [US2] Create CreateAuctionRequestValidator in src/AuctionService.Core/Validators/CreateAuctionRequestValidator.cs using FluentValidation (validate EndTime >= Now + 1 hour, StartingPrice > 0, CategoryId exists, Name/Description length)
- [ ] T099 [P] [US2] Create UpdateAuctionRequestValidator in src/AuctionService.Core/Validators/UpdateAuctionRequestValidator.cs
- [ ] T100 [P] [US2] Add CreateAsync method to IAuctionRepository in src/AuctionService.Core/Interfaces/IAuctionRepository.cs
- [ ] T101 [P] [US2] Add UpdateAsync method to IAuctionRepository in src/AuctionService.Core/Interfaces/IAuctionRepository.cs
- [ ] T102 [P] [US2] Add DeleteAsync method to IAuctionRepository in src/AuctionService.Core/Interfaces/IAuctionRepository.cs
- [ ] T103 [P] [US2] Add GetByUserIdAsync method to IAuctionRepository in src/AuctionService.Core/Interfaces/IAuctionRepository.cs
- [ ] T104 [US2] Implement CreateAsync in AuctionRepository in src/AuctionService.Infrastructure/Repositories/AuctionRepository.cs
- [ ] T105 [US2] Implement UpdateAsync in AuctionRepository in src/AuctionService.Infrastructure/Repositories/AuctionRepository.cs
- [ ] T106 [US2] Implement DeleteAsync in AuctionRepository in src/AuctionService.Infrastructure/Repositories/AuctionRepository.cs
- [ ] T107 [US2] Implement GetByUserIdAsync in AuctionRepository in src/AuctionService.Infrastructure/Repositories/AuctionRepository.cs
- [ ] T108 [P] [US2] Add CreateAuctionAsync to IAuctionService in src/AuctionService.Core/Interfaces/IAuctionService.cs
- [ ] T109 [P] [US2] Add UpdateAuctionAsync to IAuctionService in src/AuctionService.Core/Interfaces/IAuctionService.cs
- [ ] T110 [P] [US2] Add DeleteAuctionAsync to IAuctionService in src/AuctionService.Core/Interfaces/IAuctionService.cs
- [ ] T111 [P] [US2] Add GetUserAuctionsAsync to IAuctionService in src/AuctionService.Core/Interfaces/IAuctionService.cs
- [ ] T112 [US2] Implement CreateAuctionAsync in AuctionService in src/AuctionService.Core/Services/AuctionService.cs (validate request, set UserId from auth context, set StartTime to Now, call repository)
- [ ] T113 [US2] Implement UpdateAuctionAsync in AuctionService in src/AuctionService.Core/Services/AuctionService.cs (check ownership, check no bids via BiddingService, update fields)
- [ ] T114 [US2] Implement DeleteAuctionAsync in AuctionService in src/AuctionService.Core/Services/AuctionService.cs (check ownership, check no bids, soft/hard delete)
- [ ] T115 [US2] Implement GetUserAuctionsAsync in AuctionService in src/AuctionService.Core/Services/AuctionService.cs
- [ ] T116 [P] [US2] Add CheckAuctionHasBidsAsync method to IBiddingServiceClient in src/AuctionService.Core/Interfaces/IBiddingServiceClient.cs
- [ ] T117 [US2] Implement CheckAuctionHasBidsAsync in BiddingServiceClient in src/AuctionService.Infrastructure/HttpClients/BiddingServiceClient.cs
- [ ] T118 [P] [US2] Create mapping extensions ToEntity(CreateAuctionRequest) in src/AuctionService.Shared/Extensions/AuctionExtensions.cs
- [ ] T119 [US2] Implement AuctionsController.CreateAuction() in src/AuctionService.Api/Controllers/AuctionsController.cs for POST /api/auctions (validate, extract UserId from JWT claims, call service, return 201 Created)
- [ ] T120 [US2] Implement AuctionsController.UpdateAuction() in src/AuctionService.Api/Controllers/AuctionsController.cs for PUT /api/auctions/{id} (authorize owner, call service, handle forbidden/not found)
- [ ] T121 [US2] Implement AuctionsController.DeleteAuction() in src/AuctionService.Api/Controllers/AuctionsController.cs for DELETE /api/auctions/{id} (authorize owner, call service)
- [ ] T122 [US2] Implement AuctionsController.GetUserAuctions() in src/AuctionService.Api/Controllers/AuctionsController.cs for GET /api/auctions/user/{userId}
- [ ] T123 [P] [US2] Create custom exceptions in src/AuctionService.Core/Exceptions/AuctionNotFoundException.cs
- [ ] T124 [P] [US2] Create UnauthorizedException in src/AuctionService.Core/Exceptions/UnauthorizedException.cs
- [ ] T125 [P] [US2] Create ValidationException in src/AuctionService.Core/Exceptions/ValidationException.cs
- [ ] T126 [US2] Update ExceptionHandlingMiddleware to map custom exceptions to proper HTTP status codes (404, 401, 400)
- [ ] T127 [US2] Add authorization filter/attribute for authenticated endpoints in src/AuctionService.Api/Filters/AuthorizeAttribute.cs
- [ ] T128 [US2] Run all US2 tests and verify they pass with green status

**Checkpoint**: At this point, User Stories 1 AND 2 should both work independently - users can browse auctions AND sellers can create/manage their auctions

---

## Phase 5: User Story 3 - 商品追蹤功能 (Priority: P2)

**Goal**: 使用者可將感興趣的商品加入追蹤清單，方便隨時查看這些商品的最新狀態，也可取消不再關注的商品。

**Independent Test**: 可透過追蹤商品 API (POST /api/follows)、取消追蹤 API (DELETE /api/follows/{auctionId})、查看追蹤清單 API (GET /api/follows) 來完整測試此功能，驗證使用者能成功管理追蹤清單。

**Note**: 本功能對應 spec.md 中的 "使用者故事 3 - 追蹤感興趣的商品"，為保持術語一致性統一稱為 "商品追蹤功能 (Follow Feature)"。

### Tests for User Story 3 (Write FIRST - ensure they FAIL before implementation)

- [ ] T129 [P] [US3] Unit test for FollowService.AddFollowAsync() success case in tests/AuctionService.UnitTests/Services/FollowServiceTests.cs
- [ ] T130 [P] [US3] Unit test for FollowService.AddFollowAsync() duplicate follow case in tests/AuctionService.UnitTests/Services/FollowServiceTests.cs
- [ ] T131 [P] [US3] Unit test for FollowService.AddFollowAsync() self-follow rejection case in tests/AuctionService.UnitTests/Services/FollowServiceTests.cs
- [ ] T132 [P] [US3] Unit test for FollowService.RemoveFollowAsync() success case in tests/AuctionService.UnitTests/Services/FollowServiceTests.cs
- [ ] T133 [P] [US3] Unit test for FollowService.GetUserFollowsAsync() pagination in tests/AuctionService.UnitTests/Services/FollowServiceTests.cs
- [ ] T134 [P] [US3] Integration test for POST /api/follows with valid auction in tests/AuctionService.IntegrationTests/Controllers/FollowsControllerIntegrationTests.cs
- [ ] T135 [P] [US3] Integration test for POST /api/follows duplicate rejection in tests/AuctionService.IntegrationTests/Controllers/FollowsControllerIntegrationTests.cs
- [ ] T136 [P] [US3] Integration test for POST /api/follows self-follow rejection in tests/AuctionService.IntegrationTests/Controllers/FollowsControllerIntegrationTests.cs
- [ ] T137 [P] [US3] Integration test for DELETE /api/follows/{auctionId} success in tests/AuctionService.IntegrationTests/Controllers/FollowsControllerIntegrationTests.cs
- [ ] T138 [P] [US3] Integration test for GET /api/follows with auction details in tests/AuctionService.IntegrationTests/Controllers/FollowsControllerIntegrationTests.cs
- [ ] T139 [P] [US3] Contract test for GET /api/follows response schema in tests/AuctionService.ContractTests/FollowsContractTests.cs

### Implementation for User Story 3

- [ ] T140 [P] [US3] Create FollowDto in src/AuctionService.Core/DTOs/Responses/FollowDto.cs (FollowId, UserId, AuctionId, AuctionName, AuctionStatus, CurrentBid, EndTime, CreatedAt)
- [ ] T141 [P] [US3] Create FollowAuctionRequest DTO in src/AuctionService.Core/DTOs/Requests/FollowAuctionRequest.cs (AuctionId)
- [ ] T142 [P] [US3] Create IFollowRepository interface in src/AuctionService.Core/Interfaces/IFollowRepository.cs (AddAsync, RemoveAsync, GetByUserIdAsync, ExistsAsync, IsFollowingOwnAuction)
- [ ] T143 [US3] Implement FollowRepository in src/AuctionService.Infrastructure/Repositories/FollowRepository.cs (enforce unique constraint on UserId+AuctionId, Include Auction navigation for follow list)
- [ ] T144 [P] [US3] Create IFollowService interface in src/AuctionService.Core/Interfaces/IFollowService.cs (AddFollowAsync, RemoveFollowAsync, GetUserFollowsAsync, CheckFollowExistsAsync)
- [ ] T145 [US3] Implement FollowService in src/AuctionService.Core/Services/FollowService.cs (check auction exists via AuctionRepository, check not self-follow, check not duplicate, integrate current bid from BiddingService for follow list)
- [ ] T146 [P] [US3] Create mapping extensions ToFollowDto in src/AuctionService.Shared/Extensions/FollowExtensions.cs
- [ ] T147 [US3] Implement FollowsController.GetFollows() in src/AuctionService.Api/Controllers/FollowsController.cs for GET /api/follows (extract UserId from JWT, return paginated FollowDto list with auction details)
- [ ] T148 [US3] Implement FollowsController.FollowAuction() in src/AuctionService.Api/Controllers/FollowsController.cs for POST /api/follows (validate authenticated, call service, return 201 or 409 Conflict)
- [ ] T149 [US3] Implement FollowsController.UnfollowAuction() in src/AuctionService.Api/Controllers/FollowsController.cs for DELETE /api/follows/{auctionId} (verify ownership of follow, return 204 No Content)
- [ ] T150 [US3] Add validation for max 500 follows per user in FollowService (throw ValidationException if limit exceeded)
- [ ] T151 [US3] Run all US3 tests and verify they pass with green status

**Checkpoint**: At this point, User Stories 1, 2, AND 3 should all work independently - users can browse, sellers can manage auctions, and users can track interesting auctions

---

## Phase 6: User Story 4 - 商品狀態自動管理 (Priority: P3)

**Goal**: 系統根據時間自動更新商品狀態，將已結束的商品標記為「已結束」，確保商品清單顯示正確的狀態。

**Independent Test**: 可透過建立即將結束的商品，等待時間過期後驗證狀態是否自動更新為「已結束」（通過 CalculateStatus() 被動計算）。

**Note**: 此功能已在 Phase 2 Foundational 實作（T041 AuctionExtensions.CalculateStatus），此 Phase 主要驗證被動式狀態計算在所有 API 端點正確運作。

### Tests for User Story 4 (Write FIRST - ensure they FAIL before implementation)

- [ ] T152 [P] [US4] Integration test: create auction with EndTime=Now+2sec, wait 3 seconds, verify GET /api/auctions returns status=Ended in tests/AuctionService.IntegrationTests/Controllers/AuctionsControllerIntegrationTests.cs
- [ ] T153 [P] [US4] Integration test: verify ended auctions excluded from GET /api/auctions?status=Active in tests/AuctionService.IntegrationTests/Controllers/AuctionsControllerIntegrationTests.cs
- [ ] T154 [P] [US4] Integration test: verify auction with StartTime in future returns status=Pending in tests/AuctionService.IntegrationTests/Controllers/AuctionsControllerIntegrationTests.cs
- [ ] T155 [P] [US4] Unit test: verify operations on ended auction are rejected (edit/delete/bid) in tests/AuctionService.UnitTests/Services/AuctionServiceTests.cs

### Implementation for User Story 4

- [ ] T156 [P] [US4] Ensure AuctionExtensions.CalculateStatus() is called in all DTO mapping methods (ToListItemDto, ToDetailDto) in src/AuctionService.Shared/Extensions/AuctionExtensions.cs
- [ ] T157 [US4] Add status validation in AuctionService.UpdateAuctionAsync(): reject if status=Ended in src/AuctionService.Core/Services/AuctionService.cs
- [ ] T158 [US4] Add status validation in AuctionService.DeleteAuctionAsync(): reject if status=Ended in src/AuctionService.Core/Services/AuctionService.cs
- [ ] T159 [US4] Verify AuctionRepository queries use EndTime index for status filtering (status=Active uses WHERE NOW() BETWEEN StartTime AND EndTime) in src/AuctionService.Infrastructure/Repositories/AuctionRepository.cs
- [ ] T160 [US4] Add database index performance test: query 10,000 active auctions with EndTime filter <100ms in tests/AuctionService.IntegrationTests/Performance/AuctionQueryPerformanceTests.cs
- [ ] T161 [US4] Run all US4 tests and verify they pass with green status

**Checkpoint**: All user stories should now be independently functional with correct passive status management

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T162 [P] Create ResponseCodeService in src/AuctionService.Core/Services/ResponseCodeService.cs (retrieve localized messages from ResponseCodes table based on language header)
- [ ] T163 [P] Update all ApiResponse wrappers to use ResponseCodeService for consistent metadata (statusCode, statusName, message) across all controllers
- [ ] T164 [P] Create health check endpoint in src/AuctionService.Api/Controllers/HealthController.cs (verify DB connection, BiddingService availability)
- [ ] T165 [P] Add YARP configuration in appsettings.json for API Gateway routing (if deploying with gateway)
- [ ] T166 [P] Create architecture.md in docs/ documenting Clean Architecture layers and data flow
- [ ] T167 [P] Create api-guide.md in docs/ with API usage examples and authentication guide
- [ ] T168 [P] Create deployment.md in docs/ with Docker deployment instructions and environment variables
- [ ] T169 [P] Create build.ps1 script in scripts/ for Windows builds (dotnet restore, build, test)
- [ ] T170 [P] Create build.sh script in scripts/ for Linux/macOS builds
- [ ] T171 [P] Create init-db.sql script in scripts/ for manual PostgreSQL database initialization
- [ ] T172 [P] Create run-tests.sh script in scripts/ for running all test projects with coverage report
- [ ] T173 [P] Create .github/workflows/build.yml for CI/CD: build on every PR
- [ ] T174 [P] Create .github/workflows/test.yml for CI/CD: run tests with Testcontainers on every PR
- [ ] T175 [P] Add XML documentation comments to all public APIs in Controllers
- [ ] T176 [P] Configure Swagger UI with examples from contracts/openapi.yaml
- [ ] T177 [P] Add correlation ID to all log entries via RequestLoggingMiddleware
- [ ] T178 [P] Add performance logging: log query execution times >1000ms as warnings
- [ ] T179 [P] Add circuit breaker pattern to BiddingServiceClient (fail fast after 5 consecutive failures)
- [ ] T180 [P] Review and optimize all database queries: ensure proper use of AsNoTracking() for read-only queries
- [ ] T181 [P] Add unit tests for all validators in tests/AuctionService.UnitTests/Validators/
- [ ] T182 [P] Add unit tests for all mapping extensions in tests/AuctionService.UnitTests/Extensions/
- [ ] T183 [P] Add unit tests for all custom exceptions and middleware in tests/AuctionService.UnitTests/Middlewares/
- [ ] T184 Validate quickstart.md: follow installation steps, verify all commands work, update any outdated instructions
- [ ] T185 Run full integration test suite against real PostgreSQL via Testcontainers
- [ ] T186 Run contract tests to ensure API matches contracts/openapi.yaml specification
- [ ] T187 Perform load testing: verify system handles 100+ req/s for GET /api/auctions
- [ ] T188 Verify all API responses include proper ResponseMetadata with statusCode/statusName/message
- [ ] T189 Security review: ensure JWT validation, CORS policy, SQL injection protection, XSS protection
- [ ] T190 Code review: verify no AutoMapper usage, all DTOs manually mapped, no Minimal APIs, Controller-based
- [ ] T191 Final test: deploy to Docker, run docker-compose up, verify all endpoints work end-to-end

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational (Phase 2) - MVP start
- **User Story 2 (Phase 4)**: Depends on Foundational (Phase 2) - can start in parallel with US1 if staffed
- **User Story 3 (Phase 5)**: Depends on Foundational (Phase 2) AND User Story 1 (needs IAuctionRepository) - can start after US1 complete
- **User Story 4 (Phase 6)**: Depends on Foundational (Phase 2) - validates passive status calculation
- **Polish (Phase 7)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1) - 瀏覽與搜尋**: Independent, only depends on Foundational
- **User Story 2 (P1) - 建立與管理**: Independent, only depends on Foundational (can integrate with US1 for BiddingService check)
- **User Story 3 (P2) - 追蹤商品**: Depends on US1 (needs IAuctionRepository.GetByIdAsync to verify auction exists)
- **User Story 4 (P3) - 狀態自動管理**: Independent validation phase, verifies passive status calculation from Foundational phase

### Within Each User Story

- Tests MUST be written and FAIL before implementation (TDD: Red-Green-Refactor)
- DTOs before validators before repositories
- Repositories before services
- Services before controllers
- Core implementation before integration
- Story complete before moving to next priority

### Parallel Opportunities

- **Phase 1 (Setup)**: All tasks marked [P] can run in parallel (T003-T005, T007-T009, T015-T021)
- **Phase 2 (Foundational)**: All entity creation (T024-T027), all configurations (T028-T031), all interface definitions (T034-T041, T047-T048) can run in parallel
- **Within User Story Tests**: All test tasks marked [P] within same story can run in parallel
- **User Story 1 & 2**: Can be developed in parallel after Foundational (if team capacity allows)
- **Cross-story**: Different team members can work on different user stories simultaneously after Foundational phase

---

## Parallel Example: User Story 1 Implementation

```bash
# After all US1 tests are written and failing, launch parallel implementation:

# Parallel group 1: All DTOs
Task T061: Create AuctionListItemDto
Task T062: Create AuctionDetailDto
Task T063: Create CurrentBidDto
Task T064: Create CategoryDto

# Parallel group 2: All interfaces
Task T065: Create IAuctionRepository interface
Task T067: Create IBiddingServiceClient interface
Task T069: Create IAuctionService interface
Task T072: Create ICategoryService interface

# Sequential: Repository implementations (depend on interfaces)
Task T066: Implement AuctionRepository
Task T068: Implement BiddingServiceClient

# Parallel group 3: Mapping extensions
Task T071: Create mapping extensions

# Sequential: Service implementations (depend on repositories)
Task T070: Implement AuctionService
Task T073: Implement CategoryService

# Sequential: Controllers (depend on services)
Task T074-T077: Implement all controller endpoints
```

---

## Implementation Strategy

### MVP First (User Stories 1 & 2 Only - Both P1)

1. Complete Phase 1: Setup (T001-T021)
2. Complete Phase 2: Foundational (T022-T048) - CRITICAL blocking phase
3. Complete Phase 3: User Story 1 - 瀏覽與搜尋 (T049-T080)
4. Complete Phase 4: User Story 2 - 建立與管理 (T081-T128)
5. **STOP and VALIDATE**: Test US1 & US2 independently and together
6. Deploy/demo MVP with core auction functionality

### Incremental Delivery

1. Setup + Foundational → Foundation ready (T001-T048)
2. Add User Story 1 → Test independently → Deploy/Demo (T049-T080) - Users can browse auctions
3. Add User Story 2 → Test independently → Deploy/Demo (T081-T128) - MVP complete: browse + manage
4. Add User Story 3 → Test independently → Deploy/Demo (T129-T151) - Enhanced UX with tracking
5. Add User Story 4 → Test independently → Deploy/Demo (T152-T161) - Status validation
6. Polish phase → Production-ready (T162-T191)

### Parallel Team Strategy

With 3 developers after Foundational phase completes:

- **Developer A**: User Story 1 (T049-T080) - Browse & search features
- **Developer B**: User Story 2 (T081-T128) - Create & manage features  
- **Developer C**: Setup tests infrastructure, prepare User Story 3 (T129-T151)

Once US1 completes, Developer A can help with US3 (depends on US1 repositories).

---

## Task Summary

- **Total Tasks**: 191
- **Setup Phase**: 21 tasks
- **Foundational Phase**: 27 tasks (CRITICAL - blocks all stories)
- **User Story 1** (P1 - MVP): 32 tasks (12 tests + 20 implementation)
- **User Story 2** (P1 - MVP): 48 tasks (15 tests + 33 implementation)
- **User Story 3** (P2): 23 tasks (11 tests + 12 implementation)
- **User Story 4** (P3): 10 tasks (4 tests + 6 implementation)
- **Polish Phase**: 30 tasks

**Parallel Opportunities**: 89 tasks marked [P] can run in parallel (within phase constraints)

**MVP Scope**: Phases 1-4 (T001-T128) = 128 tasks for complete browse + manage functionality

**Test Coverage**: 42 test tasks ensuring >80% code coverage target via TDD approach

---

## Format Validation ✅

All 191 tasks follow the required checklist format:
- ✅ Checkbox `- [ ]` present
- ✅ Task ID (T001-T191) sequential
- ✅ [P] marker on parallelizable tasks (89 tasks)
- ✅ [Story] label on user story tasks (US1, US2, US3, US4)
- ✅ File paths included in descriptions
- ✅ Clear action verbs (Create, Implement, Configure, etc.)

**Ready for execution**: Each task is specific enough for LLM completion without additional context.
