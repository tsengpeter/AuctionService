---
name: logging-observability
description: >
  Structured logging patterns for AuctionService (ASP.NET Core 10 / C# 13).
  Use when writing log statements, configuring log levels, adding request
  tracing, or reviewing code for PII leakage and noisy logs.
applyTo: "**/*.cs"
---

# Logging & Observability — AuctionService

## Current Stack

| Component | Implementation |
|-----------|---------------|
| Logging abstraction | `Microsoft.Extensions.Logging.ILogger<T>` |
| Providers | `AddConsole()` (all envs) + `AddJsonConsole()` (Development, indented) |
| Config | `appsettings.json` → `Logging.LogLevel` |
| Exception capture | `GlobalExceptionMiddleware` |

---

## 1. Log Levels — When to Use Each

| Level | Use for |
|-------|---------|
| `LogTrace` | Step-by-step execution details — **never in production** |
| `LogDebug` | Diagnostic details useful during development |
| `LogInformation` | High-level flow milestones (request received, entity created) |
| `LogWarning` | Recoverable issues, validation failures, expected edge cases |
| `LogError` | Unhandled exceptions, failures that require attention |
| `LogCritical` | System cannot continue (startup failure, missing JWT secret) |

**appsettings.json defaults:**
```json
"Logging": {
  "LogLevel": {
    "Default": "Information",
    "Microsoft.AspNetCore": "Warning",
    "Microsoft.EntityFrameworkCore": "Warning"
  }
}
```

Keep `Microsoft.EntityFrameworkCore` at `Warning` or higher — SQL query logs are noisy and belong in Development only.

---

## 2. Structured Logging — Message Templates

Always use **message templates with named placeholders**, never string interpolation.

```csharp
// ✅ Structured — queryable, parseable
_logger.LogInformation("Auction {AuctionId} created by member {MemberId}", auctionId, memberId);
_logger.LogWarning("Bid rejected for auction {AuctionId}: {Reason}", auctionId, reason);
_logger.LogError(ex, "Failed to process payment for order {OrderId}", orderId);

// ❌ String interpolation — loses structure, cannot be indexed
_logger.LogInformation($"Auction {auctionId} created by member {memberId}");
_logger.LogError($"Error: {ex.Message}");
```

### Placeholder Naming Conventions

- Use **PascalCase** for placeholder names: `{AuctionId}`, `{MemberId}`, `{Path}`
- Prefix destructured objects with `@`: `_logger.LogDebug("Command {@Command}", command);`
- For collections / counts, name them clearly: `{ItemCount}`, `{BatchSize}`

---

## 3. Inject ILogger via Constructor

```csharp
// ✅ Correct — inject generic ILogger<T>
public class CreateAuctionCommandHandler : IRequestHandler<CreateAuctionCommand, ApiResponse<AuctionDto>>
{
    private readonly ILogger<CreateAuctionCommandHandler> _logger;

    public CreateAuctionCommandHandler(ILogger<CreateAuctionCommandHandler> logger, ...)
    {
        _logger = logger;
    }

    public async Task<ApiResponse<AuctionDto>> Handle(CreateAuctionCommand request, CancellationToken ct)
    {
        _logger.LogInformation("Creating auction for seller {SellerId}", request.SellerId);
        // ...
        _logger.LogInformation("Auction {AuctionId} created successfully", auction.Id);
        return ApiResponseFactory.Ok(dto);
    }
}
```

---

## 4. GlobalExceptionMiddleware Logging

The existing middleware already handles error logging. Do **not** add redundant try/catch + log in handlers.

```csharp
// GlobalExceptionMiddleware pattern (already in place):
catch (ValidationException ex)
{
    _logger.LogWarning(ex, "Validation failed for {Path}", context.Request.Path);
    // → 422
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unhandled exception for {Method} {Path}",
        context.Request.Method, context.Request.Path);
    // → 500
}
```

**Rule:** Handlers should let exceptions bubble up to the middleware. Only catch exceptions you can meaningfully handle and log at the handler level.

---

## 5. What NOT to Log (Security)

```csharp
// ❌ Never log secrets or credentials
_logger.LogInformation("JWT secret: {Secret}", jwtSecret);
_logger.LogDebug("User password hash: {Hash}", passwordHash);

// ❌ Never log full request bodies that may contain PII
_logger.LogDebug("Request body: {Body}", rawBody);

// ❌ Never log full connection strings
_logger.LogInformation("Connected to: {ConnectionString}", connectionString);

// ✅ Log safe identifiers only
_logger.LogInformation("Member {MemberId} authenticated", memberId);
_logger.LogInformation("Connected to database host {Host}", dbHost);  // host only, not full string
```

**PII to avoid logging:** passwords, tokens, full email addresses, payment card numbers, personal addresses.

---

## 6. Logging in Domain Events & Background Services

```csharp
// Domain event handler — log event dispatch and result
public class AuctionEndedHandler : INotificationHandler<AuctionEndedEvent>
{
    private readonly ILogger<AuctionEndedHandler> _logger;

    public async Task Handle(AuctionEndedEvent notification, CancellationToken ct)
    {
        _logger.LogInformation("Handling AuctionEnded for auction {AuctionId}", notification.AuctionId);
        // ...
        _logger.LogInformation("Notification sent for auction {AuctionId}", notification.AuctionId);
    }
}

// IHostedService — log start/stop lifecycle
public class MyBackgroundService : BackgroundService
{
    private readonly ILogger<MyBackgroundService> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("{Service} started", nameof(MyBackgroundService));
        while (!stoppingToken.IsCancellationRequested)
        {
            // work...
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
        _logger.LogInformation("{Service} stopped", nameof(MyBackgroundService));
    }
}
```

---

## 7. Log Level Configuration Per Environment

**appsettings.json** (production defaults):
```json
"Logging": {
  "LogLevel": {
    "Default": "Information",
    "Microsoft.AspNetCore": "Warning",
    "Microsoft.EntityFrameworkCore": "Warning"
  }
}
```

**appsettings.Development.json** (verbose for local dev):
```json
"Logging": {
  "LogLevel": {
    "Default": "Debug",
    "Microsoft.AspNetCore": "Information",
    "Microsoft.EntityFrameworkCore": "Information"
  }
}
```

Enable EF Core SQL logging **only in Development** — never in production.

---

## 8. JSON Console Format (Development)

Already configured in `Program.cs`:
```csharp
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddJsonConsole(opts =>
    {
        opts.JsonWriterOptions = new JsonWriterOptions { Indented = true };
    });
}
```

JSON format makes logs parseable by tools like Seq, Datadog, or ELK if you add them later.

---

## 9. Log Scope for Request Correlation (Optional)

If you need to correlate multiple log lines within a single request:
```csharp
using (_logger.BeginScope(new Dictionary<string, object>
{
    ["CorrelationId"] = correlationId,
    ["UserId"] = userId
}))
{
    _logger.LogInformation("Processing bid for auction {AuctionId}", auctionId);
    // All logs inside this block automatically include CorrelationId + UserId
}
```

---

## 10. Checklist Before Committing

- [ ] Used message template (no `$""` interpolation) in all new log calls
- [ ] No credentials, tokens, or PII in log messages
- [ ] Log level matches severity (Warning for expected failures, Error for exceptions)
- [ ] EF Core log level not set to Debug/Information in production appsettings.json
- [ ] Background service logs start/stop lifecycle
- [ ] Handlers let unhandled exceptions bubble to GlobalExceptionMiddleware
