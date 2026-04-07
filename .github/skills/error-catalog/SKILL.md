---
name: error-catalog
description: >
  Business error patterns and HTTP status code conventions for AuctionService
  (ASP.NET Core 10 / C# 13). Use when handling exceptions, mapping business
  errors to HTTP responses, or designing new error scenarios for any module.
applyTo: "**/*.cs"
---

# Error Catalog — AuctionService

---

## 1. Error Response Shape

All error responses use `ApiResponse` from `AuctionService.Shared`:

```csharp
// Validation error (422)
public record ApiResponse(bool Success, IEnumerable<FieldError>? Errors, string? Error, int StatusCode);

// General error (4xx / 5xx)
// ApiResponseFactory.Fail("message", statusCode)
```

JSON output:
```json
// 422 Validation
{ "success": false, "errors": [{"field": "Amount", "message": "Must be greater than 0"}], "statusCode": 422 }

// 404 Not Found
{ "success": false, "error": "Auction not found.", "statusCode": 404 }

// 409 Conflict
{ "success": false, "error": "Auction is not active.", "statusCode": 409 }
```

---

## 2. Exception → HTTP Status Code Mapping

`GlobalExceptionMiddleware` handles all unhandled exceptions:

| Exception Type | HTTP Status | When to Use |
|----------------|-------------|-------------|
| `FluentValidation.ValidationException` | 422 | Input validation fails (handled automatically by pipeline) |
| `KeyNotFoundException` | 404 | Requested resource not found |
| `InvalidOperationException` | 409 | Business rule / state machine violation |
| `UnauthorizedAccessException` | 403 | Authenticated but not permitted |
| `ArgumentException` / `ArgumentNullException` | 400 | Caller passed invalid argument (rare — prefer FluentValidation) |
| Any other `Exception` | 500 | Unexpected server error |

**GlobalExceptionMiddleware already handles `ValidationException` and generic `Exception`.** For the remaining types (404, 409, 403), map them in the middleware:

```csharp
// GlobalExceptionMiddleware.cs — extend the exception handling
catch (KeyNotFoundException ex)
{
    _logger.LogWarning(ex, "Resource not found for {Path}", context.Request.Path);
    var response = ApiResponseFactory.Fail(ex.Message, 404);
    await WriteJsonResponse(context, 404, response);
}
catch (InvalidOperationException ex)
{
    _logger.LogWarning(ex, "Business rule violation for {Path}", context.Request.Path);
    var response = ApiResponseFactory.Fail(ex.Message, 409);
    await WriteJsonResponse(context, 409, response);
}
catch (UnauthorizedAccessException ex)
{
    _logger.LogWarning(ex, "Access denied for {Path}", context.Request.Path);
    var response = ApiResponseFactory.Fail(ex.Message, 403);
    await WriteJsonResponse(context, 403, response);
}
```

---

## 3. Per-Module Business Error Catalog

### Member Module

| Scenario | Exception | HTTP |
|----------|-----------|------|
| Email already registered | `InvalidOperationException("Email is already registered.")` | 409 |
| Member not found | `KeyNotFoundException($"Member {id} not found.")` | 404 |
| Invalid credentials | `UnauthorizedAccessException("Invalid email or password.")` | 401* |
| Username taken | `InvalidOperationException("Username is already taken.")` | 409 |

*401 for authentication failures is handled separately by JWT `OnChallenge`.

### Auction Module

| Scenario | Exception | HTTP |
|----------|-----------|------|
| Auction not found | `KeyNotFoundException($"Auction {id} not found.")` | 404 |
| Auction not in Pending status | `InvalidOperationException($"Cannot activate auction in status {Status}.")` | 409 |
| Auction not Active (for bidding) | `InvalidOperationException("Auction is not currently active.")` | 409 |
| Auction already ended/cancelled | `InvalidOperationException($"Cannot modify auction in status {Status}.")` | 409 |
| EndsAt in the past | FluentValidation → 422 | 422 |

### Bidding Module

| Scenario | Exception | HTTP |
|----------|-----------|------|
| Auction not active | `InvalidOperationException("Can only bid on active auctions.")` | 409 |
| Bid amount too low | `InvalidOperationException($"Bid must exceed current highest bid of {highest}.")` | 409 |
| Bidder bids on own auction | `InvalidOperationException("Cannot bid on your own auction.")` | 409 |
| Invalid bid amount | FluentValidation → 422 | 422 |

### Ordering Module

| Scenario | Exception | HTTP |
|----------|-----------|------|
| Order not found | `KeyNotFoundException($"Order {id} not found.")` | 404 |
| Order already paid | `InvalidOperationException("Order has already been paid.")` | 409 |
| Order cancelled | `InvalidOperationException("Cannot process a cancelled order.")` | 409 |

### Notification Module

| Scenario | Exception | HTTP |
|----------|-----------|------|
| Notification not found | `KeyNotFoundException($"Notification {id} not found.")` | 404 |

---

## 4. Handler Error Patterns

```csharp
// ✅ Not Found — throw KeyNotFoundException, let middleware handle it
public async Task<ApiResponse<AuctionDto>> Handle(GetAuctionByIdQuery request, CancellationToken ct)
{
    var auction = await _db.AuctionItems.FindAsync([request.AuctionId], ct)
        ?? throw new KeyNotFoundException($"Auction {request.AuctionId} not found.");

    return ApiResponseFactory.Ok(auction.ToDto());
}

// ✅ Business rule violation — throw from entity method (InvalidOperationException)
public async Task<ApiResponse> Handle(EndAuctionCommand request, CancellationToken ct)
{
    var auction = await _db.AuctionItems.FindAsync([request.AuctionId], ct)
        ?? throw new KeyNotFoundException($"Auction {request.AuctionId} not found.");

    auction.End();   // throws InvalidOperationException if not in Active status
    await _db.SaveChangesAsync(ct);
    return ApiResponseFactory.Ok(true);
}

// ✅ Validation error — FluentValidation handles automatically, returns 422
public class CreateAuctionCommandValidator : AbstractValidator<CreateAuctionCommand>
{
    public CreateAuctionCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.StartingPrice).GreaterThan(0);
        RuleFor(x => x.EndsAt).GreaterThan(DateTimeOffset.UtcNow)
            .WithMessage("EndsAt must be in the future.");
    }
}

// ❌ Wrong — manually returning error ApiResponse from handler
public async Task<ApiResponse> Handle(EndAuctionCommand request, CancellationToken ct)
{
    if (auction.Status != AuctionStatus.Active)
        return ApiResponseFactory.Fail("Auction is not active.", 409);  // don't do this
}
```

---

## 5. Error Message Conventions

- Use **English** for all exception messages (code and logs are in English)
- Be **specific**: include the resource ID in not-found messages
  - ✅ `$"Auction {id} not found."`
  - ❌ `"Auction not found"`
- State machine messages should include **current state**
  - ✅ `$"Cannot activate auction in status {Status}."`
  - ❌ `"Invalid operation"`
- **Never** expose stack traces, internal types, or DB schema details in error messages
- Validation field names use **PascalCase** matching the command property name

---

## 6. FluentValidation Common Rules

```csharp
// Required string
RuleFor(x => x.Title).NotEmpty().MaximumLength(200);

// Positive decimal
RuleFor(x => x.Amount).GreaterThan(0);

// Guid not empty
RuleFor(x => x.AuctionId).NotEmpty();

// Future date
RuleFor(x => x.EndsAt)
    .GreaterThan(DateTimeOffset.UtcNow)
    .WithMessage("EndsAt must be in the future.");

// Email format
RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);

// Min length + max length
RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
```

---

## 7. Do Not Return Errors from Controllers

Controllers dispatch to MediatR and return the result. They must not catch exceptions or build error responses:

```csharp
// ✅ Slim controller — errors handled by middleware
[HttpPost]
public async Task<IActionResult> CreateAuction(
    [FromBody] CreateAuctionCommand command,
    CancellationToken ct)
{
    var result = await _mediator.Send(command, ct);
    return StatusCode(result.StatusCode, result);
}

// ❌ Wrong — catching exceptions in controller
[HttpPost]
public async Task<IActionResult> CreateAuction([FromBody] CreateAuctionCommand command)
{
    try { ... }
    catch (Exception ex) { return BadRequest(ex.Message); }
}
```

---

## 8. Error Design Checklist

- [ ] Use `KeyNotFoundException` for not-found, `InvalidOperationException` for business rule violations
- [ ] Error messages include the resource ID (for not-found) or current state (for state violations)
- [ ] No stack traces or internal details in messages
- [ ] FluentValidation used for input constraints — not `if` checks in the handler
- [ ] Controller does not catch exceptions
- [ ] GlobalExceptionMiddleware handles all exception-to-response mapping
