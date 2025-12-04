# Tasks: API Gateway

**Input**: Design documents from `/specs/004-api-gateway/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: This project follows TDD approach as specified in SC-006 (單元測試覆蓋率 > 80%)

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

Based on plan.md structure:
- **Source**: `ApiGateway/src/ApiGateway.Api/`, `ApiGateway/src/ApiGateway.Core/`, `ApiGateway/src/ApiGateway.Infrastructure/`, `ApiGateway/src/ApiGateway.Shared/`
- **Tests**: `ApiGateway/tests/ApiGateway.UnitTests/`, `ApiGateway/tests/ApiGateway.IntegrationTests/`, `ApiGateway/tests/ApiGateway.LoadTests/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure per quickstart.md

- [ ] T001 Create ApiGateway root directory structure
- [ ] T002 Initialize .NET 10 solution with ApiGateway.sln
- [ ] T003 [P] Create ApiGateway.Api project in src/ApiGateway.Api/ (.NET 10, no minimal APIs)
- [ ] T004 [P] Create ApiGateway.Core project in src/ApiGateway.Core/
- [ ] T005 [P] Create ApiGateway.Infrastructure project in src/ApiGateway.Infrastructure/
- [ ] T006 [P] Create ApiGateway.Shared project in src/ApiGateway.Shared/
- [ ] T007 [P] Create ApiGateway.UnitTests project in tests/ApiGateway.UnitTests/
- [ ] T008 [P] Create ApiGateway.IntegrationTests project in tests/ApiGateway.IntegrationTests/
- [ ] T009 [P] Create ApiGateway.LoadTests project in tests/ApiGateway.LoadTests/
- [ ] T010 Configure project references (Api → Core/Infrastructure/Shared, Infrastructure → Core)
- [ ] T011 Install YARP NuGet package (Yarp.ReverseProxy 2.1.0) in ApiGateway.Api
- [ ] T012 [P] Install JWT authentication package (Microsoft.AspNetCore.Authentication.JwtBearer 10.0.0) in ApiGateway.Api
- [ ] T013 [P] Install Serilog packages in ApiGateway.Api (Serilog.AspNetCore, Serilog.Sinks.Console, Serilog.Sinks.File)
- [ ] T014 [P] Install Redis package (StackExchange.Redis 2.7.0) in ApiGateway.Infrastructure
- [ ] T015 [P] Install Polly packages (Polly 8.2.0, Polly.Extensions.Http 3.0.0) in ApiGateway.Infrastructure
- [ ] T016 [P] Install test packages (Moq, FluentAssertions) in ApiGateway.UnitTests
- [ ] T017 [P] Install integration test packages (Microsoft.AspNetCore.Mvc.Testing 10.0.0, Testcontainers.Redis 3.7.0) in ApiGateway.IntegrationTests
- [ ] T018 [P] Install NBomber 5.0.0 in ApiGateway.LoadTests
- [ ] T019 [P] Create global.json to lock .NET 10 SDK version
- [ ] T020 [P] Create .editorconfig for code style consistency
- [ ] T021 [P] Create Dockerfile for production multi-stage build
- [ ] T022 [P] Create docker-compose.yml for local development (Redis + Backend Mocks)
- [ ] T023 [P] Create docker-compose.override.yml for local environment overrides
- [ ] T024 [P] Create .dockerignore file
- [ ] T025 [P] Create README.md based on quickstart.md template
- [ ] T026 [P] Create docs/architecture.md placeholder
- [ ] T027 [P] Create docs/api-guide.md placeholder
- [ ] T028 [P] Create docs/deployment.md placeholder
- [ ] T029 [P] Create scripts/build.sh and scripts/build.ps1
- [ ] T030 [P] Create scripts/run-tests.sh
- [ ] T031 [P] Create scripts/deploy.sh

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [ ] T032 Create error code constants in src/ApiGateway.Shared/Constants/ErrorCodes.cs (UNAUTHORIZED, TOKEN_EXPIRED, INVALID_TOKEN, MALFORMED_TOKEN, NOT_FOUND, PAYLOAD_TOO_LARGE, RATE_LIMIT_EXCEEDED, INTERNAL_SERVER_ERROR, SERVICE_UNAVAILABLE, GATEWAY_TIMEOUT)
- [ ] T033 [P] Create ErrorResponse DTO in src/ApiGateway.Core/DTOs/Responses/ErrorResponse.cs per contracts/error-responses.json
- [ ] T034 [P] Create HealthCheckResponse DTO in src/ApiGateway.Core/DTOs/Responses/HealthCheckResponse.cs
- [ ] T035 [P] Create HttpContextExtensions in src/ApiGateway.Shared/Extensions/HttpContextExtensions.cs (GetClientIp method for X-Forwarded-For handling)
- [ ] T036 [P] Create ServiceCollectionExtensions in src/ApiGateway.Shared/Extensions/ServiceCollectionExtensions.cs for DI registration
- [ ] T037 Configure Serilog structured logging in src/ApiGateway.Api/Program.cs (JSON format, console + file sinks)
- [ ] T038 Create CorrelationIdMiddleware in src/ApiGateway.Api/Middlewares/CorrelationIdMiddleware.cs (generate X-Gateway-Request-Id)
- [ ] T039 Create RequestLoggingMiddleware in src/ApiGateway.Api/Middlewares/RequestLoggingMiddleware.cs (log all requests with path, method, status, latency, userId, IP per FR-012)
- [ ] T040 Create ExceptionHandlingMiddleware in src/ApiGateway.Api/Middlewares/ExceptionHandlingMiddleware.cs (unified error handling, convert exceptions to ErrorResponse format)
- [ ] T041 Configure middleware pipeline in src/ApiGateway.Api/Program.cs (CorrelationId → RequestLogging → ExceptionHandling order)
- [ ] T042 Create base appsettings.json in src/ApiGateway.Api/ with YARP routes per contracts/routes.yaml
- [ ] T043 [P] Create appsettings.Development.json with development-specific settings
- [ ] T044 Create RedisConnection in src/ApiGateway.Infrastructure/Redis/RedisConnection.cs (ConnectionMultiplexer singleton pattern)
- [ ] T045 Configure Redis connection in src/ApiGateway.Api/Program.cs DI container
- [ ] T046 Create IServiceDiscovery interface in src/ApiGateway.Core/Interfaces/IServiceDiscovery.cs (abstraction for future Consul migration)
- [ ] T047 Create StaticServiceDiscovery in src/ApiGateway.Infrastructure/HttpClients/StaticServiceDiscovery.cs (reads from appsettings.json)
- [ ] T048 Configure YARP reverse proxy in src/ApiGateway.Api/Program.cs (load from appsettings.json ReverseProxy section)
- [ ] T049 Configure CORS in src/ApiGateway.Api/Program.cs (AllowedOrigins from appsettings.json per FR-007)
- [ ] T050 Configure request size limit (10MB) in src/ApiGateway.Api/Program.cs per FR-013
- [ ] T051 Configure HTTPS redirection in src/ApiGateway.Api/Program.cs per FR-008

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - 請求路由 (Priority: P1) 🎯 MVP

