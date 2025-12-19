# Tasks: 競標服務 (Bidding Service)

**Input**: Design documents from `/specs/003-bidding-service/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/openapi.yaml

**Tests**: Tests are NOT explicitly requested in the specification but implied through TDD requirements. Unit tests and integration tests will be included following憲法原則 II.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

All paths are relative to `BiddingService/` root directory (single-folder self-contained structure):
- Source code: `src/BiddingService.{Layer}/`
- Tests: `tests/BiddingService.{TestType}/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure per plan.md

- [x] T001 Create BiddingService.sln solution file and project structure (Api/Core/Infrastructure/Shared)
- [x] T002 Initialize BiddingService.Api project with ASP.NET Core 10 Web API (Controller-based)
- [x] T003 [P] Initialize BiddingService.Core project with .NET 10 class library
- [x] T004 [P] Initialize BiddingService.Infrastructure project with .NET 10 class library
- [x] T005 [P] Initialize BiddingService.Shared project with .NET 10 class library
- [x] T006 [P] Create docker-compose.yml with PostgreSQL 14 and Redis 7 containers
- [x] T007 [P] Create Dockerfile for multi-stage production build
- [x] T008 [P] Configure .gitignore, .editorconfig, global.json (SDK 10.0)
- [x] T009 Add NuGet packages to BiddingService.Api (Serilog, Prometheus.NET, Swashbuckle)
- [x] T010 [P] Add NuGet packages to BiddingService.Infrastructure (EF Core 10, Npgsql, StackExchange.Redis 2.7+, IdGen, Azure.Security.KeyVault.Secrets)
- [x] T011 [P] Create BiddingService.UnitTests project with xUnit, Moq, FluentAssertions
- [x] T012 [P] Create BiddingService.IntegrationTests project with Testcontainers
- [x] T013 Configure appsettings.json and appsettings.Development.json in BiddingService.Api

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Infrastructure Setup

- [x] T014 Create BiddingDbContext in src/BiddingService.Infrastructure/Data/BiddingDbContext.cs
- [x] T015 Implement SnowflakeIdGenerator using IdGen in src/BiddingService.Infrastructure/IdGeneration/SnowflakeIdGenerator.cs
- [x] T016 [P] Implement EncryptionService (AES-256-GCM) in src/BiddingService.Infrastructure/Encryption/EncryptionService.cs
- [x] T017 [P] Implement EncryptionValueConverter for EF Core in src/BiddingService.Infrastructure/Encryption/EncryptionValueConverter.cs
- [x] T018 [P] Create RedisConnection manager in src/BiddingService.Infrastructure/Redis/RedisConnection.cs
- [x] T019 Implement CorrelationIdMiddleware in src/BiddingService.Api/Middlewares/CorrelationIdMiddleware.cs
- [x] T020 [P] Implement ExceptionHandlingMiddleware with standardized ErrorResponse DTO (per spec.md FR-014) in src/BiddingService.Api/Middlewares/ExceptionHandlingMiddleware.cs
  - Map exceptions to HTTP status codes (400/401/403/404/409/500/503)
  - Return consistent ErrorResponse format with error code and message
  - Log errors with correlation ID
- [x] T021 [P] Implement RequestLoggingMiddleware with Serilog in src/BiddingService.Api/Middlewares/RequestLoggingMiddleware.cs

### Core Entities and Value Objects

- [x] T022 Create Bid entity in src/BiddingService.Core/Entities/Bid.cs (with BidId, AuctionId, BidderId, Amount, BidAt, CreatedAt, SyncedFromRedis, BidderIdHash)
- [x] T023 [P] Create BidAmount value object in src/BiddingService.Core/ValueObjects/BidAmount.cs
- [x] T024 Configure BidConfiguration (EF Core) with encryption converters in src/BiddingService.Infrastructure/Data/Configurations/BidConfiguration.cs
- [x] T025 Create EF Core migration InitialCreate in src/BiddingService.Infrastructure/Migrations/

### Repository Interfaces and Base Classes

- [x] T026 [P] Define IRepository<T> generic interface in src/BiddingService.Core/Interfaces/IRepository.cs
- [x] T027 [P] Define IBidRepository interface in src/BiddingService.Core/Interfaces/IBidRepository.cs
- [x] T028 [P] Define IRedisRepository interface in src/BiddingService.Core/Interfaces/IRedisRepository.cs
- [x] T029 Implement GenericRepository<T> base class in src/BiddingService.Infrastructure/Repositories/GenericRepository.cs
- [x] T030 Implement BidRepository (PostgreSQL) in src/BiddingService.Infrastructure/Repositories/BidRepository.cs

