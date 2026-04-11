---
name: backend-patterns
description: "Backend architecture patterns for AuctionService (ASP.NET Core 10 / C# 13). Use when: implementing CQRS handlers, domain events, EF Core queries, module DI registration, background services, error handling middleware, or any server-side architectural pattern. Covers MediatR commands/queries, FluentValidation pipeline, EF Core N+1 prevention, transactions, BaseEntity, module isolation, and IHostedService."
argument-hint: "Optional: specify the pattern or layer you are implementing (e.g. 'EF Core transaction in Auction module')"
---

# Backend Development Patterns — AuctionService (ASP.NET Core)

Architecture patterns and best practices for this C# 13 / .NET 10 modular monolith.

## When to Activate

- Implementing a MediatR Command or Query handler
- Writing domain events or handlers
- Optimizing EF Core queries (N+1, projection, transactions)
- Registering a new module's services (DI extension method)
- Adding a background service (`IHostedService`)
- Structuring error handling or validation pipeline
- Creating or updating domain entities

---

## API Style — Hard Constraints

**Minimal API is NOT allowed.** All HTTP endpoints must be implemented using one of:

| Style | When to Use |
|---|---|
| **Controller-based REST** (`ControllerBase`) | Default — all current modules use this |
| **GraphQL** (e.g. Hot Chocolate) | Only if a module explicitly requires flexible querying |

```csharp
// ✅ Correct — controller-based
[ApiController]
[Route("api/auctions")]
public class AuctionsController : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(ApiResponseFactory.Ok(await _mediator.Send(new GetAuctionQuery(id), ct)));
}

// ❌ Forbidden — Minimal API
app.MapGet("/api/auctions/{id}", async (Guid id, IMediator mediator) =>
    Results.Ok(await mediator.Send(new GetAuctionQuery(id))));
```

---

## Modular Monolith Architecture

Each module is a separate C# project (`src/Modules/<Name>/`) with:

```
Modules/<Name>/
├── <Name>.csproj
├── Application/          # CQRS: Commands, Queries, Handlers, Validators, DTOs
├── Domain/               # Entities, Value Objects, IDomainEvent implementations
└── Infrastructure/
    ├── Persistence/      # DbContext, Migrations, EF Config
    └── DependencyInjection.cs
```

**Rules:**
- Each module has its own `DbContext` scoped to a dedicated PostgreSQL schema
- No cross-module EF navigation properties or foreign keys
- Cross-module communication via domain events (`IDomainEvent`) only
- Logical `Guid` references between modules — never EF entity references

---

## Domain Entities — BaseEntity

All domain entities inherit `BaseEntity`:

```csharp
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; protected set; }
    public DateTimeOffset UpdatedAt { get; protected set; }
}
```

```csharp
// ✅ Correct entity
public class Auction : BaseEntity
{
    public string Title { get; private set; } = string.Empty;
    public decimal StartPrice { get; private set; }
    public string Status { get; private set; } = "Pending";

    // Private constructor for EF
    private Auction() { }

    public static Auction Create(string title, decimal startPrice)
    {
        return new Auction
        {
            Title = title,
            StartPrice = startPrice,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }
}
```

---

## CQRS — Commands and Queries (MediatR)

### Commands (Write)

```csharp
// Command — named with imperative verb + "Command"
public record CreateAuctionCommand(
    string Title,
    decimal StartPrice,
    DateTimeOffset EndsAt
) : IRequest<AuctionDto>;

// Handler — lives in Application/Auctions/Commands/
public class CreateAuctionCommandHandler
    : IRequestHandler<CreateAuctionCommand, AuctionDto>
{
    private readonly AuctionDbContext _db;
    private readonly IPublisher _publisher;

    public CreateAuctionCommandHandler(AuctionDbContext db, IPublisher publisher)
    {
        _db = db;
        _publisher = publisher;
    }

    public async Task<AuctionDto> Handle(
        CreateAuctionCommand request, CancellationToken cancellationToken)
    {
        var auction = Auction.Create(request.Title, request.StartPrice);
        _db.Auctions.Add(auction);
        await _db.SaveChangesAsync(cancellationToken);

        // Publish domain event AFTER saving
        await _publisher.Publish(new AuctionCreatedEvent(auction.Id), cancellationToken);

        return new AuctionDto(auction.Id, auction.Title, auction.StartPrice);
    }
}
```

### Queries (Read)

```csharp
// Query — named with "Get/List" + "Query"
public record GetAuctionQuery(Guid Id) : IRequest<AuctionDto>;

public class GetAuctionQueryHandler : IRequestHandler<GetAuctionQuery, AuctionDto>
{
    private readonly AuctionDbContext _db;

    public GetAuctionQueryHandler(AuctionDbContext db) => _db = db;

    public async Task<AuctionDto> Handle(
        GetAuctionQuery request, CancellationToken cancellationToken)
    {
        var dto = await _db.Auctions
            .Where(a => a.Id == request.Id)
            .Select(a => new AuctionDto(a.Id, a.Title, a.StartPrice))  // project, don't load entity
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException($"Auction {request.Id} not found");

        return dto;
    }
}
```

