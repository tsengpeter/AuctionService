---
name: tdd-workflow
description: "Use this skill when writing new features, fixing bugs, or refactoring code in AuctionService. Enforces test-driven development with xUnit 2.9 + Testcontainers.PostgreSql + NSubstitute + FluentAssertions. Covers unit tests for handlers/validators and integration tests with WebApplicationFactory."
---

# Test-Driven Development Workflow — AuctionService

TDD practices for this ASP.NET Core 10 / C# 13 modular monolith.

## When to Activate

- Writing a new MediatR Command or Query
- Implementing a new API endpoint
- Fixing a bug
- Refactoring existing handlers or domain logic
- Adding a FluentValidation validator

---

## Test Stack

| Library | Version | Purpose |
|---|---|---|
| xUnit | 2.9 | Test framework |
| Testcontainers.PostgreSql | 4.x | Real PostgreSQL in integration tests |
| FluentAssertions | 8.x | Assertion DSL |
| NSubstitute | 5.x | Mocking/stubbing |

---

## Test Project Structure

```
tests/
├── AuctionService.UnitTests/         # Fast, no I/O
│   ├── Auction/
│   │   ├── CreateAuctionCommandHandlerTests.cs
│   │   ├── CreateAuctionCommandValidatorTests.cs
│   │   └── GetAuctionQueryHandlerTests.cs
│   ├── Bidding/
│   ├── Member/
│   └── Shared/
│       └── ValidationBehaviorTests.cs
└── AuctionService.IntegrationTests/  # Real PostgreSQL via Testcontainers
    ├── Auction/
    │   └── AuctionsControllerTests.cs
    ├── Member/
    ├── Health/
    └── Infrastructure/
        └── IntegrationTestBase.cs    # WebApplicationFactory + container setup
```

---

## TDD Workflow

### Step 1: Write a Failing Test First

Never write production code before a test. Red → Green → Refactor.

```csharp
// ✅ Write the test FIRST
[Fact]
public async Task Handle_ValidCommand_ReturnsAuctionDto()
{
    // Arrange
    var command = new CreateAuctionCommand("Vintage Watch", 100m, DateTimeOffset.UtcNow.AddDays(7));
    var handler = new CreateAuctionCommandHandler(_dbContext, _publisher);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Title.Should().Be("Vintage Watch");
    result.StartPrice.Should().Be(100m);
}
```

### Step 2: Run — Confirm It Fails

```bash
dotnet test tests/AuctionService.UnitTests/ --filter "FullyQualifiedName~CreateAuction"
```

### Step 3: Write Minimal Implementation

Write only enough code to make the test green. No extras.

### Step 4: Run Again — Confirm Green

```bash
dotnet test
```

### Step 5: Refactor

Improve readability, remove duplication — keep all tests green.

---

## Unit Test Patterns

### Handler Test (NSubstitute + FluentAssertions)

```csharp
public class CreateAuctionCommandHandlerTests : IAsyncLifetime
{
    private AuctionDbContext _db = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<AuctionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AuctionDbContext(options);
    }

    public async Task DisposeAsync() => await _db.DisposeAsync();

    [Fact]
    public async Task Handle_ValidCommand_PersistsAuction()
    {
        // Arrange
        var publisher = Substitute.For<IPublisher>();
        var handler = new CreateAuctionCommandHandler(_db, publisher);
        var command = new CreateAuctionCommand("Watch", 50m, DateTimeOffset.UtcNow.AddDays(3));

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Title.Should().Be("Watch");
        _db.Auctions.Should().HaveCount(1);
        await publisher.Received(1).Publish(
            Arg.Is<AuctionCreatedEvent>(e => e.AuctionId == result.Id),
            Arg.Any<CancellationToken>());
    }
}
```

### Validator Test

```csharp
public class CreateAuctionCommandValidatorTests
{
    private readonly CreateAuctionCommandValidator _validator = new();

    [Fact]
    public async Task Validate_EmptyTitle_ReturnsError()
    {
        var command = new CreateAuctionCommand("", 100m, DateTimeOffset.UtcNow.AddDays(1));

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public async Task Validate_NegativePrice_ReturnsError()
    {
        var command = new CreateAuctionCommand("Watch", -1m, DateTimeOffset.UtcNow.AddDays(1));

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.StartPrice);
    }

    [Fact]
    public async Task Validate_ValidCommand_PassesValidation()
    {
        var command = new CreateAuctionCommand("Watch", 100m, DateTimeOffset.UtcNow.AddDays(1));

        var result = await _validator.TestValidateAsync(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
```

### Domain Entity Test