### Redis Lua Script and Repository

- [x] T031 Create place-bid.lua script in src/BiddingService.Infrastructure/Redis/Scripts/place-bid.lua
- [x] T032 Implement RedisRepository with Lua script execution in src/BiddingService.Infrastructure/Repositories/RedisRepository.cs

### Cross-Service Communication

- [x] T033 [P] Define IAuctionServiceClient interface in src/BiddingService.Core/Interfaces/IAuctionServiceClient.cs
- [x] T034 Implement AuctionServiceClient with HttpClient and Polly retry in src/BiddingService.Infrastructure/HttpClients/AuctionServiceClient.cs
- [x] T035 [P] Implement CorrelationIdDelegatingHandler for HttpClient in src/BiddingService.Infrastructure/HttpClients/CorrelationIdDelegatingHandler.cs

### Background Services

- [x] T036 [P] Implement RedisSyncWorker (IHostedService) in src/BiddingService.Infrastructure/BackgroundServices/RedisSyncWorker.cs
- [x] T037 [P] Implement RedisHealthCheckService in src/BiddingService.Infrastructure/BackgroundServices/RedisHealthCheckService.cs

### Shared Utilities

- [x] T038 [P] Create ErrorCodes constants in src/BiddingService.Shared/Constants/ErrorCodes.cs
- [x] T039 [P] Create HashHelper (SHA-256) in src/BiddingService.Shared/Helpers/HashHelper.cs
- [x] T040 [P] Create ServiceCollectionExtensions for DI in src/BiddingService.Shared/Extensions/ServiceCollectionExtensions.cs
- [x] T041 [P] Create BidExtensions for POCO mapping in src/BiddingService.Shared/Extensions/BidExtensions.cs

### API Infrastructure

- [x] T042 Configure Program.cs with DI, middleware pipeline, Serilog, Prometheus in src/BiddingService.Api/Program.cs
- [x] T043 [P] Create HealthController in src/BiddingService.Api/Controllers/HealthController.cs
- [x] T044 Configure Swagger/OpenAPI generation in Program.cs

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - 提交競標出價 (Priority: P1) 🎯 MVP

**Goal**: 實現出價提交 API，包含金額驗證、擁有者檢查、併發控制，確保 < 100ms (P95) 回應時間

**Independent Test**: 呼叫 `POST /api/bids` API 並驗證：(1) 出價成功回傳 201, (2) 金額不足回傳 400, (3) 對自己商品出價回傳 403, (4) 商品結束回傳 409, (5) 併發場景只有一筆成功, (6) 回應時間 < 100ms

### Tests for User Story 1

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [x] T045 [P] [US1] Unit test for BiddingService.CreateBidAsync in tests/BiddingService.UnitTests/Services/BiddingServiceTests.cs
- [x] T046 [P] [US1] Unit test for BidValidator in tests/BiddingService.UnitTests/Validators/BidValidatorTests.cs
- [x] T047 [P] [US1] Unit test for SnowflakeIdGenerator in tests/BiddingService.UnitTests/Infrastructure/SnowflakeIdGeneratorTests.cs
- [x] T048 [P] [US1] Unit test for EncryptionService in tests/BiddingService.UnitTests/Infrastructure/EncryptionServiceTests.cs
- [ ] T049 [P] [US1] Integration test for BidsController.CreateBid in tests/BiddingService.IntegrationTests/Controllers/BidsControllerIntegrationTests.cs (with Testcontainers)
- [ ] T050 [P] [US1] Integration test for RedisRepository.PlaceBidAsync (Lua script) in tests/BiddingService.IntegrationTests/Repositories/RedisRepositoryTests.cs
- [ ] T051 [P] [US1] Load test for concurrent bidding (1000 requests) in tests/BiddingService.LoadTests/ConcurrentBiddingTests.cs

### Implementation for User Story 1