---

## Validation — FluentValidation Pipeline

Validators run automatically via `ValidationBehavior` before any handler executes.

```csharp
// Create a validator alongside every Command that accepts external input
public class CreateAuctionCommandValidator : AbstractValidator<CreateAuctionCommand>
{
    public CreateAuctionCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.StartPrice)
            .GreaterThan(0)
            .WithMessage("Start price must be greater than zero.");

        RuleFor(x => x.EndsAt)
            .GreaterThan(DateTimeOffset.UtcNow)
            .WithMessage("End date must be in the future.");
    }
}
```

**Rules:**
- Every `IRequest<T>` command that takes user input MUST have a paired validator
- Never validate manually inside handlers — use FluentValidation rules only
- `GlobalExceptionMiddleware` catches `ValidationException` → HTTP 422

---

## EF Core Patterns

### DbContext — Schema Isolation

```csharp
public class AuctionDbContext : DbContext
{
    public AuctionDbContext(DbContextOptions<AuctionDbContext> options) : base(options) { }

    public DbSet<Auction> Auctions => Set<Auction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("auction");  // schema isolation
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
    }
}
```

### Entity Type Configuration

```csharp
public class AuctionConfiguration : IEntityTypeConfiguration<Auction>
{
    public void Configure(EntityTypeBuilder<Auction> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Title).HasMaxLength(200).IsRequired();
        builder.Property(a => a.StartPrice).HasPrecision(18, 2);
        builder.HasIndex(a => a.Status);
    }
}
```

### N+1 Query Prevention

```csharp
// ❌ BAD: N+1 — loads auction, then queries bids for each one
var auctions = await _db.Auctions.ToListAsync();
foreach (var auction in auctions)
{
    var bidCount = await _db.Bids.CountAsync(b => b.AuctionId == auction.Id); // N queries
}

// ✅ GOOD: Project everything in one query
var auctions = await _db.Auctions
    .Select(a => new AuctionListDto(
        a.Id,
        a.Title,
        a.StartPrice,
        _db.Bids.Count(b => b.AuctionId == a.Id)  // subquery, single round-trip
    ))
    .ToListAsync(cancellationToken);

// ✅ GOOD: Use Include() only when navigation is needed in the result
var auction = await _db.Auctions
    .Include(a => a.Images)
    .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
```

### Projection — Always Use for Read Queries

```csharp
// ❌ BAD: Loads full entity when only a DTO is needed
var auction = await _db.Auctions.FindAsync(id);
return new AuctionDto(auction.Id, auction.Title);

// ✅ GOOD: Project directly to DTO at the database level
var dto = await _db.Auctions
    .Where(a => a.Id == id)
    .Select(a => new AuctionDto(a.Id, a.Title, a.StartPrice))
    .FirstOrDefaultAsync(cancellationToken);
```

### Transactions

```csharp
// Use ExecutionStrategy for PostgreSQL retry-safe transactions
public async Task Handle(CompleteAuctionCommand request, CancellationToken ct)
{
    var strategy = _db.Database.CreateExecutionStrategy();

    await strategy.ExecuteAsync(async () =>
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var auction = await _db.Auctions.FindAsync([request.AuctionId], ct)
                ?? throw new KeyNotFoundException("Auction not found");

            auction.Complete();
            await _db.SaveChangesAsync(ct);

            // Publish event INSIDE transaction so it only fires on success
            await _publisher.Publish(new AuctionCompletedEvent(auction.Id), ct);

            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    });
}
```

### Pagination with EF Core

```csharp
// Shared PagedResult<T> return type
public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Total,
    int Page,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
}

// In handler
var query = _db.Auctions.Where(a => a.Status == request.Status).AsQueryable();

var total = await query.CountAsync(cancellationToken);

var items = await query
    .OrderByDescending(a => a.CreatedAt)
    .Skip((request.Page - 1) * request.PageSize)
    .Take(request.PageSize)
    .Select(a => new AuctionDto(a.Id, a.Title, a.StartPrice))
    .ToListAsync(cancellationToken);

return new PagedResult<AuctionDto>(items, total, request.Page, request.PageSize);
```

---

## Domain Events (Cross-Module Communication)

