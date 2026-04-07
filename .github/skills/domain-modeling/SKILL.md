---
name: domain-modeling
description: >
  Domain entity design patterns for AuctionService (ASP.NET Core 10 / C# 13).
  Use when designing or reviewing Domain layer code: entities, value objects,
  enums, state machines, domain events, and business invariants.
applyTo: "src/Modules/**/Domain/**"
---

# Domain Modeling — AuctionService

---

## 1. BaseEntity Contract

All entities inherit `BaseEntity` from `AuctionService.Shared`:

```csharp
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; protected set; }
    public DateTimeOffset UpdatedAt { get; protected set; }
}
```

Rules:
- `Id` is set in the static factory, **not** by EF Core
- `CreatedAt` and `UpdatedAt` are **always UTC** (`DateTimeOffset.UtcNow`)
- Always call `UpdatedAt = DateTimeOffset.UtcNow` inside any mutation method

---

## 2. Entity Design Rules

### Private Setters Only

```csharp
// ✅ Correct — state change only via methods
public class AuctionItem : BaseEntity
{
    public string Title { get; private set; } = string.Empty;
    public decimal StartingPrice { get; private set; }
    public AuctionStatus Status { get; private set; } = AuctionStatus.Pending;
}

// ❌ Wrong — public setters allow external mutation without invariant checks
public class AuctionItem : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public AuctionStatus Status { get; set; }
}
```

### Private Parameterless Constructor for EF Core

```csharp
public class AuctionItem : BaseEntity
{
    private AuctionItem() { }   // Required by EF Core — never public

    public static AuctionItem Create(...) { ... }
}
```

### Static Factory Method

```csharp
// ✅ Validate inputs in the factory
public static AuctionItem Create(string title, decimal startingPrice, DateTimeOffset endsAt)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(title);
    if (startingPrice <= 0)
        throw new ArgumentException("StartingPrice must be greater than 0.", nameof(startingPrice));

    var now = DateTimeOffset.UtcNow;
    if (endsAt <= now)
        throw new ArgumentException("EndsAt must be in the future.", nameof(endsAt));

    return new AuctionItem
    {
        Title = title.Trim(),
        StartingPrice = startingPrice,
        EndsAt = endsAt,
        Status = AuctionStatus.Pending,
        CreatedAt = now,
        UpdatedAt = now
    };
}
```

---

## 3. State Machines (Status Enums)

Use enums with explicit state transition methods. Guard every transition.

```csharp
public enum AuctionStatus { Pending, Active, Ended, Sold, Cancelled }

public void Activate()
{
    if (Status != AuctionStatus.Pending)
        throw new InvalidOperationException($"Cannot activate auction in status {Status}.");
    Status = AuctionStatus.Active;
    UpdatedAt = DateTimeOffset.UtcNow;
}

public void End()
{
    if (Status != AuctionStatus.Active)
        throw new InvalidOperationException($"Cannot end auction in status {Status}.");
    Status = AuctionStatus.Ended;
    UpdatedAt = DateTimeOffset.UtcNow;
}
```

**Rules:**
- Method name = action name, not `SetStatus()`
- Always check current state before transitioning
- Always update `UpdatedAt` on any mutation
- Always store enum as `string` in EF Core: `builder.Property(x => x.Status).HasConversion<string>()`

---

## 4. Cross-Module References — Guid Only

Never reference another module's Entity class. Use `Guid` logical references.

```csharp
// ✅ Correct — Guid reference, no navigation property
public class Bid : BaseEntity
{
    public Guid AuctionId { get; private set; }   // logical ref, no AuctionItem nav prop
    public Guid BidderId { get; private set; }    // logical ref, no MemberUser nav prop
    public decimal Amount { get; private set; }
}

// ❌ Wrong — cross-module navigation property
public class Bid : BaseEntity
{
    public AuctionItem Auction { get; private set; } = null!;    // crosses module boundary
    public MemberUser Bidder { get; private set; } = null!;      // crosses module boundary
}
```

---

## 5. Domain Events

Implement `IDomainEvent` (from `AuctionService.Shared`) and publish via MediatR.

```csharp
// Define the event
using AuctionService.Shared.Events;
using MediatR;

namespace Auction.Domain.Events;

public record AuctionEndedEvent(Guid AuctionId, DateTimeOffset OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    DateTimeOffset IDomainEvent.OccurredAt => OccurredAt;
}
```

```csharp
// Publish from handler (Application layer), NOT from the entity
public class EndAuctionCommandHandler : IRequestHandler<EndAuctionCommand, ApiResponse>
{
    private readonly AuctionDbContext _db;
    private readonly IPublisher _publisher;

    public async Task<ApiResponse> Handle(EndAuctionCommand request, CancellationToken ct)
    {
        var auction = await _db.AuctionItems.FindAsync([request.AuctionId], ct)
            ?? throw new KeyNotFoundException($"Auction {request.AuctionId} not found.");

        auction.End();   // state transition on entity
        await _db.SaveChangesAsync(ct);

        await _publisher.Publish(new AuctionEndedEvent(auction.Id, DateTimeOffset.UtcNow), ct);
        return ApiResponseFactory.Ok(true);
    }
}
```

```csharp
// Handle in another module via INotificationHandler
namespace Notification.Application.EventHandlers;

public class AuctionEndedNotificationHandler : INotificationHandler<AuctionEndedEvent>
{
    public async Task Handle(AuctionEndedEvent notification, CancellationToken ct)
    {
        // send notification using notification.AuctionId
    }
}
```

**Rules:**
- Events are **records** (`record`, not `class`) — immutable by design
- Publish events **after** `SaveChangesAsync` — data must be persisted first
- Event handlers in the **receiving module**, not in the originating module
- Do not directly query another module's DbContext inside an event handler

---

## 6. Value Objects (When to Use)

Use value objects for domain concepts with no identity — e.g., `Money`, `Address`, `DateRange`.

```csharp
// Simple value object — C# record
public record Money(decimal Amount, string Currency)
{
    public Money
    {
        if (Amount < 0) throw new ArgumentException("Amount cannot be negative.");
        ArgumentException.ThrowIfNullOrWhiteSpace(Currency);
    }

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot add different currencies.");
        return this with { Amount = Amount + other.Amount };
    }
}

// Owned entity in EF Core
modelBuilder.Entity<AuctionItem>().OwnsOne(x => x.StartingBid, m =>
{
    m.Property(x => x.Amount).HasPrecision(18, 2).HasColumnName("starting_amount");
    m.Property(x => x.Currency).HasMaxLength(3).HasColumnName("starting_currency");
});
```

**Use value objects when:** the concept has equality by value (not Id), multiple properties belong together, and the concept has its own invariants.

---

## 7. EF Core Mapping Conventions

| Pattern | EF Core configuration |
|---------|----------------------|
| Enum stored as string | `HasConversion<string>().HasMaxLength(20)` |
| Decimal money | `HasPrecision(18, 2)` |
| Max lengths | String properties always have `HasMaxLength()` |
| Table / column names | `snake_case` — `ToTable("auction_items")`, `HasColumnName("starts_at")` |
| Schema per module | `modelBuilder.HasDefaultSchema("auction")` |
| No cross-module FK | Never use `HasForeignKey` to another module's table |

---

## 8. Invariant Validation — Where It Lives

| Layer | Responsibility |
|-------|---------------|
| **Domain Entity** | Business invariants (state transitions, value constraints) — throw `InvalidOperationException` or `ArgumentException` |
| **FluentValidation** | Input validation (required, max length, format) — returns 422 |
| **Controller** | HTTP binding only — no logic |

```csharp
// Domain — business rule
public void PlaceBid(decimal amount)
{
    if (Status != AuctionStatus.Active)
        throw new InvalidOperationException("Can only bid on active auctions.");
    if (amount <= CurrentHighestBid)
        throw new InvalidOperationException($"Bid must exceed current highest bid of {CurrentHighestBid}.");
}

// Validator — input format
public class PlaceBidCommandValidator : AbstractValidator<PlaceBidCommand>
{
    public PlaceBidCommandValidator()
    {
        RuleFor(x => x.AuctionId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}
```

---

## 9. Entity Design Checklist

- [ ] Inherits `BaseEntity`
- [ ] All properties have `private set`
- [ ] Private parameterless constructor exists (for EF Core)
- [ ] Static `Create()` factory validates inputs and sets `CreatedAt`/`UpdatedAt`
- [ ] Every mutation method updates `UpdatedAt = DateTimeOffset.UtcNow`
- [ ] State transitions are guarded with current-state checks
- [ ] Cross-module references use `Guid` only — no navigation properties
- [ ] Enum stored as string in EF Core config
- [ ] Domain events are published from the **handler**, after `SaveChangesAsync`
