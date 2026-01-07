# BiddingService

A high-performance microservice for handling auction bidding operations, built with .NET 10 and Clean Architecture.

## Features

- **Real-time Bidding**: Handle bid submissions with Redis-backed high-performance caching
- **Bid History**: Query bidding history with pagination support
- **User Bids**: Retrieve all bids placed by a specific user
- **Auction Statistics**: Comprehensive auction analytics including price growth rates
- **Highest Bid Queries**: Fast retrieval of current highest bids
- **Background Sync**: Reliable Redis-to-PostgreSQL synchronization with dead letter queue
- **Health Checks**: Built-in health monitoring for Redis connectivity

## Architecture

This service follows Clean Architecture principles with the following layers:

- **API Layer**: ASP.NET Core Web API controllers and middleware
- **Core Layer**: Business logic, domain entities, interfaces, and DTOs
- **Infrastructure Layer**: Data access, external service clients, and background services
- **Shared Layer**: Common utilities and constants

## Technology Stack

- **Framework**: .NET 10 / ASP.NET Core 10
- **Database**: PostgreSQL 14+ with EF Core 10
- **Cache**: Redis 7 with Lua scripting
- **ID Generation**: Snowflake IDs via IdGen
- **Encryption**: AES-256-GCM for sensitive data
- **Testing**: xUnit, Moq, FluentAssertions, Testcontainers
- **Logging**: Serilog with structured logging
- **Monitoring**: Health checks, Prometheus metrics (planned)

## Quick Start

### Prerequisites

- .NET 10 SDK
- Docker and Docker Compose

### Running with Docker Compose

```bash
# Start all services (PostgreSQL, Redis, API)
docker-compose up -d --build

# View logs
docker-compose logs -f

# Stop all services
docker-compose down
```

### Running Locally (without Docker)

```bash
# Make sure PostgreSQL and Redis are running
dotnet run --project src/BiddingService.Api
```

### Access the API

- Swagger UI: http://localhost:5107 (HTTP) or https://localhost:7276 (HTTPS)
- Health check: http://localhost:5107/health

### Run Tests

```bash
dotnet test
```

## API Endpoints

### Bid Management
- `POST /api/bids` - Submit a new bid
- `GET /api/bids/{bidId}` - Get bid by ID
- `GET /api/bids/history/{auctionId}` - Get bid history for auction
- `GET /api/bids/my-bids` - Get current user's bids
- `GET /api/bids/highest/{auctionId}` - Get highest bid for auction
- `GET /api/bids/auctions/{auctionId}/stats` - Get auction statistics

### Health Checks
- `GET /health` - Overall health status
- `GET /health/redis` - Redis connectivity health

## Development

### Project Structure
```
src/
├── BiddingService.Api/          # Web API project
├── BiddingService.Core/         # Domain logic and interfaces
├── BiddingService.Infrastructure/ # Data access and external services
└── BiddingService.Shared/       # Common utilities

tests/
├── BiddingService.UnitTests/    # Unit tests
└── BiddingService.IntegrationTests/ # Integration tests
```

### Testing Strategy

- **Unit Tests**: Test individual components in isolation
- **Integration Tests**: Test component interactions with real dependencies (PostgreSQL, Redis)
- **Contract Tests**: Verify external service integrations

### Code Quality

- Follows C# coding standards and naming conventions
- Comprehensive test coverage
- Clean Architecture principles
- SOLID design principles

## Deployment

### Using Docker Compose

```bash
docker-compose up -d --build
```

### Using Docker

```bash
# Build image
docker build -f Dockerfile -t biddingservice:latest ./src

# Run container
docker run -p 5107:8080 -p 7276:8081 \
  -e ConnectionStrings__DefaultConnection="Host=biddingservice-db;Port=5432;Database=biddingservice_dev;Username=biddingservice;Password=Dev@Password123" \
  -e ConnectionStrings__Redis="biddingservice-redis:6379" \
  -e AuctionService__BaseUrl="http://auctionservice-api:8080" \
  -e Encryption__Key="your-key" \
  -e Encryption__IV="your-iv" \
  biddingservice:latest
```

**Note**: Update environment variables for production use.

## Monitoring

- Health checks for critical dependencies
- Structured logging with correlation IDs
- Prometheus metrics (planned)
- APM integration (planned)

## Contributing

1. Follow the established coding standards
2. Write tests for new features
3. Update documentation as needed
4. Ensure all tests pass before submitting PRs

## License

This project is licensed under the MIT License.