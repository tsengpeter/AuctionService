---
name: security-review
description: "Use this skill when adding authentication, handling user input, working with secrets, creating API endpoints, or implementing sensitive features in AuctionService. Provides ASP.NET Core security checklist covering JWT, FluentValidation, EF Core, CORS, and OWASP Top 10 patterns."
---

# Security Review — AuctionService (ASP.NET Core)

Security checklist and patterns for this C# 13 / .NET 10 modular monolith.

## When to Activate

- Implementing or reviewing authentication / authorization
- Handling user input or file uploads
- Creating new API endpoints
- Working with secrets or credentials
- Storing or transmitting sensitive data
- Reviewing any controller, handler, or middleware code

---

## 1. Secrets Management

### Rules

- All secrets come from **environment variables** — never hardcoded in code or config files
- `appsettings.json` must NOT contain real secrets — only non-sensitive defaults
- `JWT_SECRET` must be at least 32 characters
- Application fails fast at startup if required secrets are missing

### ❌ Wrong

```csharp
// Hardcoded secret — NEVER
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("my-secret-key"));

// Secret in appsettings.json — NEVER commit real values
"Jwt": { "Secret": "super-secret-key" }
```

### ✅ Correct

```csharp
// Read from environment / IConfiguration
var jwtSecret = configuration["JWT_SECRET"]
    ?? throw new InvalidOperationException("JWT_SECRET is required.");
if (jwtSecret.Length < 32)
    throw new InvalidOperationException("JWT_SECRET must be at least 32 characters.");

var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
```

### Checklist

- [ ] No secrets in `.cs`, `.json`, or `.yml` files
- [ ] `.env` is in `.gitignore`
- [ ] `JWT_SECRET` ≥ 32 chars validated at startup
- [ ] Connection string never contains hardcoded credentials in committed files

---

## 2. Input Validation

All user input is validated via **FluentValidation** + MediatR `ValidationBehavior`. 
`GlobalExceptionMiddleware` converts `ValidationException` → HTTP 422.

### ❌ Wrong

```csharp
// Manual validation or no validation in handler
public async Task<AuctionDto> Handle(CreateAuctionCommand request, CancellationToken ct)
{
    if (string.IsNullOrEmpty(request.Title)) throw new Exception("Title required"); // wrong
    // ...
}
```

### ✅ Correct

```csharp
// Validator class — runs automatically before handler
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

### Checklist

- [ ] Every command with user input has an `AbstractValidator<T>`
- [ ] No manual `if`-based validation in handlers
- [ ] Never trust data from `[FromBody]` without a paired validator
- [ ] File uploads validate size, MIME type, and extension (if applicable)

---

## 3. SQL Injection Prevention

EF Core uses **parameterized queries automatically** — this is the primary safeguard.

### ❌ Wrong

```csharp
// Raw interpolated SQL — SQL injection risk
var auctions = await _db.Auctions
    .FromSqlRaw($"SELECT * FROM auction.auctions WHERE title = '{userInput}'")
    .ToListAsync();
```

### ✅ Correct

```csharp
// LINQ — parameterized automatically
var auctions = await _db.Auctions
    .Where(a => a.Title == userInput)
    .ToListAsync(cancellationToken);

// Raw SQL with parameters (when LINQ is insufficient)
var auctions = await _db.Auctions
    .FromSqlRaw("SELECT * FROM auction.auctions WHERE title = {0}", userInput)
    .ToListAsync();
```

### Checklist

- [ ] No string interpolation inside `FromSqlRaw` or `ExecuteSqlRaw`
- [ ] LINQ used for all standard queries
- [ ] Raw SQL uses `{0}` placeholder syntax only

---

## 4. Authentication & Authorization

### JWT HS256 Setup Rules

- Algorithm: **HS256** (symmetric)
- Secret: environment variable `JWT_SECRET`, minimum 32 characters
- Validate issuer, audience, and lifetime
- 401/403 handled via **`JwtBearerEvents`**, NOT middleware or exception filters

### ❌ Wrong

```csharp
// Returning 401/403 via middleware — bypasses JWT event pipeline
app.Use(async (context, next) =>
{
    if (!context.User.Identity!.IsAuthenticated)
    {
        context.Response.StatusCode = 401;
        return;
    }
    await next();
});
```

### ✅ Correct

```csharp
// In AddJwtBearer configuration
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

### Controller Authorization Attributes

```csharp
[Authorize]                        // all authenticated users
[Authorize(Roles = "Admin")]       // specific role
[AllowAnonymous]                   // explicit public endpoint
```

