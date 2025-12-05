# Architecture

## Overview

MemberService follows Clean Architecture principles with four layers:

- **API Layer**: Controllers, middleware, configuration
- **Application Layer**: Use cases, DTOs, validation, services
- **Domain Layer**: Entities, value objects, domain logic, interfaces
- **Infrastructure Layer**: External concerns (database, external APIs, file system)

## Key Design Decisions

### Authentication
- JWT tokens with HS256 algorithm
- Access tokens: 15 minutes
- Refresh tokens: 7 days
- BCrypt password hashing with work factor 12

### ID Generation
- Snowflake IDs for distributed uniqueness
- 64-bit integers for better performance than GUIDs

### Database
- PostgreSQL with EF Core Code-First
- Async operations throughout
- Repository pattern for data access

### Validation
- FluentValidation for input validation
- Domain validation in value objects

### Testing
- TDD approach with xUnit
- Integration tests with Testcontainers
- >80% code coverage target

## Dependencies

```
API -> Application -> Domain <- Infrastructure
     |                ^
     v                |
Application -> Infrastructure
```

- API depends on Application and Infrastructure
- Application depends on Domain
- Infrastructure depends on Domain
- Domain has no dependencies