- [x] T052 [P] [US1] Define IBiddingService interface in src/BiddingService.Core/Interfaces/IBiddingService.cs
- [x] T053 [US1] Implement BiddingService.CreateBidAsync with business rules in src/BiddingService.Core/Services/BiddingService.cs
- [x] T054 [P] [US1] Create CreateBidRequest DTO in src/BiddingService.Core/DTOs/Requests/CreateBidRequest.cs
- [x] T055 [P] [US1] Create BidResponse DTO in src/BiddingService.Core/DTOs/Responses/BidResponse.cs
- [x] T056 [P] [US1] Create BidValidator using FluentValidation in src/BiddingService.Core/Validators/BidValidator.cs
- [x] T057 [P] [US1] Create custom exceptions (BidTooLowException, AuctionNotFoundException, UnauthorizedException) in src/BiddingService.Core/Exceptions/
- [x] T058 [US1] Implement BidsController.CreateBid (POST /api/bids) in src/BiddingService.Api/Controllers/BidsController.cs
- [x] T059 [US1] Add ValidationFilter for model validation in src/BiddingService.Api/Filters/ValidationFilter.cs
- [x] T060 [US1] Add logging for bid creation operations in BiddingService and BidsController
- [x] T061 [US1] Verify Redis Lua script handles concurrent bids correctly (金額檢查 + ZADD + HSET + SADD 原子操作)
- [x] T062 [US1] Integrate AuctionServiceClient to fetch auction info and validate ownership

**Checkpoint**: User Story 1 fully functional - can create bids with all validations, < 100ms response time verified in load tests

---

## Phase 4: User Story 2 - 查詢出價歷史 (Priority: P1)

**Goal**: 實現商品出價歷史查詢 API，支援分頁，優先從 Redis 讀取，回應時間 < 200ms (P95)

**Independent Test**: 呼叫 `GET /api/auctions/{id}/bids` API 並驗證：(1) 回傳出價依時間倒序, (2) 分頁正確 (預設 20 筆), (3) 回應時間 < 200ms, (4) 無出價時回傳空陣列

### Tests for User Story 2

- [x] T063 [P] [US2] Unit test for BiddingService.GetBidHistoryAsync in tests/BiddingService.UnitTests/Services/BiddingServiceTests.cs
- [ ] T064 [P] [US2] Integration test for BidsController.GetBidHistory in tests/BiddingService.IntegrationTests/Controllers/BidsControllerIntegrationTests.cs
- [ ] T065 [P] [US2] Integration test for BidRepository.GetBidsByAuctionAsync (PostgreSQL fallback) in tests/BiddingService.IntegrationTests/Repositories/BidRepositoryTests.cs

### Implementation for User Story 2

- [x] T066 [P] [US2] Create BidHistoryResponse DTO in src/BiddingService.Core/DTOs/Responses/BidHistoryResponse.cs
- [x] T067 [P] [US2] Create PaginationMetadata DTO in src/BiddingService.Core/DTOs/Responses/PaginationMetadata.cs
- [x] T068 [US2] Implement BiddingService.GetBidHistoryAsync with Redis priority fallback in src/BiddingService.Core/Services/BiddingService.cs
- [x] T069 [US2] Add RedisRepository.GetBidsByAuctionAsync (ZREVRANGE) in src/BiddingService.Infrastructure/Repositories/RedisRepository.cs
- [x] T070 [US2] Add BidRepository.GetBidsByAuctionAsync with pagination in src/BiddingService.Infrastructure/Repositories/BidRepository.cs
- [x] T071 [US2] Implement BidsController.GetBidHistory (GET /api/auctions/{auctionId}/bids) in src/BiddingService.Api/Controllers/BidsController.cs
- [x] T072 [US2] Add logging for bid history query operations (Redis hit/miss, query time)

**Checkpoint**: User Story 2 fully functional - can query bid history with pagination, Redis caching works, < 200ms response time

---

## Phase 5: User Story 3 - 查詢使用者出價記錄 (Priority: P2)

**Goal**: 實現「我的出價」查詢 API，包含商品資訊、最高出價狀態、篩選功能，回應時間 < 200ms (P95)

**Independent Test**: 呼叫 `GET /api/me/bids` API 並驗證：(1) 回傳使用者所有出價含商品標題, (2) 正確標示 isHighestBid, (3) 篩選條件 (active/won) 運作正常

### Tests for User Story 3

- [x] T073 [P] [US3] Unit test for BiddingService.GetMyBidsAsync in tests/BiddingService.UnitTests/Services/BiddingServiceTests.cs
- [x] T074 [P] [US3] Integration test for BidsController.GetMyBids in tests/BiddingService.IntegrationTests/Controllers/BidsControllerIntegrationTests.cs
- [x] T075 [P] [US3] Unit test for AuctionServiceClient.GetAuctionsBatchAsync in tests/BiddingService.UnitTests/HttpClients/AuctionServiceClientTests.cs

