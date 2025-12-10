# AuctionService

A microservice for managing auctions in the auction application.

## Overview

The AuctionService is built using ASP.NET Core 10 Web API with Clean Architecture principles. It provides auction management functionality including creating, browsing, and bidding on auctions.

## Architecture

- **API Layer**: ASP.NET Core Web API controllers and middleware
- **Core Layer**: Business logic and domain entities
- **Infrastructure Layer**: Data access and external service integrations
- **Shared Layer**: Common utilities and extensions

## Technology Stack

- **Framework**: ASP.NET Core 10 Web API
- **Language**: C# 13
- **Database**: PostgreSQL 16+
- **ORM**: Entity Framework Core 10
- **Testing**: xUnit, FluentAssertions, Moq, Testcontainers
- **Logging**: Serilog
- **Validation**: FluentValidation
- **Documentation**: Swagger/OpenAPI

## Prerequisites

- .NET 10 SDK
- Docker (for local PostgreSQL development)
- Docker Compose (for multi-container setup)

## Development Setup

1. Clone the repository
2. Navigate to the AuctionService directory
3. Restore packages: `dotnet restore`
4. Start PostgreSQL: `docker-compose up -d postgres`
5. Run migrations: `dotnet ef database update`
6. Run the application: `dotnet run --project src/AuctionService.Api`

## Testing

- Unit tests: `dotnet test tests/AuctionService.UnitTests`
- Integration tests: `dotnet test tests/AuctionService.IntegrationTests`
- Contract tests: `dotnet test tests/AuctionService.ContractTests`

## Docker

Build and run with Docker Compose:

```bash
docker-compose up --build
```

## API Documentation

When running locally, visit `https://localhost:5001/swagger` for API documentation.

## Contributing

1. Follow the established coding standards
2. Write tests for new functionality
3. Ensure all tests pass before submitting PR
4. Update documentation as needed