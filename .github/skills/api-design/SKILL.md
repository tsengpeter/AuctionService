---
name: api-design
description: "API design rules and conventions for AuctionService (ASP.NET Core 10 / C# 13). Use when: designing new API endpoints, writing controllers, reviewing API code, adding routes, implementing CQRS handlers, handling errors, or validating API contracts. Covers ApiResponse<T> wrapper, HTTP status codes, RESTful naming, validation (422), JWT auth errors, pagination, filtering, controller slim principle, and MediatR dispatch patterns."
argument-hint: "Optional: specify the module or endpoint to design (e.g. 'Auction listing endpoint')"
---

# API Design Patterns — AuctionService (ASP.NET Core)

Conventions and best practices for designing consistent REST APIs in this modular monolith.

## When to Activate

- Designing or implementing a new API endpoint
- Writing or reviewing a Controller action
- Deciding on HTTP status codes or route naming
- Implementing pagination, filtering, or sorting
- Handling validation errors or auth failures
- Reviewing any C# controller or Application layer code

---

## Resource Design

### URL Structure

```
# Resources are nouns, plural, lowercase, kebab-case
GET    /api/auctions
GET    /api/auctions/{id}
POST   /api/auctions
PUT    /api/auctions/{id}
PATCH  /api/auctions/{id}
DELETE /api/auctions/{id}

# Sub-resources for relationships
GET    /api/auctions/{id}/bids
POST   /api/auctions/{id}/bids

# Actions that don't map to CRUD (use verbs sparingly)
POST   /api/auctions/{id}/cancel
POST   /api/auth/login
POST   /api/auth/refresh
```

### Naming Rules

```
# GOOD
/api/auction-items            # kebab-case for multi-word resources
/api/auctions?status=active   # query params for filtering
/api/auctions/{id}/bids       # nested resources for ownership

# BAD
/api/getAuctions              # verb in URL
/api/auction                  # singular resource (use plural)
/api/auction_items            # snake_case in URLs
/api/auctions/{id}/getBids    # verb in nested resource
```

---

## HTTP Methods and Status Codes

### Method Semantics

| Method | Idempotent | Safe | Use For |
|--------|-----------|------|---------|
| GET | Yes | Yes | Retrieve resources |
| POST | No | No | Create resources, trigger actions |
| PUT | Yes | No | Full replacement of a resource |
| PATCH | No* | No | Partial update of a resource |
| DELETE | Yes | No | Remove a resource |

*PATCH can be made idempotent with proper implementation

### Status Code Reference

| Code | When to Use |
|------|-------------|
| 200 OK | GET, PUT, PATCH success (with body) |
| 201 Created | POST success (include `Location` header) |
| 204 No Content | DELETE success (no body) |
| 401 Unauthorized | Missing or invalid JWT |
| 403 Forbidden | Authenticated but not authorized |
| 404 Not Found | Resource doesn't exist |
| 409 Conflict | Duplicate entry, state conflict |
| **422 Unprocessable Entity** | **FluentValidation field errors — always 422, never 400** |
| 500 Internal Server Error | Unexpected failure — never expose details |

### Common Mistakes

```
# BAD: 200 for everything
{ "success": false, "error": "Not found", "statusCode": 200 }

# GOOD: Use HTTP status codes semantically
HTTP/1.1 404 Not Found
{ "success": false, "error": "Auction not found", "statusCode": 404 }

# BAD: 400 or 500 for validation errors
# GOOD: 422 with field-level details from FluentValidation

# BAD: 200 for created resources without Location
# GOOD: 201 with Location header
HTTP/1.1 201 Created
Location: /api/auctions/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

---

## Response Format — `ApiResponse<T>`

**Every** endpoint MUST use `ApiResponseFactory`. Never return raw objects or plain `IActionResult` without the wrapper.

### Success Responses

```csharp
// 200 OK
return Ok(ApiResponseFactory.Ok(dto));

// 201 Created
Response.Headers.Location = $"/api/auctions/{result.Id}";
return StatusCode(201, ApiResponseFactory.Created(result));

