# MemberService

MemberService is a microservice for user registration, login, authentication, and profile management in the AuctionService system.

## Architecture

- **Framework**: ASP.NET Core 10 Web API
- **Database**: PostgreSQL 16
- **ORM**: Entity Framework Core 10
- **Authentication**: JWT (HS256)
- **Password Hashing**: BCrypt
- **ID Generation**: Snowflake ID
- **Architecture**: Clean Architecture (Domain/Application/Infrastructure/API)

## Prerequisites

- .NET SDK 10.0
- Docker Desktop
- PostgreSQL 16 (or Docker)

## Quick Start

1. Clone the repository
2. Navigate to MemberService directory
3. Run `docker-compose up -d` to start database
4. Run `dotnet run --project src/MemberService.API` to start the API

## API Endpoints

- POST /api/auth/register - User registration
- POST /api/auth/login - User login
- POST /api/auth/refresh-token - Refresh JWT token
- POST /api/auth/logout - Logout

## Development

- Run tests: `dotnet test`
- Build: `dotnet build`
- Restore packages: `dotnet restore`

## Configuration

Environment variables:
- `DB_CONNECTION_STRING`: PostgreSQL connection string
- `JWT_SECRET_KEY`: JWT signing key (min 32 characters)
- `SNOWFLAKE_WORKER_ID`: Worker ID for Snowflake
- `SNOWFLAKE_DATACENTER_ID`: Datacenter ID for Snowflake