### Implementation for User Story 3

- [x] T076 [P] [US3] Create MyBidResponse DTO in src/BiddingService.Core/DTOs/Responses/MyBidResponse.cs
- [x] T077 [US3] Implement BiddingService.GetMyBidsAsync with BidderIdHash query in src/BiddingService.Core/Services/BiddingService.cs
- [x] T078 [US3] Add BidRepository.GetBidsByBidderIdHashAsync with pagination in src/BiddingService.Infrastructure/Repositories/BidRepository.cs
- [x] T079 [US3] Implement AuctionServiceClient.GetAuctionsBatchAsync for batch auction info in src/BiddingService.Infrastructure/HttpClients/AuctionServiceClient.cs
- [x] T080 [US3] Implement BidsController.GetMyBids (GET /api/me/bids) in src/BiddingService.Api/Controllers/BidsController.cs
- [x] T081 [US3] Add caching for auction info (MemoryCache, 5 minutes TTL) in AuctionServiceClient
- [x] T082 [US3] Add logging for my bids query operations

**Checkpoint**: User Story 3 fully functional - users can query their bid history with auction details, filtering works

---

## Phase 6: User Story 4 - 查詢最高出價 (Priority: P2)

**Goal**: 實現快速最高出價查詢 API，優先從 Redis Hash 讀取，回應時間 < 50ms (P95)

**Independent Test**: 呼叫 `GET /api/auctions/{id}/highest-bid` API 並驗證：(1) 回傳最高出價正確, (2) Redis 優先讀取 < 50ms, (3) 無出價時回傳 404

### Tests for User Story 4

- [x] T083 [P] [US4] Unit test for BiddingService.GetHighestBidAsync in tests/BiddingService.UnitTests/Services/BiddingServiceTests.cs
- [x] T084 [P] [US4] Integration test for BidsController.GetHighestBid in tests/BiddingService.IntegrationTests/Controllers/BidsControllerIntegrationTests.cs
- [ ] T085 [P] [US4] Integration test for RedisRepository.GetHighestBidAsync in tests/BiddingService.IntegrationTests/Repositories/RedisRepositoryTests.cs

### Implementation for User Story 4

- [x] T086 [P] [US4] Create HighestBidResponse DTO in src/BiddingService.Core/DTOs/Responses/HighestBidResponse.cs
- [x] T087 [US4] Implement BiddingService.GetHighestBidAsync with Redis priority in src/BiddingService.Core/Services/BiddingService.cs
- [x] T088 [US4] Add RedisRepository.GetHighestBidAsync (HGETALL) in src/BiddingService.Infrastructure/Repositories/RedisRepository.cs
- [x] T089 [US4] Add BidRepository.GetHighestBidAsync (ORDER BY amount DESC LIMIT 1) in src/BiddingService.Infrastructure/Repositories/BidRepository.cs
- [x] T090 [US4] Implement BidsController.GetHighestBid (GET /api/auctions/{auctionId}/highest-bid) in src/BiddingService.Api/Controllers/BidsController.cs
- [x] T091 [US4] Add logging for highest bid query (Redis hit/miss, query time < 50ms verification)

**Checkpoint**: User Story 4 fully functional - highest bid query works with sub-50ms Redis cache performance

---

## Phase 7: User Story 5 - 競標狀態分析 (Priority: P3)

**Goal**: 實現管理員競標統計 API (總出價次數、不重複出價者、價格成長率)

**Independent Test**: 呼叫 `GET /api/auctions/{id}/stats` API 並驗證統計數據正確計算

### Tests for User Story 5

- [x] T092 [P] [US5] Unit test for BiddingService.GetAuctionStatsAsync in tests/BiddingService.UnitTests/Services/BiddingServiceTests.cs
- [ ] T093 [P] [US5] Integration test for BidsController.GetAuctionStats in tests/BiddingService.IntegrationTests/Controllers/BidsControllerIntegrationTests.cs

### Implementation for User Story 5

- [x] T094 [P] [US5] Create AuctionStatsResponse DTO in src/BiddingService.Core/DTOs/Responses/AuctionStatsResponse.cs
- [x] T095 [US5] Implement BiddingService.GetAuctionStatsAsync in src/BiddingService.Core/Services/BiddingService.cs
- [x] T096 [US5] Add BidRepository.GetAuctionStatsAsync (aggregate queries) in src/BiddingService.Infrastructure/Repositories/BidRepository.cs
- [x] T097 [US5] Implement BidsController.GetAuctionStats (GET /api/auctions/{auctionId}/stats) in src/BiddingService.Api/Controllers/BidsController.cs
- [x] T098 [US5] Add logging for stats query operations