// 204 No Content
return NoContent(); // no body needed
```

JSON shape:
```json
{ "success": true, "data": { "id": "...", "title": "..." }, "statusCode": 200 }
```

### Paginated Collection Response

```json
{
  "success": true,
  "data": {
    "items": [{ "id": "...", "title": "..." }],
    "total": 142,
    "page": 2,
    "pageSize": 20,
    "totalPages": 8
  },
  "statusCode": 200
}
```

### Validation Error Response (422)

```json
{
  "success": false,
  "errors": [
    { "field": "title", "message": "'Title' must not be empty." },
    { "field": "startPrice", "message": "'Start Price' must be greater than '0'." }
  ],
  "statusCode": 422
}
```

### General Error Responses

```json
{ "success": false, "error": "Auction not found", "statusCode": 404 }
{ "success": false, "error": "Unauthorized", "statusCode": 401 }
{ "success": false, "error": "Forbidden", "statusCode": 403 }
{ "success": false, "error": "An unexpected error occurred", "statusCode": 500 }
```

---

## Controller Slim Principle

Controllers are **thin dispatchers only**. They MUST NOT contain:
- Business logic or domain rules
- Direct `DbContext` access
- Validation logic (handled by `ValidationBehavior`)
- Mapping logic

```csharp
// ✅ Correct — thin controller dispatches to MediatR
[ApiController]
[Route("api/auctions")]
public class AuctionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuctionsController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(
        [FromBody] CreateAuctionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateAuctionCommand(request.Title, request.StartPrice);
        var result = await _mediator.Send(command, cancellationToken);
        Response.Headers.Location = $"/api/auctions/{result.Id}";
        return StatusCode(201, ApiResponseFactory.Created(result));
    }
}

// ❌ Wrong — business logic and DbContext in controller
[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateAuctionRequest request)
{
    if (request.StartPrice <= 0) return BadRequest("Invalid price"); // ← wrong
    var auction = new Auction { Title = request.Title };             // ← wrong
    _dbContext.Auctions.Add(auction);                               // ← wrong
    await _dbContext.SaveChangesAsync();
    return Ok(auction);                                             // ← wrong (no wrapper)
}
```

---

## Validation — FluentValidation + MediatR Pipeline

- Define validators with `AbstractValidator<TCommand>`
- `ValidationBehavior` (MediatR `IPipelineBehavior`) runs validators automatically before handlers
- `GlobalExceptionMiddleware` catches `ValidationException` → HTTP 422
- **Never** manually throw `ValidationException` from handlers
- **Never** use `[ApiController]`'s built-in 400 model validation for domain commands — always rely on FluentValidation

```csharp
public class CreateAuctionCommandValidator : AbstractValidator<CreateAuctionCommand>
{
    public CreateAuctionCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.StartPrice).GreaterThan(0);
        RuleFor(x => x.EndsAt).GreaterThan(DateTimeOffset.UtcNow);
    }
}
```

---

## Authentication & Authorization

JWT 401/403 MUST be handled via `OnChallenge` / `OnForbidden` events — **not** middleware or exception filters — so that the response uses `ApiResponse` format.

```csharp
// ✅ In AddAuthentication / AddJwtBearer setup
options.Events = new JwtBearerEvents
{
    OnChallenge = async context =>
    {
        context.HandleResponse();
        context.Response.StatusCode = 401;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(ApiResponseFactory.Fail("Unauthorized", 401));
    },
    OnForbidden = async context =>
    {
        context.Response.StatusCode = 403;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(ApiResponseFactory.Fail("Forbidden", 403));
    }
};
```

Controller attributes:
```csharp
[Authorize]                        // requires valid JWT
[Authorize(Roles = "Admin")]       // role-based
[AllowAnonymous]                   // public endpoint (explicit, for clarity)
```

---

## CQRS Dispatch Pattern (MediatR)

- **Commands** (write operations): `XxxCommand` → returns DTO or `Unit`
- **Queries** (read operations): `XxxQuery` → returns DTO or `PagedResult<T>`
- Handlers live in `Application/` layer of the owning module
- Always pass `CancellationToken` through to async calls

```csharp
// Command
public record CreateAuctionCommand(string Title, decimal StartPrice, DateTimeOffset EndsAt)
    : IRequest<AuctionDto>;

// Query
public record ListAuctionsQuery(
    int Page = 1,
    int PageSize = 20,
    string? SortBy = null,
    string SortOrder = "desc",
    string? Status = null
) : IRequest<PagedResult<AuctionDto>>;

// Handler
public class ListAuctionsQueryHandler : IRequestHandler<ListAuctionsQuery, PagedResult<AuctionDto>>
{
    private readonly AuctionDbContext _db;

