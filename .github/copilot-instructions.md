# AuctionService Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-03-22

## Active Technologies

- (001-backend-scaffold) C# 13 / .NET 10 / ASP.NET Core 10
- MediatR 12.x — CQRS commands/queries + INotification domain events
- FluentValidation 12.x — input validation via MediatR IPipelineBehavior
- EF Core 10 + Npgsql.EntityFrameworkCore.PostgreSQL 10 — ORM, schema-isolated DbContext per module
- Microsoft.AspNetCore.Authentication.JwtBearer 10 — JWT HS256, custom 401/403 via OnChallenge/OnForbidden
- Swashbuckle.AspNetCore 8.1 — Swagger UI + OpenAPI 3.1 (non-production only)
- xUnit 2.9 + Testcontainers.PostgreSql 4.x — unit + integration tests
- FluentAssertions 8.x + NSubstitute 5.x — test assertion DSL + mocking
- Docker Compose — PostgreSQL 16 (port 5432) + pgAdmin 4 (port 5050)

## Project Structure

```text
src/
├── AuctionService.Api/          # API host (Program.cs, Middleware)
├── AuctionService.Shared/       # Cross-cutting: ApiResponse<T>, BaseEntity, IDomainEvent, ValidationBehavior
└── Modules/
    ├── Member/                  # Schema: member
    ├── Auction/                 # Schema: auction
    ├── Bidding/                 # Schema: bidding
    ├── Ordering/                # Schema: ordering
    └── Notification/            # Schema: notification

tests/
├── AuctionService.UnitTests/
└── AuctionService.IntegrationTests/
```

## Commands

```bash
# Build
dotnet build

# Test
dotnet test

# Run API
dotnet run --project src/AuctionService.Api/AuctionService.Api.csproj

# Start dependencies
docker compose up -d

# Add migration (example: Member module)
dotnet ef migrations add <Name> --project src/Modules/Member/Member.csproj --startup-project src/AuctionService.Api/AuctionService.Api.csproj --output-dir Infrastructure/Persistence/Migrations

# Apply migration
dotnet ef database update --project src/Modules/Member/Member.csproj --startup-project src/AuctionService.Api/AuctionService.Api.csproj
```

## Code Style

C# 13 / .NET 10: Follow standard ASP.NET Core conventions

- All API responses MUST use `ApiResponse<T>` wrapper (`{ success, data, statusCode }`)
- Validation errors return HTTP 422 with `{ success: false, errors: [{ field, message }], statusCode: 422 }`
- JWT 401/403 handled by `OnChallenge`/`OnForbidden` events, NOT middleware
- No cross-module database foreign keys — use logical Guid references only
- Each module owns its DbContext + schema; no module reads another module's tables
- Domain events implement `IDomainEvent : INotification`; handlers via `INotificationHandler<T>`
- All plan/spec/task documentation in Traditional Chinese (zh-TW); code + commits in English
- JWT secret from environment variable (min 32 chars), never hardcoded

## Recent Changes

- 001-backend-scaffold: Added full modular monolith scaffold plan — 5 modules, ApiResponse<T>, JWT, EF Core schema isolation, Testcontainers integration tests

<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->