**Checkpoint**: All user stories (US1-US5) are independently functional

---

## Phase 8: Background Worker & Dead Letter Queue

**Purpose**: Ensure Redis-to-PostgreSQL sync reliability

- [x] T099 Implement RedisSyncWorker exponential backoff retry (1s, 2s, 4s) in src/BiddingService.Infrastructure/BackgroundServices/RedisSyncWorker.cs
- [x] T100 [P] Add RedisRepository.AddToDeadLetterQueueAsync in src/BiddingService.Infrastructure/Repositories/RedisRepository.cs
- [x] T101 [P] Add RedisRepository.GetDeadLetterBidsAsync in src/BiddingService.Infrastructure/Repositories/RedisRepository.cs
- [x] T102 Add logging for sync operations (success count, dead letter queue size, errors) in RedisSyncWorker
- [x] T103 [P] Integration test for RedisSyncWorker in tests/BiddingService.IntegrationTests/BackgroundServices/RedisSyncWorkerTests.cs
- [x] T104 [P] Integration test for RedisHealthCheckService (降級機制) in tests/BiddingService.IntegrationTests/BackgroundServices/RedisHealthCheckServiceTests.cs

---

## Phase 9: Additional API Endpoints

**Purpose**: Support endpoints for external services

- [x] T105 [P] Implement BidsController.GetBidById (GET /api/bids/{bidId}) in src/BiddingService.Api/Controllers/BidsController.cs
- [x] T106 [P] Unit test for GetBidById in tests/BiddingService.UnitTests/Controllers/BidsControllerTests.cs
- [x] T107 Add BidRepository.GetByIdAsync in src/BiddingService.Infrastructure/Repositories/BidRepository.cs
- [x] T108 [P] Contract test for AuctionServiceClient endpoints (GET /api/auctions/{id}/basic, POST /api/auctions/batch) in tests/BiddingService.IntegrationTests/Contracts/AuctionServiceContractTests.cs

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [x] T109 [P] Update README.md with quickstart instructions in BiddingService/README.md
- [x] T110 [P] Create architecture.md documentation in BiddingService/docs/architecture.md
- [x] T111 [P] Create api-guide.md with usage examples in BiddingService/docs/api-guide.md
- [x] T112 Add Prometheus metrics (bid_requests_total, bid_latency_seconds, redis_fallback_active) in Program.cs
- [x] T113 [P] Configure APM integration (Application Insights or Elastic APM) per plan.md monitoring strategy in src/BiddingService.Api/Program.cs
  - Install NuGet package (Microsoft.ApplicationInsights.AspNetCore or Elastic.Apm.NetCoreAll)
  - Configure connection string in appsettings.json
  - Verify telemetry data flows to APM platform
  - Document APM setup in docs/deployment.md
- [x] T114 [P] Create GitHub Actions workflow for build in .github/workflows/build.yml
- [x] T115 [P] Create GitHub Actions workflow for tests in .github/workflows/test.yml
- [x] T116 [P] Create GitHub Actions workflow step for EF Core database update (dotnet ef database update) in .github/workflows/deploy.yml
- [x] T117 Code cleanup and refactoring (remove unused usings, apply code style)
- [x] T118 Security hardening (validate all inputs, sanitize error messages)
- [x] T119 Performance optimization (review connection pooling, Redis pipeline operations)
- [x] T120 Run quickstart.md validation (follow all steps, verify working)
- [x] T121 [P] Add OpenTelemetry support (optional future enhancement) in Program.cs
- [x] T122 Final integration test run covering all 5 user stories end-to-end

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-7)**: All depend on Foundational phase completion
  - User stories CAN proceed in parallel (if staffed)
  - Or sequentially in priority order: US1 (P1) → US2 (P1) → US3 (P2) → US4 (P2) → US5 (P3)
