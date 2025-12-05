# Deployment Guide

## Local Development

### Prerequisites
- .NET SDK 10.0
- Docker Desktop
- PostgreSQL 16

### Setup
1. Start database: `docker-compose up -d memberservice-db`
2. Run migrations: `dotnet ef database update --project src/MemberService.Infrastructure --startup-project src/MemberService.API`
3. Start API: `dotnet run --project src/MemberService.API`

## Production Deployment

### Docker
```bash
docker build -t memberservice .
docker run -d -p 80:80 \
  -e DB_CONNECTION_STRING="..." \
  -e JWT_SECRET_KEY="..." \
  memberservice
```

### Kubernetes
Use the provided k8s manifests in the k8s/ directory.

### Azure Deployment
1. Create Azure Database for PostgreSQL
2. Deploy to Azure Container Apps or AKS
3. Configure Key Vault for secrets

## Environment Variables

| Variable | Description | Example |
|----------|-------------|---------|
| DB_CONNECTION_STRING | PostgreSQL connection | Host=... |
| JWT_SECRET_KEY | JWT signing key | 32+ chars |
| SNOWFLAKE_WORKER_ID | Worker ID | 1 |
| SNOWFLAKE_DATACENTER_ID | Datacenter ID | 1 |
| ASPNETCORE_ENVIRONMENT | Environment | Production |

## Health Checks

- `/health` - Application health
- `/health/database` - Database connectivity