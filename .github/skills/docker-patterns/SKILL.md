---
name: docker-patterns
description: "Docker and Docker Compose patterns for AuctionService local development and production. Use when: setting up or modifying docker-compose.yml, building the ASP.NET Core container, debugging service connectivity, or managing PostgreSQL/pgAdmin containers."
---

# Docker Patterns — AuctionService

Docker and Docker Compose best practices for this ASP.NET Core 10 modular monolith.

## When to Activate

- Setting up or modifying `docker-compose.yml` / `docker-compose.full.yml`
- Building or debugging the API Docker image
- Troubleshooting container networking or volume issues
- Reviewing the `Dockerfile` for security or size
- Adding new services to the local development environment

---

## Project Docker Files

| File | Purpose |
|---|---|
| `Dockerfile` | Multi-stage build for the API (SDK → runtime) |
| `docker-compose.yml` | Dev dependencies only: PostgreSQL 16 + pgAdmin 4 |
| `docker-compose.full.yml` | Override that adds the API service |

---

## Local Development — Dependencies Only

```bash
# Start PostgreSQL + pgAdmin (most common — app runs with dotnet run)
docker compose up -d

# Start full stack including API container
docker compose -f docker-compose.yml -f docker-compose.full.yml up -d --build

# Stop and remove containers
docker compose down

# Stop and remove containers + volumes (resets DB)
docker compose down -v
```

### docker-compose.yml (actual project file)

```yaml
services:
  postgres:
    image: postgres:16-alpine
    container_name: auctionservice-postgres
    environment:
      POSTGRES_USER: ${POSTGRES_USER}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
      POSTGRES_DB: ${POSTGRES_DB}
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    networks:
      - auctionservice-network
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER} -d ${POSTGRES_DB}"]
      interval: 10s
      timeout: 5s
      retries: 5

  pgadmin:
    image: dpage/pgadmin4:latest
    container_name: auctionservice-pgadmin
    environment:
      PGADMIN_DEFAULT_EMAIL: admin@auctionservice.dev
      PGADMIN_DEFAULT_PASSWORD: ${PGADMIN_PASSWORD}
    ports:
      - "5050:80"
    networks:
      - auctionservice-network
    depends_on:
      postgres:
        condition: service_healthy

volumes:
  postgres_data:

networks:
  auctionservice-network:
    driver: bridge
```

### Service Ports (Local Dev)

| Service | Port | URL |
|---|---|---|
| PostgreSQL 16 | 5432 | `Host=localhost;Port=5432` |
| pgAdmin 4 | 5050 | http://localhost:5050 |
| API (full stack) | 8080 | http://localhost:8080 |
| API (dotnet run) | varies | see `launchSettings.json` |

---

## Required `.env` File

Create a `.env` file at the repo root (never commit it):

```bash
# .env — local development overrides
POSTGRES_USER=auctionuser
POSTGRES_PASSWORD=localdevpassword
POSTGRES_DB=auctionservice
PGADMIN_PASSWORD=pgadminpassword

# Used by docker-compose.full.yml
JWT_SECRET=local_dev_secret_minimum_32_characters_long
JWT_ISSUER=AuctionService
JWT_AUDIENCE=AuctionService
JWT_EXPIRY_MINUTES=60
ASPNETCORE_ENVIRONMENT=Development
```

`POSTGRES_PASSWORD`, `JWT_SECRET`, and `PGADMIN_PASSWORD` must be changed for any shared/production environment.

---

## ASP.NET Core Dockerfile (Existing)

The `Dockerfile` in the repo root follows the multi-stage pattern:

```dockerfile
# Stage 1: Build — uses the full .NET SDK
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first — layer caching avoids restore on every code change
COPY AuctionService.slnx ./
COPY src/AuctionService.Api/AuctionService.Api.csproj src/AuctionService.Api/
COPY src/AuctionService.Shared/AuctionService.Shared.csproj src/AuctionService.Shared/
COPY src/Modules/Member/Member.csproj src/Modules/Member/
# ... (remaining module .csproj files)

RUN dotnet restore src/AuctionService.Api/AuctionService.Api.csproj
COPY src/ src/
RUN dotnet publish src/AuctionService.Api/AuctionService.Api.csproj \
    -c Release -o /app/publish --no-restore

# Stage 2: Runtime — minimal ASP.NET Core image (no SDK)
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

---

## Networking & Service Discovery

Services in the same Compose network resolve by **service name**:

```
# From within the API container:
Host=postgres;Port=5432;...   ← "postgres" resolves to the DB container

# From appsettings or environment variable:
ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=auctionservice;...
```

```
# From the HOST machine (dotnet run):
Host=localhost;Port=5432;...  ← use localhost when running outside Docker
```

---

## Volume Strategy

```yaml
volumes:
  postgres_data:   # Named volume — persists DB data across container restarts
                   # docker compose down -v removes this (resets the database)
```

Do NOT use bind mounts for PostgreSQL data — they cause permission issues on Windows.

---

## Container Security (Already Applied)

The Dockerfile already implements:

| Practice | Status |
|---|---|
| Non-root user (`appuser`) | ✅ Applied |
| Multi-stage build (SDK not in runtime) | ✅ Applied |
| Pinned base tag (`sdk:10.0`, `aspnet:10.0`) | ✅ Applied |
| No secrets baked into image layers | ✅ Applied (env vars at runtime) |

Additional recommendations for `docker-compose`:

```yaml
services:
  api:
    security_opt:
      - no-new-privileges:true
    restart: unless-stopped
```

---

## Common Commands

```bash
# View logs for a specific service
docker compose logs -f api
docker compose logs -f postgres

# Connect to PostgreSQL CLI inside container
docker exec -it auctionservice-postgres psql -U auctionuser -d auctionservice

# Inspect environment variables inside running container
docker exec auctionservice-api env | grep -E "JWT|ConnectionStrings"

# Rebuild API image only (after code changes)
docker compose -f docker-compose.yml -f docker-compose.full.yml build api

# Remove dangling images
docker image prune -f
```

---

## .dockerignore (project root)

```
obj/
bin/
.git/
.env
.env.*
tests/
*.md
docker-compose*.yml
Dockerfile*
```

---

## Anti-Patterns

| Anti-Pattern | Why | Fix |
|---|---|---|
| `image: postgres:latest` | Breaks on minor updates | Use `postgres:16-alpine` |
| Secrets in `Dockerfile` ENV | Baked into image layers forever | Inject via `environment:` at runtime |
| Skipping `healthcheck` on PostgreSQL | API starts before DB is ready | Use `condition: service_healthy` |
| Bind-mounting source code into the runtime container | Bypasses the build layer | Rebuild image on code changes |
| Running `dotnet ef database update` inside the runtime container | Runtime image has no SDK | Run from host or a build-stage container |