- **Background Worker (Phase 8)**: Depends on Foundational (can run in parallel with user stories)
- **Additional Endpoints (Phase 9)**: Depends on Foundational + US1 data model
- **Polish (Phase 10)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P1)**: Can start after Foundational (Phase 2) - Uses Bid entity from US1 but independently testable
- **User Story 3 (P2)**: Can start after Foundational (Phase 2) - Uses Bid entity, integrates with AuctionServiceClient
- **User Story 4 (P2)**: Can start after Foundational (Phase 2) - Uses Bid entity and Redis structures from US1
- **User Story 5 (P3)**: Can start after Foundational (Phase 2) - Aggregates Bid data

### Within Each User Story

- **Tests MUST be written and FAIL before implementation**
- Models (DTOs, Entities) before services
- Services before controllers
- Core implementation before cross-service integration
- Story complete and independently tested before moving to next priority

### Parallel Opportunities

- **Phase 1**: All [P] tasks (T003-T005, T006-T008, T010-T012) can run in parallel
- **Phase 2**: 
  - Infrastructure tasks (T016-T018, T020-T021) can run in parallel
  - Repository interfaces (T026-T028) can run in parallel
  - Cross-service clients (T033, T035) can run in parallel
  - Background services (T036-T037) can run in parallel
  - Shared utilities (T038-T041) can run in parallel
- **User Story Tests**: All [P] test tasks within each story can run in parallel
- **User Story Models**: All [P] DTO/entity tasks can run in parallel
- **Across User Stories**: Once Phase 2 completes, all 5 user stories can be worked on in parallel by different team members

---

## Parallel Example: User Story 1

```bash
# Launch all tests for User Story 1 together (write tests first, ensure they fail):
Task T045: "Unit test for BiddingService.CreateBidAsync"
Task T046: "Unit test for BidValidator"
Task T047: "Unit test for SnowflakeIdGenerator"
Task T048: "Unit test for EncryptionService"
Task T049: "Integration test for BidsController.CreateBid"
Task T050: "Integration test for RedisRepository.PlaceBidAsync"
Task T051: "Load test for concurrent bidding"

# Launch all DTOs for User Story 1 together (after tests fail):
Task T054: "Create CreateBidRequest DTO"
Task T055: "Create BidResponse DTO"

# Launch all exceptions for User Story 1 together:
Task T057: "Create custom exceptions (BidTooLowException, AuctionNotFoundException, UnauthorizedException)"
```

---

## Implementation Strategy

### MVP First (User Story 1 + User Story 2 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1 (出價提交)
4. Complete Phase 4: User Story 2 (查詢出價歷史)
5. **STOP and VALIDATE**: Test US1 + US2 independently together
6. Deploy/demo if ready (核心競標功能完整)

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready (T001-T044)
2. Add User Story 1 → Test independently → Deploy/Demo (出價功能 MVP!)
3. Add User Story 2 → Test independently → Deploy/Demo (含歷史查詢)
4. Add User Story 3 → Test independently → Deploy/Demo (含使用者記錄)
5. Add User Story 4 → Test independently → Deploy/Demo (最高出價優化)
6. Add User Story 5 → Test independently → Deploy/Demo (統計功能)
7. Each story adds value without breaking previous stories

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together (T001-T044)
2. Once Foundational is done:
   - Developer A: User Story 1 (T045-T062)
   - Developer B: User Story 2 (T063-T072)
   - Developer C: Background Worker (T099-T104)
3. After MVP (US1 + US2):
   - Developer A: User Story 3 (T073-T082)
   - Developer B: User Story 4 (T083-T091)
   - Developer C: User Story 5 (T092-T098)
4. All: Polish phase together (T109-T122, includes new APM integration & CI/CD tasks)

---

## Notes

- **[P] tasks** = different files, no dependencies, can run in parallel
- **[Story] label** maps task to specific user story for traceability (US1-US5)
- Each user story should be independently completable and testable
- **TDD Workflow**: Write tests → Ensure they FAIL → Implement → Tests pass → Refactor
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- **Tests are included** per憲法原則 II (TDD requirement) and SC-005 (> 80% coverage)
- **PostgreSQL 14+** and **Redis 7** via Docker Compose (docker-compose.yml)
- **Snowflake ID** (64-bit Long) via IdGen package
- **AES-256-GCM encryption** for Amount and BidderId fields
- **Correlation ID tracking** via X-Correlation-ID header
- **Redis Write-Behind Cache** architecture with Lua Script atomic operations
- **Background Worker** syncs Redis → PostgreSQL every 1 second
- **降級機制**: Redis failure → PostgreSQL direct write
- **Response time goals**: Bid < 100ms, History < 200ms, Highest < 50ms
- **All paths relative to `BiddingService/` root directory**
