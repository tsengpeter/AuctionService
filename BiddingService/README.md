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
- PostgreSQL 14+
- Redis 7+

### Running Locally

1. Clone the repository
2. Navigate to the BiddingService directory
3. Start dependencies:
   ```bash
   docker-compose up -d
   ```
4. Run the application:
   ```bash
   dotnet run --project src/BiddingService.Api
   ```
5. Run tests:
   ```bash
   dotnet test
   ```

### Configuration

The service uses the following environment variables:

- `ConnectionStrings__Database`: PostgreSQL connection string
- `ConnectionStrings__Redis`: Redis connection string
- `AuctionService__BaseUrl`: Auction service base URL
- `Encryption__Key`: AES encryption key (32 bytes, base64 encoded)
- `Encryption__IV`: AES initialization vector (16 bytes, base64 encoded)

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

The service is containerized and can be deployed using Docker:

```bash
docker build -t biddingservice .
docker run -p 8080:80 biddingservice
```

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