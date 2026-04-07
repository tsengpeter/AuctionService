---
name: deployment-patterns
description: "Deployment workflows, CI/CD pipeline patterns, Docker containerization, health checks, rollback strategies, and production readiness checklists for AuctionService (.NET 10 / ASP.NET Core 10)."
---

# Deployment Patterns — AuctionService (.NET 10)

Production deployment workflows and CI/CD best practices for this ASP.NET Core modular monolith.

## When to Activate

- Setting up CI/CD pipelines
- Building or reviewing the Docker image
- Planning deployment strategy (rolling, blue-green)
- Implementing or updating the `/health` check endpoint
- Preparing for a production release
- Configuring environment-specific settings

---

## Deployment Strategies

### Rolling Deployment (Default)

Replace instances gradually — old and new versions run simultaneously during rollout.
Requires backward-compatible API changes and database migrations.

```
Instance 1: v1 → v2  (update first)
Instance 2: v1        (still running v1, serving traffic)

Instance 1: v2
Instance 2: v1 → v2  (update second)
```

**Pros:** Zero downtime, gradual rollout
**Cons:** Both versions run simultaneously — DB schema must be backward-compatible
**Use when:** Standard deployments, additive changes only

### Blue-Green Deployment

Run two identical environments, switch traffic atomically.

```
Blue  (v1) ← traffic     Green (v2) idle, apply DB migrations
→ After verification:
Blue  (v1) idle (standby) Green (v2) ← traffic
```

**Pros:** Instant rollback — just switch back to Blue
**Cons:** Requires 2× infrastructure during deployment
**Use when:** Critical releases, large DB schema changes, zero-tolerance for issues

---

## Docker Image — ASP.NET Core Multi-Stage Build

The project's `Dockerfile` at the repo root uses a two-stage build:

```dockerfile
# Stage 1: Build — SDK image
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first for layer caching
COPY AuctionService.slnx ./
COPY src/AuctionService.Api/AuctionService.Api.csproj src/AuctionService.Api/
# ... (other module .csproj files)

RUN dotnet restore src/AuctionService.Api/AuctionService.Api.csproj
COPY src/ src/
RUN dotnet publish src/AuctionService.Api/AuctionService.Api.csproj \
    -c Release -o /app/publish --no-restore

# Stage 2: Runtime — minimal ASP.NET Core image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN groupadd --system appgroup && useradd --system --gid appgroup --no-create-home appuser
COPY --from=build /app/publish .
RUN chown -R appuser:appgroup /app
USER appuser
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "AuctionService.Api.dll"]
```

**Key practices already in the Dockerfile:**
- Non-root user (`appuser`) for security
- Project files copied before source for layer caching
- SDK stage discarded — only runtime image ships
- No secrets baked into the image

---

## CI/CD Pipeline (GitHub Actions)

```yaml
name: CI/CD

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - run: dotnet restore
      - run: dotnet build --no-restore
      - run: dotnet test --no-build --configuration Release

  build-and-push:
    needs: test
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    steps:
      - uses: actions/checkout@v4
      - uses: docker/setup-buildx-action@v3
      - uses: docker/login-action@v3
        with:
          registry: ghcr.io
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}
      - uses: docker/build-push-action@v5
        with:
          push: true
          tags: ghcr.io/${{ github.repository }}:${{ github.sha }}
          cache-from: type=gha
          cache-to: type=gha,mode=max

  deploy:
    needs: build-and-push
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    environment: production
    steps:
      - name: Deploy
        run: |
          # Update running container to new image tag
          echo "Deploying ghcr.io/${{ github.repository }}:${{ github.sha }}"
```

### Pipeline Stages

```
PR opened:
  build → unit tests → integration tests

Merged to main:
  build → tests → docker image → deploy staging → smoke test /health → deploy production
```

---

## Environment Configuration

All configuration via environment variables — never hardcoded in source or config files.