    public ListAuctionsQueryHandler(AuctionDbContext db) => _db = db;

    public async Task<PagedResult<AuctionDto>> Handle(
        ListAuctionsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Auctions.AsQueryable();

        if (request.Status is not null)
            query = query.Where(a => a.Status == request.Status);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDynamic(request.SortBy, request.SortOrder)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new AuctionDto(a.Id, a.Title, a.StartPrice))
            .ToListAsync(cancellationToken);

        return new PagedResult<AuctionDto>(items, total, request.Page, request.PageSize);
    }
}
```

---

## Pagination, Filtering & Sorting

### Query String Convention

| Parameter | Type | Default | Example |
|---|---|---|---|
| `page` | int | 1 | `?page=2` |
| `pageSize` | int | 20 (max 100) | `?pageSize=50` |
| `sortBy` | string | resource-dependent | `?sortBy=createdAt` |
| `sortOrder` | `asc` \| `desc` | `desc` | `?sortOrder=asc` |
| Filter fields | varies | — | `?status=active` |

### Controller Binding

```csharp
[HttpGet]
[AllowAnonymous]
public async Task<IActionResult> List(
    [FromQuery] ListAuctionsQuery query,
    CancellationToken cancellationToken)
{
    var result = await _mediator.Send(query, cancellationToken);
    return Ok(ApiResponseFactory.Ok(result));
}
```

---

## Module Isolation Rules

- Each module has its own `DbContext` scoped to a dedicated PostgreSQL schema
- **Never** reference another module's EF entities — use logical `Guid` references only
- **Never** inject another module's `DbContext`
- Cross-module data flows through domain events (`IDomainEvent : INotification`) only

```csharp
// ✅ Logical Guid reference (good)
public class Bid : BaseEntity
{
    public Guid AuctionId { get; private set; }  // logical ref, no FK to Auction table
    public Guid BidderId { get; private set; }   // logical ref, no FK to Member table
}

// ❌ Cross-module EF navigation (forbidden)
public class Bid : BaseEntity
{
    public Auction Auction { get; private set; }  // violates module isolation
}
```

---

## JSON Serialization Conventions

- All property names: **camelCase**
- DateTimes: ISO 8601 with UTC offset → `"2026-04-07T12:00:00+00:00"`
- Enums: serialized as **strings**, not integers
- Nulls: omit from response where semantically meaningless

---

## Versioning Strategy

- Current approach: no version prefix (APIs are internal/single-consumer)
- If versioning becomes needed, prefer **URL path versioning**: `/api/v2/auctions`
- Non-breaking changes (adding fields, new optional params, new endpoints) do NOT require a new version
- Breaking changes (removing/renaming fields, changing types) require a new version with deprecation notice

---

## OpenAPI / Swagger

- Swagger UI available **only in non-production**: `/swagger/index.html`
- Annotate endpoints with `[ProducesResponseType]` for accurate schema generation:

```csharp
[ProducesResponseType(typeof(ApiResponse<AuctionDto>), 201)]
[ProducesResponseType(typeof(ApiResponse<object>), 422)]
[ProducesResponseType(typeof(ApiResponse<object>), 401)]
```

- Keep `specs/001-backend-scaffold/contracts/openapi.yaml` in sync with implemented routes

---

## New Endpoint Checklist

Before shipping a new endpoint:

- [ ] Route is plural noun, kebab-case, no verbs in URL
- [ ] Correct HTTP method (GET for reads, POST for creates, etc.)
- [ ] Controller action is thin — dispatches only to MediatR
- [ ] Command/Query defined in `Application/` of the owning module
- [ ] FluentValidation `AbstractValidator<T>` created for commands with inputs
- [ ] All responses use `ApiResponseFactory` (never raw `Ok(dto)`)
- [ ] `[Authorize]` or `[AllowAnonymous]` attribute explicitly applied
- [ ] Paginated list endpoints use standard `page`/`pageSize`/`sortBy`/`sortOrder` query params
- [ ] No cross-module `DbContext` access
- [ ] `CancellationToken` passed through to all async calls
- [ ] Response does not leak internal details (stack traces, SQL errors)
- [ ] `[ProducesResponseType]` attributes added for Swagger accuracy
- [ ] `openapi.yaml` contract updated to reflect the new route