**Goal**: 統一 API 入口，將客戶端請求路由到正確的後端微服務 (Member/Auction/Bidding Service)

**Independent Test**: 可透過向不同路徑發送請求 (如 `/api/members/*`, `/api/auctions/*`, `/api/bids/*`) 並驗證請求是否正確轉發到對應服務，即可完整測試此功能。

### Tests for User Story 1

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [ ] T052 [P] [US1] Create YarpRoutingTests in tests/ApiGateway.IntegrationTests/Routing/YarpRoutingTests.cs (test all 6 routes from contracts/routes.yaml)
- [ ] T053 [P] [US1] Test GET /api/members/* routes to Member Service (AC-1)
- [ ] T054 [P] [US1] Test GET /api/auctions/* routes to Auction Service (AC-2)
- [ ] T055 [P] [US1] Test GET /api/bids/* routes to Bidding Service (AC-3)
- [ ] T056 [P] [US1] Test GET /api/me/bids routes to Bidding Service with higher priority (AC-4)
- [ ] T057 [P] [US1] Test GET /api/me/follows routes to Auction Service with higher priority (AC-5)
- [ ] T058 [P] [US1] Test GET /api/me routes to Member Service (AC-6)
- [ ] T059 [P] [US1] Test PUT /api/me routes to Member Service (AC-6)
- [ ] T060 [P] [US1] Create routing latency benchmark test in tests/ApiGateway.LoadTests/GatewayLoadTests.cs (verify < 10ms P95 per AC-7)

### Implementation for User Story 1

- [ ] T061 [US1] Verify YARP routes configuration in appsettings.json matches contracts/routes.yaml (6 routes with correct Order priority)
- [ ] T062 [US1] Verify YARP clusters configuration in appsettings.json (member-cluster, auction-cluster, bidding-cluster with health checks)
- [ ] T063 [US1] Create mock backend services using WireMock in tests/ApiGateway.IntegrationTests/Fixtures/MockBackendServices.cs
- [ ] T064 [US1] Run integration tests to verify routing works correctly
- [ ] T065 [US1] Add request/response logging for routed requests in RequestLoggingMiddleware
- [ ] T066 [US1] Add X-Forwarded-For header forwarding in YARP configuration
- [ ] T067 [US1] Run load tests to verify routing latency < 10ms (P95)

**Checkpoint**: At this point, User Story 1 should be fully functional - all requests route to correct backend services with < 10ms latency

---

## Phase 4: User Story 2 - JWT 身份驗證 (Priority: P1)

**Goal**: Gateway 統一處理 JWT 驗證，保護需認證的端點，傳遞使用者資訊到後端服務

**Independent Test**: 可透過發送帶有有效/無效/過期 JWT 的請求到需認證端點，並驗證 Gateway 是否正確接受或拒絕請求，即可完整測試此功能。

### Tests for User Story 2

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [ ] T068 [P] [US2] Create JwtAuthMiddlewareTests in tests/ApiGateway.UnitTests/Middlewares/JwtAuthMiddlewareTests.cs
- [ ] T069 [P] [US2] Test valid JWT passes authentication (AC-1)
- [ ] T070 [P] [US2] Test missing JWT returns 401 Unauthorized (AC-2)
- [ ] T071 [P] [US2] Test expired JWT returns 401 with "Token expired" message (AC-3)
- [ ] T072 [P] [US2] Test invalid JWT signature returns 401 with "Invalid token" message (AC-4)
- [ ] T073 [P] [US2] Test malformed JWT returns 401 with "Malformed token" message
- [ ] T074 [P] [US2] Create JwtAuthenticationTests in tests/ApiGateway.IntegrationTests/Authentication/JwtAuthenticationTests.cs
- [ ] T075 [P] [US2] Test public endpoints (login, register, auctions list) work without JWT (AC-5)
- [ ] T076 [P] [US2] Test private endpoints (bids, profile update) require JWT
- [ ] T077 [P] [US2] Test X-User-Id header is added to forwarded requests (AC-6)
- [ ] T078 [P] [US2] Create JWT verification latency benchmark in tests/ApiGateway.LoadTests/GatewayLoadTests.cs (verify < 20ms P95 per AC-7)

### Implementation for User Story 2

- [ ] T079 [P] [US2] Create IJwtAuthService interface in src/ApiGateway.Core/Interfaces/IJwtAuthService.cs
- [ ] T080 [P] [US2] Create UnauthorizedException in src/ApiGateway.Core/Exceptions/UnauthorizedException.cs
- [ ] T081 [US2] Implement JwtAuthService in src/ApiGateway.Infrastructure/Authentication/JwtAuthService.cs (HS256 validation, extract UserId claim)
- [ ] T082 [US2] Configure JWT authentication in src/ApiGateway.Api/Program.cs (add Microsoft.AspNetCore.Authentication.JwtBearer, configure Issuer/Audience/SecretKey from appsettings)
- [ ] T083 [US2] Create JwtAuthMiddleware in src/ApiGateway.Api/Middlewares/JwtAuthMiddleware.cs (extract JWT from Authorization header, validate, add X-User-Id to request)
- [ ] T084 [US2] Add JWT authentication to middleware pipeline in src/ApiGateway.Api/Program.cs (after CorrelationId, before YARP routing)
- [ ] T085 [US2] Configure public endpoints list in appsettings.json (routes that skip JWT validation)
- [ ] T086 [US2] Update ExceptionHandlingMiddleware to handle UnauthorizedException and JWT validation exceptions (map to ERROR codes: UNAUTHORIZED, TOKEN_EXPIRED, INVALID_TOKEN, MALFORMED_TOKEN)
- [ ] T087 [US2] Run unit tests to verify JWT validation logic
- [ ] T088 [US2] Run integration tests to verify end-to-end authentication flow
- [ ] T089 [US2] Run load tests to verify JWT verification latency < 20ms (P95)

**Checkpoint**: At this point, User Stories 1 AND 2 should both work - routing works + JWT authentication protects private endpoints

---

## Phase 5: User Story 5 - 限流與安全防護 (Priority: P1)

**Goal**: 實作 Rate Limiting 防止 API 濫用，配置 CORS 與 HTTPS 安全防護

**Independent Test**: 可透過在短時間內發送大量請求並驗證 Gateway 是否正確限流、設定 CORS、處理 HTTPS，即可完整測試此功能。

**Note**: US5 moved before US3/US4 because it's P1 priority (security-critical)

### Tests for User Story 5

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [ ] T090 [P] [US5] Create RateLimitServiceTests in tests/ApiGateway.UnitTests/Services/RateLimitServiceTests.cs
- [ ] T091 [P] [US5] Test 100 requests within 1 minute are allowed
- [ ] T092 [P] [US5] Test 101st request returns 429 Too Many Requests (AC-1)
- [ ] T093 [P] [US5] Test rate limit counter resets after 1 minute (AC-2)
- [ ] T094 [P] [US5] Test Redis INCR + TTL pattern works correctly
- [ ] T095 [P] [US5] Test Redis unavailable triggers fallback (allow all requests + log alert per FR-006)
- [ ] T096 [P] [US5] Create RateLimitMiddlewareTests in tests/ApiGateway.UnitTests/Middlewares/RateLimitMiddlewareTests.cs
- [ ] T097 [P] [US5] Test X-Forwarded-For IP extraction
- [ ] T098 [P] [US5] Test rate limit response includes Retry-After header
- [ ] T099 [P] [US5] Create RateLimitingTests in tests/ApiGateway.IntegrationTests/RateLimiting/RateLimitingTests.cs (with Testcontainers Redis)
- [ ] T100 [P] [US5] Test rate limiting accuracy is 100% (no false positives/negatives per AC-5)
- [ ] T101 [P] [US5] Create RedisTestContainer in tests/ApiGateway.IntegrationTests/Infrastructure/RedisTestContainer.cs (Testcontainers fixture)

### Implementation for User Story 5

- [ ] T102 [P] [US5] Create RateLimitKey value object in src/ApiGateway.Core/ValueObjects/RateLimitKey.cs (format: ratelimit:{ip}:{minute})
- [ ] T103 [P] [US5] Create RateLimitExceededException in src/ApiGateway.Core/Exceptions/RateLimitExceededException.cs
- [ ] T104 [P] [US5] Create IRateLimitService interface in src/ApiGateway.Core/Interfaces/IRateLimitService.cs
- [ ] T105 [US5] Create RateLimitRepository in src/ApiGateway.Infrastructure/Redis/RateLimitRepository.cs (Redis INCR + EXPIRE operations)
- [ ] T106 [US5] Implement RateLimitService in src/ApiGateway.Core/Services/RateLimitService.cs (100 req/min/IP, Redis Fixed Window algorithm)
- [ ] T107 [US5] Create RateLimitMiddleware in src/ApiGateway.Api/Middlewares/RateLimitMiddleware.cs (check rate limit before routing, use GetClientIp from HttpContextExtensions)
- [ ] T108 [US5] Add rate limit configuration to appsettings.json (RequestsPerMinute: 100, EnableRedisFailover: true)
- [ ] T109 [US5] Add RateLimitMiddleware to pipeline in src/ApiGateway.Api/Program.cs (after JWT auth, before YARP routing)
- [ ] T110 [US5] Update ExceptionHandlingMiddleware to handle RateLimitExceededException (map to RATE_LIMIT_EXCEEDED, include Retry-After header)
- [ ] T111 [US5] Implement Redis failover logic in RateLimitService (allow all + log alert when Redis unavailable)
- [ ] T112 [US5] Add rate limit event logging in RateLimitService (log IP, timestamp, exceeded count per FR-012)
- [ ] T113 [US5] Run unit tests to verify rate limiting logic
- [ ] T114 [US5] Run integration tests with Testcontainers Redis to verify end-to-end rate limiting
- [ ] T115 [US5] Verify CORS configuration already set in Phase 2 (T049) - test cross-origin requests (AC-3)
- [ ] T116 [US5] Verify HTTPS redirection already set in Phase 2 (T051) - test HTTP → HTTPS redirect (AC-4)

**Checkpoint**: At this point, User Stories 1, 2, AND 5 should all work - routing + authentication + rate limiting + CORS + HTTPS

---

## Phase 6: User Story 3 - 統一錯誤處理 (Priority: P2)

**Goal**: Gateway 統一處理後端錯誤並轉換為友善訊息，提供一致的錯誤回應格式

**Independent Test**: 可透過觸發各種後端錯誤 (404, 500, 503) 並驗證 Gateway 是否將錯誤轉換為統一格式且記錄日誌，即可完整測試此功能。

### Tests for User Story 3

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [ ] T117 [P] [US3] Create ExceptionHandlingMiddlewareTests in tests/ApiGateway.UnitTests/Middlewares/ExceptionHandlingMiddlewareTests.cs
- [ ] T118 [P] [US3] Test backend 404 returns unified error format (AC-1)
- [ ] T119 [P] [US3] Test backend 500 hides internal details and returns "服務暫時無法使用" (AC-2)
- [ ] T120 [P] [US3] Test backend timeout returns 503 Service Unavailable (AC-3)
- [ ] T121 [P] [US3] Test all errors include timestamp, path, code, message fields per FR-004
- [ ] T122 [P] [US3] Test payload too large (>10MB) returns 413 with PAYLOAD_TOO_LARGE code
- [ ] T123 [P] [US3] Test gateway timeout (>30s) returns 504 with GATEWAY_TIMEOUT code per FR-010
- [ ] T124 [P] [US3] Create ErrorHandlingTests in tests/ApiGateway.IntegrationTests/ErrorHandling/ErrorHandlingTests.cs
- [ ] T125 [P] [US3] Test error logging includes request path, timestamp, error type, userId (AC-4)

### Implementation for User Story 3

- [ ] T126 [P] [US3] Create ServiceUnavailableException in src/ApiGateway.Core/Exceptions/ServiceUnavailableException.cs
- [ ] T127 [US3] Update ExceptionHandlingMiddleware in src/ApiGateway.Api/Middlewares/ExceptionHandlingMiddleware.cs to handle all backend error codes (404 → NOT_FOUND, 500 → INTERNAL_SERVER_ERROR, 503 → SERVICE_UNAVAILABLE, 504 → GATEWAY_TIMEOUT, 413 → PAYLOAD_TOO_LARGE)
- [ ] T128 [US3] Add error response sanitization logic (hide internal stack traces in production)
- [ ] T129 [US3] Add comprehensive error logging with structured data (path, method, status, error type, userId, correlation ID per FR-012)
- [ ] T130 [US3] Configure backend service timeout (30s) in YARP appsettings.json per FR-010
- [ ] T131 [US3] Run unit tests to verify error mapping logic
- [ ] T132 [US3] Run integration tests to verify end-to-end error handling with mock backend failures
- [ ] T133 [US3] Verify error handling coverage reaches 100% per AC-5

**Checkpoint**: At this point, User Stories 1, 2, 3, AND 5 should all work - complete core Gateway functionality

---

## Phase 7: User Story 4 - 請求聚合 (Priority: P2)

**Goal**: Gateway 能組合多個服務的資料，減少客戶端多次請求，提升效能

**Independent Test**: 可透過呼叫聚合端點 (如商品詳細頁) 並驗證回應是否包含來自多個服務的完整資料 (商品+賣家+出價)，即可完整測試此功能。

### Tests for User Story 4

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [ ] T134 [P] [US4] Create AggregationServiceTests in tests/ApiGateway.UnitTests/Services/AggregationServiceTests.cs
- [ ] T135 [P] [US4] Test parallel calls to 3 services use Task.WhenAll (AC-1)
- [ ] T136 [P] [US4] Test all services succeed returns complete data (AC-2)
- [ ] T137 [P] [US4] Test one service fails returns partial data with metadata (AC-3)
- [ ] T138 [P] [US4] Test total latency equals slowest service (not sum) per AC-5
- [ ] T139 [P] [US4] Test aggregation latency < 300ms (P95) per AC-4
- [ ] T140 [P] [US4] Create AggregationControllerTests in tests/ApiGateway.UnitTests/Controllers/AggregationControllerTests.cs
- [ ] T141 [P] [US4] Create AggregationControllerIntegrationTests in tests/ApiGateway.IntegrationTests/Controllers/AggregationControllerIntegrationTests.cs
- [ ] T142 [P] [US4] Test GET /api/aggregated/auctions/{id} with all services available
- [ ] T143 [P] [US4] Test GET /api/aggregated/auctions/{id} with partial service failures

### Implementation for User Story 4

- [ ] T144 [P] [US4] Create AggregatedAuctionResponse DTO in src/ApiGateway.Core/DTOs/Responses/AggregatedAuctionResponse.cs per contracts/aggregation-endpoints.yaml
- [ ] T145 [P] [US4] Create AggregationRequest DTO in src/ApiGateway.Core/DTOs/Requests/AggregationRequest.cs (if needed)
- [ ] T146 [P] [US4] Create IAggregationService interface in src/ApiGateway.Core/Interfaces/IAggregationService.cs
- [ ] T147 [P] [US4] Create IAuctionServiceClient interface in src/ApiGateway.Core/Interfaces/IAuctionServiceClient.cs
- [ ] T148 [P] [US4] Create IBiddingServiceClient interface in src/ApiGateway.Core/Interfaces/IBiddingServiceClient.cs
- [ ] T149 [P] [US4] Create IUserServiceClient interface in src/ApiGateway.Core/Interfaces/IUserServiceClient.cs
- [ ] T150 [US4] Implement AuctionServiceClient in src/ApiGateway.Infrastructure/HttpClients/AuctionServiceClient.cs (with Polly retry policy)
- [ ] T151 [US4] Implement BiddingServiceClient in src/ApiGateway.Infrastructure/HttpClients/BiddingServiceClient.cs (with Polly retry policy)
- [ ] T152 [US4] Implement UserServiceClient in src/ApiGateway.Infrastructure/HttpClients/UserServiceClient.cs (with Polly retry policy)
- [ ] T153 [US4] Configure HttpClient factory with Polly in src/ApiGateway.Api/Program.cs (retry 3 times with exponential backoff)
- [ ] T154 [US4] Implement AggregationService in src/ApiGateway.Core/Services/AggregationService.cs (parallel calls using Task.WhenAll, handle partial failures)
- [ ] T155 [US4] Create AggregationController in src/ApiGateway.Api/Controllers/AggregationController.cs
- [ ] T156 [US4] Implement GET /api/aggregated/auctions/{id} endpoint
- [ ] T157 [US4] Add DataAvailabilityMetadata to response when services fail (set auction/seller/bids flags per contracts/aggregation-endpoints.yaml)
- [ ] T158 [US4] Configure aggregation timeout (30s shared across all parallel calls) per FR-010
- [ ] T159 [US4] Run unit tests to verify aggregation logic and partial failure handling
- [ ] T160 [US4] Run integration tests to verify end-to-end aggregation with mock services
- [ ] T161 [US4] Run load tests to verify aggregation latency < 300ms (P95)

**Checkpoint**: All user stories should now be independently functional - complete API Gateway feature set

---

## Phase 8: Health Checks & Monitoring

**Purpose**: Implement health check endpoint and enhance observability

- [ ] T162 [P] Create ServiceHealthStatus model in src/ApiGateway.Core/Models/ServiceHealthStatus.cs per data-model.md
- [ ] T163 [P] Create IHealthCheckService interface in src/ApiGateway.Core/Interfaces/IHealthCheckService.cs
- [ ] T164 Implement HealthCheckService in src/ApiGateway.Core/Services/HealthCheckService.cs (check all 3 backend services every 30s per FR-011)
- [ ] T165 Create HealthController in src/ApiGateway.Api/Controllers/HealthController.cs
- [ ] T166 Implement GET /health endpoint (return status + services per FR-011)
- [ ] T167 Configure ASP.NET Core Health Checks in src/ApiGateway.Api/Program.cs (integrate with backend service checks)
- [ ] T168 [P] Create HealthCheckTests in tests/ApiGateway.IntegrationTests/HealthCheck/HealthCheckTests.cs
- [ ] T169 [P] Test GET /health returns healthy when all services available
- [ ] T170 [P] Test GET /health returns degraded when some services unavailable
- [ ] T171 [P] Test GET /health returns unhealthy when all services unavailable
- [ ] T172 Configure YARP health check settings in appsettings.json (Active health checks every 30s per contracts/routes.yaml)
- [ ] T173 Run integration tests to verify health check endpoint

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories and final validation

- [ ] T174 [P] Update docs/architecture.md with final architecture diagrams and component descriptions
- [ ] T175 [P] Update docs/api-guide.md with all endpoint documentation (routing, auth, aggregation, health)
- [ ] T176 [P] Update docs/deployment.md with Docker + Kubernetes deployment instructions
- [ ] T177 [P] Create .github/workflows/ci.yml for continuous integration (build + test)
- [ ] T178 [P] Create .github/workflows/cd.yml for continuous deployment (Docker push + K8s deploy)
- [ ] T179 Code review and refactoring for code quality (ensure > 80% test coverage per SC-006)
- [ ] T180 Performance optimization review (verify all latency targets: routing < 10ms, JWT < 20ms, aggregation < 300ms)
- [ ] T181 Security hardening review (verify JWT, HTTPS, CORS, rate limiting all working correctly)
- [ ] T182 [P] Add additional unit tests if coverage < 80%
- [ ] T183 Run complete test suite (unit + integration + load tests)
- [ ] T184 Run quickstart.md validation (follow all steps to ensure documentation is accurate)
- [ ] T185 Verify all success criteria from spec.md (SC-001 through SC-007)
- [ ] T186 Load testing at scale (1000+ concurrent requests per plan.md Scale/Scope)
- [ ] T187 Create production deployment checklist (environment variables, secrets, configuration)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-7)**: All depend on Foundational phase completion
  - US1 (請求路由): Can start after Phase 2 - No dependencies on other stories
  - US2 (JWT 驗證): Can start after Phase 2 - No dependencies, but should follow US1 for testing convenience
  - US5 (限流防護): Can start after Phase 2 - No dependencies, placed after US1+US2 because it builds on routing+auth
  - US3 (錯誤處理): Can start after Phase 2 - Enhances existing functionality
  - US4 (請求聚合): Can start after Phase 2 - Independent feature but benefits from having routing+auth working
- **Health Checks (Phase 8)**: Can start after Phase 2 - Independent monitoring feature
- **Polish (Phase 9)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (請求路由)**: Foundational routing - no dependencies
- **User Story 2 (JWT 驗證)**: Independent authentication - can run parallel with US1 but typically done after for easier testing
- **User Story 5 (限流防護)**: Independent security - can run parallel but benefits from US1+US2 being complete
- **User Story 3 (錯誤處理)**: Cross-cutting - enhances all stories but doesn't block them
- **User Story 4 (請求聚合)**: Independent feature - can run parallel with others

### Within Each User Story

- Tests MUST be written and FAIL before implementation (TDD approach per SC-006)
- DTOs and exceptions before services
- Services before controllers/middleware
- Controllers before integration tests
- Story complete and validated before moving to next priority

### Parallel Opportunities

- **Phase 1 (Setup)**: T003-T009 (all project creation), T011-T018 (NuGet packages), T019-T031 (config files and docs) can all run in parallel
- **Phase 2 (Foundational)**: T032-T036 (DTOs and extensions), T042-T043 (appsettings) can run in parallel
- **User Story Tests**: Within each story, all test tasks marked [P] can run simultaneously
- **User Story Models**: Within each story, all DTO/Exception/Interface creation tasks marked [P] can run simultaneously
- **Different User Stories**: After Phase 2, different developers can work on US1, US2, US5, US3, US4 in parallel
- **Phase 8**: Health check implementation can proceed in parallel with user story work
- **Phase 9**: Documentation tasks (T174-T178) can run in parallel

---

## Parallel Example: User Story 1 (請求路由)

```bash
# Launch all tests for US1 together:
T052: "Create YarpRoutingTests in tests/ApiGateway.IntegrationTests/Routing/YarpRoutingTests.cs"
T053: "Test GET /api/members/* routes to Member Service"
T054: "Test GET /api/auctions/* routes to Auction Service"
T055: "Test GET /api/bids/* routes to Bidding Service"
T056: "Test GET /api/me/bids routes to Bidding Service"
T057: "Test GET /api/me/follows routes to Auction Service"
T058: "Test GET /api/me routes to Member Service"
T059: "Test PUT /api/me routes to Member Service"
T060: "Create routing latency benchmark test"

# After tests are written and failing, proceed with implementation
```

## Parallel Example: User Story 2 (JWT 驗證)

```bash
# Launch all parallel tasks for US2 together:
T068-T078: All test creation tasks (unit + integration + load tests)
T079: "Create IJwtAuthService interface"
T080: "Create UnauthorizedException"

# After interfaces/exceptions created, implement service:
T081: "Implement JwtAuthService"
```

---

## Implementation Strategy

### MVP First (User Stories 1 + 2 + 5 Only)

1. Complete Phase 1: Setup (T001-T031)
2. Complete Phase 2: Foundational (T032-T051) - CRITICAL blocking phase
3. Complete Phase 3: User Story 1 - 請求路由 (T052-T067)
4. **STOP and VALIDATE**: Test US1 independently - routing works correctly
5. Complete Phase 4: User Story 2 - JWT 驗證 (T068-T089)
6. **STOP and VALIDATE**: Test US1+US2 together - routing + authentication work
7. Complete Phase 5: User Story 5 - 限流防護 (T090-T116)
8. **STOP and VALIDATE**: Test US1+US2+US5 together - core Gateway functionality complete
9. Deploy/demo MVP with P1 features (routing + auth + security)

**MVP Scope**: User Stories 1, 2, 5 provide complete core Gateway functionality - routing, authentication, and security protection

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 (請求路由) → Test independently → Deploy/Demo
3. Add User Story 2 (JWT 驗證) → Test independently → Deploy/Demo
4. Add User Story 5 (限流防護) → Test independently → Deploy/Demo (MVP complete!)
5. Add User Story 3 (錯誤處理) → Test independently → Deploy/Demo
6. Add User Story 4 (請求聚合) → Test independently → Deploy/Demo
7. Add Health Checks → Test independently → Deploy/Demo
8. Polish & optimize → Final production release

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together (critical path)
2. Once Foundational is done (after T051):
   - **Developer A**: User Story 1 (請求路由) - T052-T067
   - **Developer B**: User Story 2 (JWT 驗證) - T068-T089
   - **Developer C**: User Story 5 (限流防護) - T090-T116
   - **Developer D**: User Story 3 (錯誤處理) - T117-T133
   - **Developer E**: User Story 4 (請求聚合) - T134-T161
   - **Developer F**: Health Checks (Phase 8) - T162-T173
3. Each story completes and integrates independently
4. Team collaborates on Phase 9 (Polish) for final production readiness

---

## Validation Checklist

After completing all tasks, verify:

- [ ] All 5 user stories (US1-US5) are independently functional
- [ ] All acceptance criteria from spec.md are met
- [ ] All success criteria (SC-001 through SC-007) are achieved
- [ ] Test coverage > 80% per SC-006
- [ ] All performance targets met: routing < 10ms, JWT < 20ms, aggregation < 300ms (P95)
- [ ] System availability > 99.9% demonstrated
- [ ] All FR-001 through FR-015 functional requirements implemented
- [ ] quickstart.md steps work correctly
- [ ] Documentation is complete and accurate
- [ ] Production deployment is ready (Docker + Kubernetes configs)

---

## Notes

- [P] tasks = different files, no dependencies - can run in parallel
- [Story] label maps task to specific user story (US1, US2, US3, US4, US5) for traceability
- Each user story is independently completable and testable
- TDD approach required: Write tests FIRST, ensure they FAIL, then implement
- Commit after each task or logical group
- Stop at each checkpoint to validate story independently
- All file paths are relative to ApiGateway/ root directory
- Follow constitution principles: Code Quality (FR-015), TDD (SC-006), UX Consistency (FR-004), Performance (success metrics), Observability (FR-012)