### Checklist

- [ ] `[Authorize]` or `[AllowAnonymous]` explicitly on every controller action
- [ ] 401/403 responses go through `JwtBearerEvents`, not middleware
- [ ] JWT `ValidateIssuer`, `ValidateAudience`, `ValidateLifetime` all `true`
- [ ] JWT_SECRET from environment variable, minimum 32 chars
- [ ] Tokens are short-lived (default 60 minutes)

---

## 5. Error Handling — No Information Leakage

`GlobalExceptionMiddleware` already suppresses internal details. Never leak stack traces or DB errors.

### ❌ Wrong

```csharp
catch (Exception ex)
{
    return Problem(detail: ex.ToString(), statusCode: 500); // leaks stack trace
}
```

### ✅ Correct

```csharp
// GlobalExceptionMiddleware handles this automatically:
// - ValidationException  → 422 + field errors
// - KeyNotFoundException → 404 + generic message
// - Any Exception        → 500 + "An unexpected error occurred"
// Stack traces and DB messages never reach the response body.
```

### Checklist

- [ ] No `catch (Exception ex)` in handlers that returns `ex.Message` or `ex.ToString()` to the client
- [ ] HTTP 500 responses only contain generic messages
- [ ] `GlobalExceptionMiddleware` is the FIRST middleware in the pipeline

---

## 6. CORS Configuration

CORS must be explicitly configured — do not use `AllowAnyOrigin()` in production.

```csharp
// ✅ Correct — restrict to known origins
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins("https://auctionservice.example.com")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// ❌ Wrong — allows any origin
policy.AllowAnyOrigin(); // never in production
```

### Checklist

- [ ] `AllowAnyOrigin()` not used in production
- [ ] Allowed origins come from configuration, not hardcoded
- [ ] CORS policy applied before `UseAuthentication()`

---

## 7. API Response — Never Expose Internal Details

All responses go through `ApiResponseFactory`. Never use `Problem()` or raw exceptions in controllers.

```csharp
// ✅ Correct
return StatusCode(404, ApiResponseFactory.Fail("Auction not found", 404));

// ❌ Wrong — exposes framework internals
return Problem("Auction not found", statusCode: 404);
return BadRequest(ex.Message);
```

---

## 8. Sensitive Data in Logs

```csharp
// ❌ Wrong — logs password or token
_logger.LogInformation("Login attempt: {Email} {Password}", email, password);

// ✅ Correct — log only non-sensitive identifiers
_logger.LogInformation("Login attempt: {Email}", email);
_logger.LogWarning("Failed login for user {UserId}", userId);
```

### Checklist

- [ ] No passwords, JWT tokens, or connection strings in log output
- [ ] `ILogger<T>` used (not `Console.WriteLine`)
- [ ] Structured logging with named parameters (not string interpolation)

---

## 9. Dependency Security

```bash
# Check for known vulnerabilities in NuGet packages
dotnet list package --vulnerable

# Update outdated packages
dotnet outdated   # requires dotnet-outdated tool
```

### Checklist

- [ ] `dotnet list package --vulnerable` returns no high/critical issues
- [ ] Packages pinned to explicit versions in `.csproj`
- [ ] `Dependabot` or equivalent enabled on GitHub

---

## Pre-Deployment Security Checklist

- [ ] **Secrets**: No hardcoded secrets in any `.cs` or `.json` file
- [ ] **JWT**: Secret ≥ 32 chars, from environment, validated at startup
- [ ] **Input Validation**: All commands have FluentValidation validators
- [ ] **SQL Safety**: No interpolated raw SQL strings
- [ ] **Auth**: Every endpoint has `[Authorize]` or `[AllowAnonymous]`
- [ ] **401/403**: Handled via `JwtBearerEvents`, not middleware
- [ ] **Error Responses**: Stack traces never reach client
- [ ] **CORS**: Specific origins configured, no `AllowAnyOrigin()` in production
- [ ] **Logs**: No PII, passwords, or tokens in log output
- [ ] **Dependencies**: No known vulnerable NuGet packages
- [ ] **Swagger**: Disabled in production (`!env.IsProduction()` guard)
- [ ] **HTTPS**: `UseHttpsRedirection()` in middleware pipeline

---

## Resources

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [ASP.NET Core Security Docs](https://learn.microsoft.com/aspnet/core/security/)
- [JWT Best Practices](https://datatracker.ietf.org/doc/html/rfc8725)