```csharp
public class AuctionTests
{
    [Fact]
    public void Create_ValidArgs_SetsProperties()
    {
        var auction = Auction.Create("Watch", 100m);

        auction.Title.Should().Be("Watch");
        auction.StartPrice.Should().Be(100m);
        auction.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_AssignsNewGuidId()
    {
        var a1 = Auction.Create("A", 1m);
        var a2 = Auction.Create("B", 2m);

        a1.Id.Should().NotBe(a2.Id);
    }
}
```

---

## Integration Test Patterns

Integration tests use **Testcontainers** (real PostgreSQL) + **WebApplicationFactory**.

### Base Class Setup

```csharp
// tests/AuctionService.IntegrationTests/Infrastructure/IntegrationTestBase.cs
public abstract class IntegrationTestBase : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    protected HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(host =>
            {
                host.ConfigureAppConfiguration((ctx, cfg) =>
                {
                    cfg.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = _postgres.GetConnectionString(),
                        ["JWT_SECRET"] = "integration-test-secret-minimum-32-chars",
                        ["JWT_ISSUER"] = "AuctionService",
                        ["JWT_AUDIENCE"] = "AuctionService"
                    });
                });
            });

        Client = factory.CreateClient();
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();
}
```

### Controller Integration Test

```csharp
public class AuctionsControllerTests : IntegrationTestBase
{
    [Fact]
    public async Task CreateAuction_ValidRequest_Returns201()
    {
        // Arrange
        var request = new { title = "Vintage Watch", startPrice = 100, endsAt = DateTime.UtcNow.AddDays(7) };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auctions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuctionDto>>();
        body!.Success.Should().BeTrue();
        body.Data!.Title.Should().Be("Vintage Watch");
    }

    [Fact]
    public async Task CreateAuction_EmptyTitle_Returns422WithFieldError()
    {
        // Arrange
        var request = new { title = "", startPrice = 100, endsAt = DateTime.UtcNow.AddDays(7) };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auctions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        body!.Success.Should().BeFalse();
        body.Errors.Should().ContainSingle(e => e.Field == "title");
    }

    [Fact]
    public async Task GetAuction_NotFound_Returns404()
    {
        var response = await Client.GetAsync($"/api/auctions/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
```

---

## Test Naming Convention

Format: `MethodName_Condition_ExpectedResult`

```csharp
// ✅ Good
Handle_ValidCommand_ReturnsAuctionDto
Handle_AuctionNotFound_ThrowsKeyNotFoundException
Validate_EmptyTitle_ReturnsValidationError
CreateAuction_ValidRequest_Returns201

// ❌ Bad
TestCreate
AuctionTest1
HandlerWorks
```

---

## Running Tests

```bash
# Run all tests
dotnet test

# Run only unit tests
dotnet test tests/AuctionService.UnitTests/

# Run only integration tests
dotnet test tests/AuctionService.IntegrationTests/

# Run tests matching a filter
dotnet test --filter "FullyQualifiedName~Auction"
dotnet test --filter "Category=Unit"

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run with coverage (requires coverlet)
dotnet test --collect:"XPlat Code Coverage"
```

---

## Test Isolation Rules

- Each test must be **independent** — no shared mutable state between tests
- Unit tests use **in-memory DbContext** (`UseInMemoryDatabase`) or **NSubstitute** mocks
- Integration tests use **Testcontainers** — each test class gets its own PostgreSQL container
- Never rely on test execution order

```csharp
// ✅ Correct — each test sets up its own data
[Fact]
public async Task Test1()
{
    var auction = Auction.Create("Watch", 100m);
    _db.Auctions.Add(auction);
    await _db.SaveChangesAsync();
    // assert...
}

// ❌ Wrong — depends on state from another test
[Fact]
public async Task Test2()
{
    var existing = _db.Auctions.First(); // relies on Test1 having run first
}
```

---

## Coverage Requirements

- **Handlers**: Every public `Handle()` method must have at least a happy-path and one error-path test
- **Validators**: Every `RuleFor()` must have a test for the invalid case
- **Controllers**: Integration test for each route (200/201 happy path + 422 validation + 401 auth)
- **Domain entities**: Factory methods and any domain logic must be unit tested

---

## Common Mistakes to Avoid

```csharp
// ❌ Testing implementation details
Assert.Equal(1, _db.ChangeTracker.Entries().Count()); // brittle

// ✅ Test observable behavior
_db.Auctions.Should().HaveCount(1);

// ❌ No assertion (test always passes)
await handler.Handle(command, CancellationToken.None);

// ✅ Always assert the outcome
var result = await handler.Handle(command, CancellationToken.None);
result.Should().NotBeNull();

// ❌ Sharing DbContext between tests (state leaks)
private static AuctionDbContext _shared = new(...);  // static = shared

// ✅ Fresh context per test
public async Task InitializeAsync() { _db = new AuctionDbContext(...); }
```