```csharp
// 1. Define the event in the publishing module (Domain layer)
public record AuctionWonEvent(Guid AuctionId, Guid WinnerId, decimal FinalPrice)
    : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

// 2. Publish AFTER SaveChangesAsync succeeds
await _db.SaveChangesAsync(cancellationToken);
await _publisher.Publish(new AuctionWonEvent(auction.Id, winner.Id, finalPrice), cancellationToken);

// 3. Handle in another module (e.g. Ordering)
public class AuctionWonEventHandler : INotificationHandler<AuctionWonEvent>
{
    private readonly OrderingDbContext _db;

    public AuctionWonEventHandler(OrderingDbContext db) => _db = db;

    public async Task Handle(AuctionWonEvent notification, CancellationToken cancellationToken)
    {
        var order = Order.Create(notification.AuctionId, notification.WinnerId, notification.FinalPrice);
        _db.Orders.Add(order);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
```

**Rules:**
- Event names use past tense: `AuctionWonEvent`, `BidPlacedEvent`
- Publish AFTER `SaveChangesAsync` — never before
- Handler failures are isolated (one handler failing doesn't affect others)
- The current module owns the event definition; subscribing modules import only the event type

---

## Module DI Registration Pattern

```csharp
// Infrastructure/DependencyInjection.cs in each module
public static class DependencyInjection
{
    public static IServiceCollection AddAuctionModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AuctionDbContext>(opts =>
            opts.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}

// Program.cs wires up all modules
builder.Services.AddAuctionModule(builder.Configuration);
builder.Services.AddBiddingModule(builder.Configuration);
```

---

## Error Handling — GlobalExceptionMiddleware

The middleware handles all unhandled exceptions centrally:

| Exception Type | HTTP Code | Response |
|---|---|---|
| `ValidationException` (FluentValidation) | 422 | `{ success: false, errors: [{field, message}] }` |
| `KeyNotFoundException` | 404 | `{ success: false, error: "message" }` |
| Any other `Exception` | 500 | `{ success: false, error: "An unexpected error occurred" }` |

**Rules:**
- Never catch `Exception` generically inside handlers unless you plan to rethrow
- Throw `KeyNotFoundException` for missing resources — middleware maps it to 404
- Never expose stack traces; middleware strips them in production

---

## Background Services — IHostedService

```csharp
// Register as a hosted service; runs for the lifetime of the application
public class AuctionExpiryService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuctionExpiryService> _logger;

    public AuctionExpiryService(
        IServiceScopeFactory scopeFactory,
        ILogger<AuctionExpiryService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessExpiredAuctions(stoppingToken);
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task ProcessExpiredAuctions(CancellationToken ct)
    {
        // MUST create a scope — DbContext is scoped, BackgroundService is singleton
        await using var scope = _scopeFactory.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(new ExpireAuctionsCommand(), ct);
    }
}

// Registration in module DI
services.AddHostedService<AuctionExpiryService>();
```

**Rules:**
- Background services are singleton — always use `IServiceScopeFactory` to resolve scoped services (DbContext, MediatR)
- Dispatch work through MediatR commands rather than calling services directly
- Handle `OperationCanceledException` (or via `stoppingToken`) for graceful shutdown

---

## Async Best Practices

```csharp
// ✅ Always propagate CancellationToken
public async Task<AuctionDto> Handle(GetAuctionQuery request, CancellationToken cancellationToken)
{
    return await _db.Auctions
        .Where(a => a.Id == request.Id)
        .Select(a => new AuctionDto(a.Id, a.Title, a.StartPrice))
        .FirstOrDefaultAsync(cancellationToken)  // ← pass token
        ?? throw new KeyNotFoundException($"Auction {request.Id} not found");
}

// ✅ Use ConfigureAwait(false) in library/infrastructure code (not needed in ASP.NET Core handlers)

// ❌ Never block async code
var result = _mediator.Send(command).Result;      // deadlock risk
var result = _mediator.Send(command).GetAwaiter().GetResult();  // same

// ✅ Await all the way up
var result = await _mediator.Send(command, cancellationToken);
```

---

## Backend Patterns Checklist

Before completing a feature:

- [ ] Entity inherits `BaseEntity`; created via factory method (`Create(...)`)
- [ ] Command/Query defined as `record` implementing `IRequest<T>`
- [ ] Handler does NOT contain validation logic — validator class exists
- [ ] EF queries use `Select(...)` projection for read operations
- [ ] No `SELECT *` — only needed columns fetched
- [ ] `Include()` used only when navigation properties are part of the result
- [ ] Multi-step writes wrapped in a transaction + `ExecutionStrategy`
- [ ] Domain events published **after** `SaveChangesAsync`
- [ ] Cross-module references are logical `Guid` only — no EF FKs
- [ ] Background service uses `IServiceScopeFactory` to resolve scoped dependencies
- [ ] `CancellationToken` passed to all async EF and MediatR calls
- [ ] Module registered via `Add{Module}Module(services, config)` extension method