```bash
# Required — must be set before starting the application
ConnectionStrings__DefaultConnection="Host=postgres;Port=5432;Database=auctionservice;Username=user;Password=pass"
JWT_SECRET="your-secret-minimum-32-characters-long"   # MUST be ≥32 chars
JWT_ISSUER="AuctionService"
JWT_AUDIENCE="AuctionService"
JWT_EXPIRY_MINUTES=60

# Optional — defaults shown
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
```

### Startup Validation (Fail-Fast)

The API startup code must validate critical secrets at boot:

```csharp
var jwtSecret = builder.Configuration["JWT_SECRET"]
    ?? throw new InvalidOperationException("JWT_SECRET is required.");
if (jwtSecret.Length < 32)
    throw new InvalidOperationException("JWT_SECRET must be at least 32 characters.");
```

---

## Health Checks

The API exposes `/health` (registered via `AddHealthChecks` in each module):

```
GET /health
200 OK    → { "status": "Healthy", ... }
503       → { "status": "Unhealthy", ... }
```

The `/health` endpoint checks all 5 module `DbContext` connections. The Docker Compose `healthcheck` and any upstream load balancer should probe this endpoint.

```yaml
# In docker-compose.full.yml (already configured):
healthcheck:
  test: ["CMD-SHELL", "curl -f http://localhost:8080/health || exit 1"]
  interval: 15s
  timeout: 5s
  retries: 5
  start_period: 30s
```

---

## Database Migration on Deploy

Migrations are NOT applied automatically inside the runtime container. Apply them as a pre-deploy step:

```bash
# Apply all pending migrations for all modules (run against target environment)
dotnet ef database update \
  --project src/Modules/Member/Member.csproj \
  --startup-project src/AuctionService.Api/AuctionService.Api.csproj

# Repeat for Auction, Bidding, Ordering, Notification modules
```

Or generate a SQL script for review before applying:

```bash
dotnet ef migrations script \
  --project src/Modules/Auction/Auction.csproj \
  --startup-project src/AuctionService.Api/AuctionService.Api.csproj \
  --output ./migrations-auction.sql
```

---

## Rollback Strategy

| Layer | Rollback Method |
|---|---|
| Application | Redeploy previous Docker image tag |
| Database | Forward migration that reverts changes (never delete applied migrations) |
| Configuration | Restore previous environment variable values |

```bash
# Rollback checklist
# 1. Redeploy previous image
docker compose pull && docker compose up -d --no-deps api

# 2. If migration was destructive, apply reverse migration
dotnet ef migrations add RevertXxx --project src/Modules/...
dotnet ef database update RevertXxx ...

# 3. Monitor /health until all instances healthy
```

---

## Production Readiness Checklist

### Application
- [ ] All tests pass (`dotnet test`)
- [ ] No hardcoded secrets in `.cs` / `.json` files
- [ ] `JWT_SECRET` is ≥ 32 chars and rotated from default
- [ ] `GlobalExceptionMiddleware` suppresses stack traces in production
- [ ] `/health` endpoint returns 200 with all modules healthy
- [ ] Swagger UI disabled in production (`!env.IsProduction()` guard in place)

### Docker / Infrastructure
- [ ] Image built from pinned base tag (`mcr.microsoft.com/dotnet/aspnet:10.0`)
- [ ] Non-root user (`appuser`) running the process
- [ ] `POSTGRES_PASSWORD` and `JWT_SECRET` injected at runtime (not in Dockerfile)
- [ ] `ASPNETCORE_ENVIRONMENT=Production` set

### Database
- [ ] All module migrations applied before traffic switch
- [ ] `ConnectionStrings__DefaultConnection` points to production database
- [ ] DB password changed from default `change_me_in_production`

### Monitoring
- [ ] `/health` endpoint monitored by uptime checker
- [ ] Structured logs (no PII) being collected
- [ ] Error rate alerting configured
