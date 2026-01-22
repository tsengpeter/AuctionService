# Deployment Guide

## Local Development

### Prerequisites
- .NET SDK 10.0
- Docker Desktop
- PostgreSQL 16
- Redis 7

### Setup
1. Start services: `docker-compose up -d`
2. Run migrations: `dotnet ef database update --project src/MemberService.Infrastructure --startup-project src/MemberService.API`
3. Start API: `dotnet run --project src/MemberService.API`

## Production Deployment

### Container Images
- **PostgreSQL**: postgres:16 (standard edition)
- **Redis**: redis:7-alpine
- **API**: Built from Dockerfile

### Docker
```bash
docker build -t memberservice .
docker run -d -p 80:80 \
  -e ConnectionStrings__MemberDb="Host=...;Port=5432;..." \
  -e ConnectionStrings__Redis="host:6379" \
  -e Jwt__SecretKey="..." \
  -e Email__SmtpHost="..." \
  -e AWS__AccessKeyId="..." \
  memberservice
```

### Docker Compose
```bash
# Start all services (PostgreSQL 16 + Redis 7 + API)
docker-compose up -d

# Check service status
docker-compose ps

# View logs
docker-compose logs -f memberservice-api
```

### Kubernetes
Use the provided k8s manifests in the k8s/ directory.

### Azure Deployment
1. Create Azure Database for PostgreSQL 16
2. Create Azure Cache for Redis 7
3. Deploy to Azure Container Apps or AKS
4. Configure Key Vault for secrets

## Environment Variables

| Variable | Description | Example |
|----------|-------------|---------|
| ConnectionStrings__MemberDb | PostgreSQL connection | Host=...;Port=5432;... |
| ConnectionStrings__Redis | Redis connection | host:6379 |
| Jwt__SecretKey | JWT signing key | 32+ chars |
| Jwt__Issuer | JWT issuer | MemberService |
| Jwt__Audience | JWT audience | MemberService |
| Jwt__ExpiryInMinutes | Token expiry | 15 |
| Snowflake__WorkerId | Worker ID | 1 |
| Snowflake__DatacenterId | Datacenter ID | 1 |
| Email__SmtpHost | Email SMTP host | smtp.gmail.com |
| Email__SmtpPort | Email SMTP port | 587 |
| AWS__AccessKeyId | AWS access key | ... |
| AWS__SecretAccessKey | AWS secret key | ... |
| AWS__Region | AWS region | us-east-1 |
| AliCloud__AccessKeyId | AliCloud key | ... |
| AliCloud__AccessKeySecret | AliCloud secret | ... |
| TEST_POSTGRES_IMAGE | Test PostgreSQL image | postgres:16-alpine |
| TEST_REDIS_IMAGE | Test Redis image | redis:7-alpine |
| ASPNETCORE_ENVIRONMENT | Environment | Production |

## Health Checks

- `/health` - Application health
- `/health/database` - PostgreSQL connectivity
- `/health/redis` - Redis connectivity

## Testing Environment

### Testcontainers
- **PostgreSQL**: postgres:16-alpine (faster startup)
- **Redis**: redis:7-alpine
- Environment variables allow version override
- All 231 tests passing