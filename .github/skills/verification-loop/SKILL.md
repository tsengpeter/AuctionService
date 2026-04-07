---
name: verification-loop
description: "A comprehensive verification system for AuctionService (ASP.NET Core 10 / C# 13). Run after completing a feature, before creating a PR, or after any significant code change. Covers dotnet build, test, security scan, and diff review."
---

# Verification Loop — AuctionService

Run this checklist after completing a feature or significant change.

## When to Use

- After implementing a new endpoint or handler
- Before creating a PR
- After refactoring code
- After modifying middleware or DI registration

> **Definition of Done**: A feature is **NOT complete** until both unit tests and integration tests pass with zero failures. Always run the full test suite before declaring a task finished.

---

## Phase 1: Build Verification

```bash
dotnet build
```

**Expected:** 0 errors, 0 warnings (treat warnings as errors in CI).

If build fails: **STOP** — fix all errors before continuing.

---

## Phase 2: Test Suite

```bash
# Run all tests
dotnet test

# With detailed output if failures occur
dotnet test --logger "console;verbosity=detailed"

# Unit tests only (fast — no Docker required)
dotnet test tests/AuctionService.UnitTests/

# Integration tests (requires Docker for Testcontainers)
dotnet test tests/AuctionService.IntegrationTests/
```

Report after running:

```
Tests:    [PASS/FAIL]
  Total:   X
  Passed:  X
  Failed:  X  ← list each failing test
  Skipped: X  ← investigate any skipped tests
```

**Target:** 0 failures, 0 unexplained skips.

---

## Phase 3: Security Scan

Check for hardcoded secrets in source files:

```powershell
# Hardcoded JWT secrets or passwords
Select-String -Path "src/**/*.cs" -Pattern "secret|password|api.?key|token" -Recurse -CaseSensitive:$false |
  Where-Object { $_.Line -notmatch "//|configuration\[|IConfiguration|appsettings" }

# Hardcoded connection strings
Select-String -Path "src/**/*.cs" -Pattern "Host=|Server=|DataSource=" -Recurse

# Raw SQL string interpolation (SQL injection risk)
Select-String -Path "src/**/*.cs" -Pattern 'FromSqlRaw\(\$"' -Recurse
```

Also verify:
- [ ] `appsettings.json` contains no real secrets (only placeholder/empty values)
- [ ] `appsettings.Development.json` contains no production credentials
- [ ] `.env` is in `.gitignore`

---

## Phase 4: API Contract Check

For any modified or new endpoints:

- [ ] Response uses `ApiResponseFactory` (not raw `Ok(dto)`)
- [ ] Controller has `[Authorize]` or `[AllowAnonymous]` on every action
- [ ] New commands have a paired `AbstractValidator<T>`
- [ ] List endpoints support `page` / `pageSize` query params
- [ ] HTTP status codes match conventions (201 for create, 422 for validation, etc.)

---

## Phase 5: Architecture Rules Check

```powershell
# Check for cross-module DbContext usage (module A importing module B's DbContext)
# Each module's handlers should only reference their own DbContext
Select-String -Path "src/Modules/Auction/**/*.cs" -Pattern "BiddingDbContext|MemberDbContext|OrderingDbContext" -Recurse
Select-String -Path "src/Modules/Bidding/**/*.cs" -Pattern "AuctionDbContext|MemberDbContext|OrderingDbContext" -Recurse
# Repeat for each module
```

- [ ] No cross-module EF navigation properties
- [ ] No `app.Map*` (Minimal API) anywhere in the codebase
- [ ] Domain events published **after** `SaveChangesAsync`, never before

---

## Phase 6: Diff Review

```bash
# Show what changed
git diff --stat HEAD

# Review file-by-file
git diff HEAD
```

For each changed file, check:
- [ ] No unintended changes to unrelated files
- [ ] No debug code left in (`Console.WriteLine`, `Debugger.Break()`, `throw new NotImplementedException()`)
- [ ] No commented-out production code

---

## Verification Report Template

```
VERIFICATION REPORT
===================
Date:      2026-04-07
Feature:   [feature name]

Build:     [PASS / FAIL — X errors]
Tests:     [PASS / FAIL — X/Y passed]
  Unit:         X/X
  Integration:  X/X
Security:  [PASS / ISSUES FOUND]
  Hardcoded secrets: none / [list]
  Raw SQL interpolation: none / [list]
API Contract:  [PASS / ISSUES]
Architecture:  [PASS / ISSUES]
Diff:      [X files changed, clean / concerns noted]

Overall:   [READY for PR / NOT READY — fix list below]

Issues to Fix:
1. ...
```

---

## Quick Single-Command Run

```bash
dotnet build && dotnet test
```

> **This is the mandatory gate.** A feature is not complete until this command exits with code 0.
> Both test projects must pass:
> - `tests/AuctionService.UnitTests/` — fast, no Docker required
> - `tests/AuctionService.IntegrationTests/` — requires Docker for Testcontainers
>
> Do NOT mark a task as done, open a PR, or move to the next task until `dotnet test` is fully green.
